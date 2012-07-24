using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Rendering;
using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;

namespace Gaia.TerrainHelper
{
    public class Clipmap
    {
        #region Properties and Fields
        // G2 and Mm1G are values that are heavily used by our algorithm. To save 
        // calculationtime, we precalculate these two.
        private readonly int G2; // G * 2
        private readonly int Mm1G; // (M - 1) * G

        private int G;
        /// <summary>
        /// Gets the distance between two horizontal/vertical vertices. The finest level L = 0 has 
        /// a distance of G = 1. Then every level it doubles so G is always G = 2^L
        /// </summary>
        public int FactorG
        {
            get { return G; }
        }

        private int L;
        /// <summary>
        /// Gets the levelindex of current clip. The finest level has the index L = 0
        /// </summary>
        public int FactorL
        {
            get { return L; }
        }

        private int M;

        public int FactorM
        {
            get { return M; }
        }

        public Rectangle ClipRegion;

        public VertexPosition[] Vertices;

        public short[] Indices;

        /// <summary>
        /// Index to indicate how much vertices are added to the triangle strip.
        /// </summary>
        int stripIndex = 0;

        /// <summary>
        /// Gets the number of triangles that are visible in current frame.
        /// This changes every frame.
        /// </summary>
        public int Triangles
        {
            get { return stripIndex >= 3 ? this.stripIndex - 2 : 0; }
        }


        TerrainHeightmap Parent;

        BoundingBox bounds;
        public BoundingBox Bounds { get { return bounds; } }

        #endregion

        #region Constructor / Initialization
        /// <summary>
        /// Creates a new clipmaplevel.
        /// </summary>
        /// <param name="L">Levelindex of the clipmap. If is 0 this will be the finest level</param>
        /// <param name="N">Number of vertices per clipside. Must be one less than power of two.</param>
        /// <param name="S">Maximum terrainheight and heightscale</param>
        /// <param name="fieldsize">Width and heightvalue of the heightfield</param>
        /// <param name="heightfield">Heightvalues with a range of 0.0f - 1.0f</param>
        /// <param name="device">The used Graphicsdevice</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public Clipmap(int L, TerrainHeightmap parent)
        {
            this.Parent = parent;
            this.L = L;

            G = (int)Math.Pow(2, L);
            M = (Parent.N + 1) / 4;
            G2 = G * 2;
            Mm1G = (M - 1) * G;
            ClipRegion = new Rectangle(0, 0, (Parent.N - 1) * G, (Parent.N - 1) * G);

            // Initialize the vertices
            Initialize();
        }

        /// <summary>
        /// Initializes the vertices and indices.
        /// </summary>
        private void Initialize()
        {
            // create new vertexdeclaration of our custom VertexPosition4 struct

            //
            // Vertices
            //

            // N is the number of vertices per clipmapside, so number of all vertices is N * N
            Vertices = new VertexPosition[Parent.N * Parent.N];

            // Gp through all vertices of the current clip and update their height
            for (int z = 0; z < Parent.N; z++)
            {
                for (int x = 0; x < Parent.N; x++)
                {
                    // x,z is the position of a vertex in the vertexarray of the current clipmap.
                    // We must scale that up to another position that this vertex would have
                    // if we had a big vertexarray for the whole terrain. Here we use the G value.
                    // It says that the current level only shows every G'th vertex
                    // of the terrainmesh. This results in a simple LOD system where every clips
                    // LOD differs by G from the finest cliplevel where G is 1
                    UpdateVertex(x * G, z * G);
                }
            }

            //
            // Indices
            //

            // Looks weird, but this results in the smallest indicesarray we can get.
            // This comes from cutting a level into pieces and using dummyvertices
            // in the trianglestrip, like it is described later in the code.
            Indices = new short[4 * (3 * M * M + (Parent.N * Parent.N) / 2 + 4 * M - 10)];

            // Go through all all rows and fill them with vertexindices.
            for (int z = 0; z < Parent.N - 1; z++)
            {
                FillRow(0, Parent.N - 1, z, z + 1);
            }
        }
        #endregion

        #region Update Vertices
        /// <summary>
        /// Updates the vertices and indices of the clipmap.
        /// </summary>
        /// <param name="center">The center of the clipmap</param>
        public void UpdateVertices(Vector3 center)
        {
            // this method is visible to outside. Just call a private updatemethod
            // with prefered parameters.
            UpdateVertices((int)center.X, (int)center.Z);
        }

        /// <summary>
        /// Updates all vertices depend on the given clipmap centerposition.
        /// </summary>
        /// <param name="cx">clipmapcenter x-coordinate</param>
        /// <param name="cz">clipmapcenter y-coordinate</param>
        private void UpdateVertices(int cx, int cz)
        {
            // Store the old position to be able to recover it if needed
            int oldX = ClipRegion.X;
            int oldZ = ClipRegion.Y;

            // Calculate the new position
            ClipRegion.X = cx - ((Parent.N + 1) * G / 2);
            ClipRegion.Y = cz - ((Parent.N + 1) * G / 2);

            // Calculate the modulo to G2 of the new position.
            // This makes sure that the current level always fits in the hole of the
            // coarser level. The gridspacing of the coarser level is G * 2, so here G2.
            int modX = ClipRegion.X % G2;
            int modY = ClipRegion.Y % G2;
            modX += modX < 0 ? G2 : 0;
            modY += modY < 0 ? G2 : 0;
            ClipRegion.X += G2 - modX;
            ClipRegion.Y += G2 - modY;

            // Calculate the moving distance
            int dx = (ClipRegion.X - oldX);
            int dz = (ClipRegion.Y - oldZ);

            // Create some better readable variables.
            // This are just the bounds of the current level (the new region).
            int xmin = ClipRegion.Left;
            int xmax = ClipRegion.Right;
            int zmin = ClipRegion.Top;
            int zmax = ClipRegion.Bottom;

            bounds.Min = new Vector3(xmin, 0, zmin);
            bounds.Max = new Vector3(xmax, Parent.MaximumHeight, zmax);
            // Update now the L shaped region. This replaces the old data with the new one.
            if (dz > 0)
            {
                // Center moved in positive z direction.

                for (int z = zmin; z <= zmax - dz; z += G)
                {
                    if (dx > 0)
                    {
                        // Center moved in positive x direction.
                        // Update the right part of the L shaped region.
                        for (int x = xmax - dx + G; x <= xmax; x += G)
                        {
                            UpdateVertex(x, z);
                        }
                    }
                    else if (dx < 0)
                    {
                        // Center moved in negative x direction.
                        // Update the left part of the L shaped region.
                        for (int x = xmin; x <= xmin - dx - G; x += G)
                        {
                            UpdateVertex(x, z);
                        }
                    }
                }

                for (int z = zmax - dz + G; z <= zmax; z += G)
                {
                    // Update the bottom part of the L shaped region.
                    for (int x = xmin; x <= xmax; x += G)
                    {
                        UpdateVertex(x, z);
                    }
                }
            }
            else
            {
                // Center moved in negative z direction.

                for (int z = zmin; z <= zmin - dz - G; z += G)
                {
                    // Update the top part of the L shaped region.
                    for (int x = xmin; x <= xmax; x += G)
                    {
                        UpdateVertex(x, z);
                    }
                }

                for (int z = zmin - dz; z <= zmax; z += G)
                {
                    if (dx > 0)
                    {
                        // Center moved in poistive x direction.
                        // Update the right part of the L shaped region.
                        for (int x = xmax - dx + G; x <= xmax; x += G)
                        {
                            UpdateVertex(x, z);
                        }
                    }
                    else if (dx < 0)
                    {
                        // Center moved in negative x direction.
                        // Update the left part of the L shaped region.
                        for (int x = xmin; x <= xmin - dx - G; x += G)
                        {
                            UpdateVertex(x, z);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the height of a vertex at the specified position.
        /// The coordinates may be some values, even outside the map.
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="z">z-coordinate</param>
        private void UpdateVertex(int x, int z)
        {
            // Map the terraincoordinates to arraycoordinates.
            // Use modulo to N
            int posx = (x / G) % Parent.N;
            int posy = (z / G) % Parent.N;
            posx += posx < 0 ? Parent.N : 0;
            posy += posy < 0 ? Parent.N : 0;

            // Set both heightvalues to zero first.
            int index = posx + posy * Parent.N;
            Vertices[index].Position = new Vector4(x, 0, z, 0);
            if (x > 0 && x < Parent.width - 1 &&
                z > 0 && z < Parent.width - 1)
            {
                // If the position is inside the map, calculate the indices 
                // where we can get our heightvalues from.

                // Index to the heightvalue of the x z coordinate.
                int k = x + z * Parent.width;

                // indices of heightvalues we can use for the second height to avoid cracks
                int j;
                int l;

                if ((x % G2) == 0)
                {
                    if ((z % G2) == 0)
                    {
                        // Coordinates are regular. Dont need additional heightvalue.
                        j = k;
                        l = k;
                    }
                    else
                    {
                        // Z value is not regular. Get indices from higher and lover vertex.
                        j = x + (z - G) * Parent.width;
                        l = x + (z + G) * Parent.width;
                    }
                }
                else
                {
                    if ((z % G2) == 0)
                    {
                        // Z value is not regular. Get indices from higher and lover vertex.
                        j = (x - G) + z * Parent.width;
                        l = (x + G) + z * Parent.width;
                    }
                    else
                    {
                        // X value is not regular. Get indices from left and right vertex.
                        j = (x - G) + (z + G) * Parent.width;
                        l = (x + G) + (z - G) * Parent.width;
                    }
                }

                // Get the height of current coordinates
                // and set both heightvalues to that height.
                float height = Parent.heightValues[k];
                Vertices[index].Position.Y = height;
                Vertices[index].Position.W = height;

                if (l >= 0 && l < Parent.heightValues.Length && j >= 0 && j < Parent.heightValues.Length)
                {
                    // If we can get the additional height, get the two values, and apply the
                    // median of it to the W value
                    float coarser1 = Parent.heightValues[j];
                    float coarser2 = Parent.heightValues[l];
                    Vertices[index].Position.W = (coarser2 + coarser1) * 0.5f;
                }
            }
        }
        #endregion

        #region Update Indices


        /// <summary>
        /// Updates the whole indexarray.
        /// </summary>
        /// <param name="nextFinerLevel"></param>
        /// <param name="frustum"></param>
        public void UpdateIndices(Clipmap nextFinerLevel)
        {
            // set the stripindex to zero. We start count vertices from here.
            // The stripindex will tell us how much of the array is used.
            stripIndex = 0;
            #region Fill MxM Blocks
            // MxM Block 1
            Fill_Block(ClipRegion.Left,
                       ClipRegion.Left + Mm1G,
                       ClipRegion.Top,
                       ClipRegion.Top + Mm1G);

            // MxM Block 2
            Fill_Block(ClipRegion.Left + Mm1G,
                       ClipRegion.Left + 2 * Mm1G,
                       ClipRegion.Top,
                       ClipRegion.Top + Mm1G);

            // MxM Block 3
            Fill_Block(ClipRegion.Right - 2 * Mm1G,
                       ClipRegion.Right - Mm1G,
                       ClipRegion.Top,
                       ClipRegion.Top + Mm1G);

            // MxM Block 4
            Fill_Block(ClipRegion.Right - Mm1G,
                       ClipRegion.Right,
                       ClipRegion.Top,
                       ClipRegion.Top + Mm1G);

            // MxM Block 5
            Fill_Block(ClipRegion.Left,
                       ClipRegion.Left + Mm1G,
                       ClipRegion.Top + Mm1G,
                       ClipRegion.Top + 2 * Mm1G);

            // MxM Block 6
            Fill_Block(ClipRegion.Right - Mm1G,
                       ClipRegion.Right,
                       ClipRegion.Top + Mm1G,
                       ClipRegion.Top + 2 * Mm1G);

            // MxM Block 7
            Fill_Block(ClipRegion.Left,
                       ClipRegion.Left + Mm1G,
                       ClipRegion.Bottom - 2 * Mm1G,
                       ClipRegion.Bottom - Mm1G);

            // MxM Block 8
            Fill_Block(ClipRegion.Right - Mm1G,
                       ClipRegion.Right,
                       ClipRegion.Bottom - 2 * Mm1G,
                       ClipRegion.Bottom - Mm1G);

            // MxM Block 9
            Fill_Block(ClipRegion.Left,
                       ClipRegion.Left + Mm1G,
                       ClipRegion.Bottom - Mm1G,
                       ClipRegion.Bottom);

            // MxM Block 10
            Fill_Block(ClipRegion.Left + Mm1G,
                       ClipRegion.Left + 2 * Mm1G,
                       ClipRegion.Bottom - Mm1G,
                       ClipRegion.Bottom);

            // MxM Block 11
            Fill_Block(ClipRegion.Right - 2 * Mm1G,
                       ClipRegion.Right - Mm1G,
                       ClipRegion.Bottom - Mm1G,
                       ClipRegion.Bottom);

            // MxM Block 12
            Fill_Block(ClipRegion.Right - Mm1G,
                       ClipRegion.Right,
                       ClipRegion.Bottom - Mm1G,
                       ClipRegion.Bottom);
            #endregion

            #region Fill Fixup Blocks
            // Fixup Top 
            Fill_Block(ClipRegion.Left + 2 * Mm1G,
                       ClipRegion.Left + 2 * Mm1G + G2,
                       ClipRegion.Top,
                       ClipRegion.Top + Mm1G);

            // Fixup Left
            Fill_Block(ClipRegion.Left,
                       ClipRegion.Left + Mm1G,
                       ClipRegion.Top + 2 * Mm1G,
                       ClipRegion.Top + 2 * Mm1G + G2);

            // Fixup Right
            Fill_Block(ClipRegion.Right - Mm1G,
                       ClipRegion.Right,
                       ClipRegion.Top + 2 * Mm1G,
                       ClipRegion.Top + 2 * Mm1G + G2);

            // Fixup Bottom
            Fill_Block(ClipRegion.Left + 2 * Mm1G,
                       ClipRegion.Left + 2 * Mm1G + G2,
                       ClipRegion.Bottom - Mm1G,
                       ClipRegion.Bottom);
            #endregion

            #region Fill Interior Trim
            if (nextFinerLevel != null)
            {
                if ((nextFinerLevel.ClipRegion.X - ClipRegion.X) / G == M)
                {
                    if ((nextFinerLevel.ClipRegion.Y - ClipRegion.Y) / G == M)
                    {
                        // Upper Left L Shape

                        // Up
                        Fill_Block(ClipRegion.Left + Mm1G,
                                   ClipRegion.Right - Mm1G,
                                   ClipRegion.Top + Mm1G,
                                   ClipRegion.Top + Mm1G + G);
                        // Left
                        Fill_Block(ClipRegion.Left + Mm1G,
                                   ClipRegion.Left + Mm1G + G,
                                   ClipRegion.Top + Mm1G + G,
                                   ClipRegion.Bottom - Mm1G);
                    }
                    else
                    {
                        // Lower Left L Shape

                        // Left
                        Fill_Block(ClipRegion.Left + Mm1G,
                                   ClipRegion.Left + Mm1G + G,
                                   ClipRegion.Top + Mm1G,
                                   ClipRegion.Bottom - Mm1G - G);

                        // Bottom
                        Fill_Block(ClipRegion.Left + Mm1G,
                                   ClipRegion.Right - Mm1G,
                                   ClipRegion.Bottom - Mm1G - G,
                                   ClipRegion.Bottom - Mm1G);
                    }
                }
                else
                {
                    if ((nextFinerLevel.ClipRegion.Y - ClipRegion.Y) / G == M)
                    {
                        // Upper Right L Shape

                        // Up
                        Fill_Block(ClipRegion.Left + Mm1G,
                                   ClipRegion.Right - Mm1G,
                                   ClipRegion.Top + Mm1G,
                                   ClipRegion.Top + Mm1G + G);
                        // Right
                        Fill_Block(ClipRegion.Right - Mm1G - G,
                                   ClipRegion.Right - Mm1G,
                                   ClipRegion.Top + Mm1G + G,
                                   ClipRegion.Bottom - Mm1G);
                    }
                    else
                    {
                        // Lower Right L Shape

                        // Right
                        Fill_Block(ClipRegion.Right - Mm1G - G,
                                   ClipRegion.Right - Mm1G,
                                   ClipRegion.Top + Mm1G,
                                   ClipRegion.Bottom - Mm1G - G);

                        // Bottom
                        Fill_Block(ClipRegion.Left + Mm1G,
                                   ClipRegion.Right - Mm1G,
                                   ClipRegion.Bottom - Mm1G - G,
                                   ClipRegion.Bottom - Mm1G);
                    }
                }
            }
            #endregion

            #region Fill Fine Inner Level
            if (nextFinerLevel == null)
            {
                Fill_Block(ClipRegion.Left + Mm1G,
                           ClipRegion.Left + Mm1G + Parent.N / 2,
                           ClipRegion.Top + Mm1G,
                           ClipRegion.Top + Mm1G + Parent.N / 2);

                Fill_Block(ClipRegion.Left + Mm1G + Parent.N / 2,
                           ClipRegion.Right - Mm1G,
                           ClipRegion.Top + Mm1G,
                           ClipRegion.Top + Mm1G + Parent.N / 2);

                Fill_Block(ClipRegion.Left + Mm1G,
                           ClipRegion.Left + Mm1G + Parent.N / 2,
                           ClipRegion.Top + Mm1G + Parent.N / 2,
                           ClipRegion.Bottom - Mm1G);

                Fill_Block(ClipRegion.Left + Mm1G + Parent.N / 2,
                           ClipRegion.Right - Mm1G,
                           ClipRegion.Top + Mm1G + Parent.N / 2,
                           ClipRegion.Bottom - Mm1G);
            }
            #endregion
        }

        /// <summary>
        /// Fills a specified area to indexarray. This will be added only after
        /// a bounding test pass.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <param name="top"></param>
        /// <param name="bot"></param>
        private void Fill_Block(int left, int right, int top, int bot)
        {
            // Setup the boundingbox of the block to fill.
            // The lowest value is zero, the highest is the scalesize.
            BoundingBox box;
            box.Min.X = left;
            box.Min.Y = 0;
            box.Min.Z = top;
            box.Max.X = right;
            box.Max.Y = Parent.MaximumHeight;
            box.Max.Z = bot;

            //if (Parent.GetScene().MainCamera.GetFrustum().Contains(box) != ContainmentType.Disjoint)
            {
                // Same moduloprocedure as when we updated the vertices.
                // Maps the terrainposition to arrayposition.
                left = (left / G) % Parent.N;
                right = (right / G) % Parent.N;
                top = (top / G) % Parent.N;
                bot = (bot / G) % Parent.N;
                left += left < 0 ? Parent.N : 0;
                right += right < 0 ? Parent.N : 0;
                top += top < 0 ? Parent.N : 0;
                bot += bot < 0 ? Parent.N : 0;

                // Now fill the block.
                if (bot < top)
                {
                    // Bottom border is positioned somwhere over the top border,
                    // we have a wrapover so we must split up the update in two parts.

                    // Go from top border to the end of the array and update every row
                    for (int z = top; z <= Parent.N - 2; z++)
                    {
                        FillRow(left, right, z, z + 1);
                    }

                    // Update the wrapover row
                    FillRow(left, right, Parent.N - 1, 0);

                    // Go from arraystart to the bottom border and update every row.
                    for (int z = 0; z <= bot - 1; z++)
                    {
                        FillRow(left, right, z, z + 1);
                    }
                }
                else
                {
                    // Top boarder is over the bottom boarder. Update from top to bottom.
                    for (int z = top; z <= bot - 1; z++)
                    {
                        FillRow(left, right, z, z + 1);
                    }
                }
            }
        }

        /// <summary>
        /// Fills a strip of triangles that can be build between vertices row Zn and Zn1.
        /// </summary>
        /// <param name="x0">Start x-coordinate</param>
        /// <param name="xn">End x-coordinate</param>
        /// <param name="zn">Row n</param>
        /// <param name="zn1">Row n + 1</param>
        private void FillRow(int x0, int xn, int zn, int zn1)
        {
            // Rows are made of trianglestrips. All rows together build up a big single
            // trianglestrip. The probloem is when a row ends, it spans a triangle to the
            // start of the next row. We must hide that triangles. Therefore we add two
            // dummy indices, Twice the starting index and twice the ending index. This
            // will result in invisible triangles because when a triangle has two vertices
            // that are at exactly the same place, there is no area that the triangle can
            // cover. So four triangles between two rows look like this: 
            // (prev, END, END) (END, END, START') (END, START', START') and (START', START', next)
            // so we have four invisible triangles but all rows in a single trianglestrip.

            addIndex(x0, zn); // "START" dummyindex
            if (x0 <= xn)
            {
                for (int x = x0; x <= xn; x++)
                {
                    addIndex(x, zn);
                    addIndex(x, zn1);
                }
            }
            else
            {
                for (int x = x0; x <= Parent.N - 1; x++)
                {
                    addIndex(x, zn);
                    addIndex(x, zn1);
                }
                for (int x = 0; x <= xn; x++)
                {
                    addIndex(x, zn);
                    addIndex(x, zn1);
                }
            }
            addIndex(xn, zn1); // "END" dummyindex
        }

        /// <summary>
        /// Adds a specific index to indexarray.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        private void addIndex(int x, int z)
        {
            // calculate the index
            int i = x + z * Parent.N;
            // add the index and increment counter.
            Indices[stripIndex++] = (short)i;
        }
        #endregion
    }
}
