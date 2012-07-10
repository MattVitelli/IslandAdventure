using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Physics;

using Gaia.Core;
using Gaia.Voxels;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering;
using Gaia.Resources;
using Gaia.Physics;

namespace Gaia.SceneGraph.GameEntities
{
    public class TerrainVoxel : Terrain
    {
        public byte IsoValue = 127; //Defines density field isosurface cutoff value (ie the transition between solid and empty space)
                                    //so if a voxel had an element of 127 or lower, that would be empty space. A value higher than 127
                                    //Would be solid space.
        public int VoxelGridSize = 16; //Defines how many voxel geometries we have (used to balance performance)
        public int DensityFieldWidth = 129; //Density field is (2^n)+1 in size. (e.g. 65, 129, 257, 513) 
        public int DensityFieldHeight;
        public int DensityFieldDepth;

        VoxelGeometry[] Voxels;
        BoundingBox[] VoxelBounds;
        //VoxelCollision[] VoxelCollisions;

        const int NUM_BITS_BLENDING = 4;

        const int NUM_BITS_MATERIAL = 8 - NUM_BITS_BLENDING;

        int MAX_MATERIALS = (int)Math.Pow(2, NUM_BITS_MATERIAL);

        public byte[] DensityField;

        public Color[] MaterialField;

        Gaia.Resources.Material terrainMaterial;

        Matrix textureMatrix = Matrix.Identity;
        float TerrainSize = 256;

        RenderTarget2D srcTarget;
        Texture3D[] noiseTextures;

        Texture3D blendTexture;
        Texture3D textureAtlasColor;
        Texture3D textureAtlasNormal;

        public int[] surfaceIndices;

        public TerrainVoxel()
        {
            Transformation.SetScale(Vector3.One * TerrainSize);
            Transformation.SetPosition(Vector3.Up * TerrainSize * 0.25f);
            GenerateFloatingIslands(128);
            terrainMaterial = ResourceManager.Inst.GetMaterial("TerrainMaterial");
        }

        public TerrainVoxel(string filename)
        {
            Transformation.SetScale(new Vector3(1, 0.125f, 1) * TerrainSize);
            Transformation.SetPosition(Vector3.Up * TerrainSize * 0.25f);

            GenerateTerrainFromFile(filename);
            terrainMaterial = ResourceManager.Inst.GetMaterial("TerrainMaterial");
        }

        void AssembleTextureAtlas(Texture3D target, Texture2D[] srcTextures, int textureSize, int mipCount)
        {
            Shader imageShader = ResourceManager.Inst.GetShader("Generic2D");
            RenderTarget2D rtTarget = new RenderTarget2D(GFX.Device, textureSize, textureSize, 1, SurfaceFormat.Color);
            target = new Texture3D(GFX.Device, textureSize, textureSize, srcTextures.Length, mipCount, TextureUsage.AutoGenerateMipMap, SurfaceFormat.Color);

            Color[] colorBuffer = new Color[textureSize * textureSize];

            GFX.Device.SamplerStates[0].MagFilter = TextureFilter.Linear;
            GFX.Device.SamplerStates[0].MinFilter = TextureFilter.Linear;
            GFX.Device.SamplerStates[0].MipFilter = TextureFilter.Linear;
            imageShader.SetupShader();
            for (int i = 0; i < srcTextures.Length; i++)
            {
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(srcTextures[i].Width, srcTextures[i].Height));
                GFX.Device.Textures[0] = srcTextures[i];
                GFX.Device.SetRenderTarget(0, rtTarget);
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);
                rtTarget.GetTexture().GetData<Color>(colorBuffer);
                target.SetData<Color>(colorBuffer, colorBuffer.Length * i, colorBuffer.Length, SetDataOptions.None);
            }

            target.GenerateMipMaps(TextureFilter.Linear);
        }
        /*
        public override bool GetYPos(ref Vector3 pos, out Vector3 normal, float minY, float maxY)
        {

            Vector3 minPos = new Vector3(pos.X, minY, pos.Z);
            Vector3 maxPos = new Vector3(pos.X, maxY, pos.Z);
            minPos = Vector3.Transform(minPos, Transformation.GetObjectSpace());
            maxPos = Vector3.Transform(maxPos, Transformation.GetObjectSpace());
            minPos = minPos * 0.5f + Vector3.One * 0.5f;
            maxPos = maxPos * 0.5f + Vector3.One * 0.5f;

            int yBegin = (int)MathHelper.Clamp(minPos.Y * DensityFieldHeight, 0, DensityFieldHeight - 1);
            int yEnd = (int)MathHelper.Clamp(maxPos.Y * DensityFieldHeight, 0, DensityFieldHeight - 1);
            int xCrd = (int)MathHelper.Clamp(DensityFieldWidth * minPos.X, 0, DensityFieldWidth - 1);
            int zCrd = (int)MathHelper.Clamp(DensityFieldDepth * minPos.Z, 0, DensityFieldDepth - 1);
            int baseIndex = xCrd + zCrd * DensityFieldWidth * DensityFieldHeight;

            int yIndex = yBegin;
            bool freeSpaceFound = false;
            bool solidSpaceFound = false;
            for (int i = yBegin; i < yEnd; i++)
            {
                if (DensityField[baseIndex + i * DensityFieldSize] <= IsoValue)
                {
                    yIndex = i;
                    freeSpaceFound = true;
                }
                else
                {
                    solidSpaceFound = true;
                }
                if (freeSpaceFound && solidSpaceFound)
                    break;
            }
            minPos.Y = ((float)yIndex / (float)DensityFieldSize);
            minPos = minPos * 2.0f - Vector3.One;
            pos = Vector3.Transform(minPos, Transformation.GetTransform());
            normal = ComputeNormal(xCrd, yIndex, zCrd);
            return (freeSpaceFound && solidSpaceFound);
        }
        */
        public override void GenerateRandomTransform(Random rand, out Vector3 position, out Vector3 normal)
        {
            int bestY = -1;
            int randX = 0;
            int randZ = 0;
            while(bestY == -1)
            {
                randX = rand.Next(DensityFieldWidth-1);
                randZ = rand.Next(DensityFieldDepth-1);
                int index = randX + randZ * DensityFieldWidth * DensityFieldHeight;
                
                for (int i = 0; i < DensityFieldHeight; i++)
                {
                    if(DensityField[index+i*DensityFieldWidth] <= IsoValue)
                    {
                        bestY = i;
                        break;
                    }
                }
            }

            Vector3 vec = new Vector3((float)randX, (float)bestY, (float)randZ) / new Vector3(DensityFieldWidth - 1, DensityFieldHeight - 1, DensityFieldDepth - 1);
            position = Vector3.Transform(2.0f * vec - Vector3.One, Transformation.GetTransform());
            normal = ComputeNormal(randX, bestY, randZ);
        }

        public override bool GetTrianglesInRegion(Random rand, out List<TriangleGraph> availableTriangles, BoundingBox region)
        {
            
            Vector3 invRegionMin = Vector3.Transform(region.Min, this.Transformation.GetObjectSpace());
            Vector3 invRegionMax = Vector3.Transform(region.Max, this.Transformation.GetObjectSpace());
            //region.Min = invRegionMin;
            //region.Max = invRegionMax;

            if (float.IsNaN(invRegionMax.X) || float.IsNaN(invRegionMax.Y) || float.IsNaN(invRegionMax.Z)
                || float.IsNaN(invRegionMin.X) || float.IsNaN(invRegionMin.Y) || float.IsNaN(invRegionMin.Z))
            {
                Console.WriteLine("This is very bad");
            }

            int voxelCountX = (DensityFieldWidth - 1) / VoxelGridSize;
            int voxelCountY = (DensityFieldHeight - 1) / VoxelGridSize;
            int voxelCountZ = (DensityFieldDepth - 1) / VoxelGridSize;

            int voxelMinX = (int)MathHelper.Clamp((invRegionMin.X + 1.0f) * 0.5f * voxelCountX, 0, voxelCountX - 1);
            int voxelMinY = (int)MathHelper.Clamp((invRegionMin.Y + 1.0f) * 0.5f * voxelCountY, 0, voxelCountY - 1);
            int voxelMinZ = (int)MathHelper.Clamp((invRegionMin.Z + 1.0f) * 0.5f * voxelCountZ, 0, voxelCountZ - 1);

            int voxelMaxX = (int)MathHelper.Clamp((invRegionMax.X + 1.0f) * 0.5f * voxelCountX, 0, voxelCountX - 1);
            int voxelMaxY = (int)MathHelper.Clamp((invRegionMax.Y + 1.0f) * 0.5f * voxelCountY, 0, voxelCountY - 1);
            int voxelMaxZ = (int)MathHelper.Clamp((invRegionMax.Z + 1.0f) * 0.5f * voxelCountZ, 0, voxelCountZ - 1);

            availableTriangles = new List<TriangleGraph>();

            for (int z = voxelMinZ; z <= voxelMaxZ; z++)
            {
                for (int y = voxelMinY; y <= voxelMaxY; y++)
                {
                    for (int x = voxelMinX; x <= voxelMaxX; x++)
                    {
                        int voxelIndex = x + (y + z * voxelCountY) * voxelCountX;
                        if (Voxels[voxelIndex].CanRender)
                        {
                            KDTree<TriangleGraph> collisionTree = Voxels[voxelIndex].GetCollisionTree();
                            PerformKDRegionSearch(collisionTree.GetRoot(), ref region, availableTriangles);
                        }
                    }
                }
            }

            return (availableTriangles.Count > 0);
        }

        public void GenerateRandomTransformConnected(Random rand, out Vector3 position, out Vector3 normal)
        {
            int bestY = -1;
            int randX = 0;
            int randZ = 0;
            while(bestY == -1)
            {
                randX = rand.Next(DensityFieldWidth- 1);
                randZ = rand.Next(DensityFieldDepth-1);
                int vX = (int)MathHelper.Clamp(((float)randX / (float)DensityFieldWidth) * VoxelGridSize, 0, VoxelGridSize - 1);
                
                int vZ = (int)MathHelper.Clamp(((float)randZ / (float)DensityFieldDepth) * VoxelGridSize, 0, VoxelGridSize - 1);
                int index = randX + randZ * DensityFieldWidth * DensityFieldHeight;
                int voxelIndex = vX + vZ * VoxelGridSize * VoxelGridSize;
                for (int i = 0; i < DensityFieldHeight; i++)
                {
                    int vY = (int)MathHelper.Clamp(((float)i / (float)DensityFieldHeight) * VoxelGridSize, 0, VoxelGridSize - 1);
                    if (DensityField[index + i * DensityFieldWidth] <= (IsoValue + 10) && Voxels[voxelIndex + vY].CanRender)
                    {
                        SortedList<ulong, TriangleGraph> graph = null;
                        if (Voxels[voxelIndex + vY].GetCollisionNodesAtPoint(out graph, ref DensityField, IsoValue, DensityFieldWidth, DensityFieldHeight, randX, i, randZ))
                        {
                            bestY = i;
                            break;
                        }
                    }
                }
            }

            Vector3 vec = new Vector3((float)randX, 1.0f + (float)bestY, (float)randZ) / new Vector3(DensityFieldWidth - 1, DensityFieldHeight - 1, DensityFieldDepth - 1);
            position = Vector3.Transform(2.0f * vec - Vector3.One, Transformation.GetTransform());
            normal = ComputeNormal(randX, bestY, randZ);
        }

        Vector3 ComputeNormal(int x, int y, int z)
        {
            int sliceArea = DensityFieldWidth * DensityFieldHeight;
            int idx = x + DensityFieldWidth * y + z * sliceArea;
            int x0 = (x - 1 >= 0) ? -1 : 0;
            int x1 = (x + 1 < DensityFieldWidth) ? 1 : 0;
            int y0 = (y - 1 >= 0) ? -DensityFieldWidth : 0;
            int y1 = (y + 1 < DensityFieldHeight) ? DensityFieldWidth : 0;
            int z0 = (z - 1 >= 0) ? -sliceArea : 0;
            int z1 = (z + 1 < DensityFieldDepth) ? sliceArea : 0;

            //Take the negative gradient (hence the x0-x1)
            Vector3 nrm = new Vector3(DensityField[idx + x0] - DensityField[idx + x1], DensityField[idx + y0] - DensityField[idx + y1], DensityField[idx + z0] - DensityField[idx + z1]);

            double magSqr = nrm.X * nrm.X + nrm.Y * nrm.Y + nrm.Z * nrm.Z + 0.0001; //Regularization constant (very important!)
            double invMag = 1.0 / Math.Sqrt(magSqr);
            nrm.X = (float)(nrm.X * invMag);
            nrm.Y = (float)(nrm.Y * invMag);
            nrm.Z = (float)(nrm.Z * invMag);

            return nrm;
        }

        public void GetBlockPos(Vector3 pos, out int xPos, out int yPos, out int zPos)
        {
            Vector3 posObjSpace = Vector3.Transform(pos, Transformation.GetObjectSpace());

            posObjSpace = posObjSpace * 0.5f + Vector3.One * 0.5f;
            posObjSpace *= new Vector3(DensityFieldWidth, DensityFieldHeight, DensityFieldDepth);

            xPos = (int)MathHelper.Clamp(posObjSpace.X, 0, DensityFieldWidth - 1);
            yPos = (int)MathHelper.Clamp(posObjSpace.Y, 0, DensityFieldHeight - 1);
            zPos = (int)MathHelper.Clamp(posObjSpace.Z, 0, DensityFieldDepth - 1);
        }


        void GenerateTerrainFromFile(string filename)
        {
            Texture2D heightMap = Texture2D.FromFile(GFX.Device, filename);

            DensityFieldWidth = heightMap.Width;
            DensityFieldDepth = DensityFieldWidth;
            DensityFieldHeight = ((DensityFieldWidth-1) / 8) + 1;

            InitializeFieldData();

            Color[] heightData = new Color[heightMap.Width * heightMap.Height];
            heightMap.GetData<Color>(heightData);

            int densityFieldSqr = DensityFieldWidth * DensityFieldHeight;

            for (int z = 0; z < DensityFieldDepth; z++)
            {
                int zStride = z * densityFieldSqr;
                for (int x = 0; x < DensityFieldWidth; x++)
                {
                    float h = (float)heightData[x + z * DensityFieldWidth].R / 255.0f;
                    int hEnd = (int)MathHelper.Clamp(h * DensityFieldHeight, 1, DensityFieldHeight - 1);
                    int hInterpStart = hEnd - (int)((float)hEnd * .3f);
                    float invRange = 1.0f / (float)(hEnd - hInterpStart);
                    for (int y = 0; y < hEnd; y++)
                    {
                        int yStride = y * DensityFieldWidth;

                        DensityField[x + yStride + zStride] = 255;
                        //if (y > hInterpStart)
                        //    DensityField[x + yStride + zStride] = (byte)MathHelper.Lerp(255, 0, (float)(y - hInterpStart) * invRange);
                    }
                }
            }

            InitializeSurfaceIndices();

            InitializeVoxels();
        }

        void GenerateFloatingIslands(int size)
        {
            DensityFieldWidth = size + 1;
            DensityFieldHeight = DensityFieldWidth;
            DensityFieldDepth = DensityFieldWidth;

            InitializeFieldData();
            
            //Here we generate our noise textures
            int nSize = 16;
            noiseTextures = new Texture3D[4];
            float[] noiseData = new float[nSize * nSize * nSize];
            Random rand = new Random();
            for (int i = 0; i < noiseTextures.Length; i++)
            {
                noiseTextures[i] = new Texture3D(GFX.Device, nSize, nSize, nSize, 1, TextureUsage.None, SurfaceFormat.Single);
                for (int j = 0; j < noiseData.Length; j++)
                {
                    noiseData[j] = (float)(rand.NextDouble() * 2 - 1);
                }
                noiseTextures[i].SetData<float>(noiseData);
            }

            noiseData = null;

            //The program we'll be using
            Shader islandShader = ResourceManager.Inst.GetShader("ProceduralIsland");
            islandShader.SetupShader();

            GFX.Device.SetPixelShaderConstant(0, Vector3.One / new Vector3(DensityFieldWidth, DensityFieldHeight, DensityFieldDepth));
            //Lets activate our textures
            for (int i = 0; i < noiseTextures.Length; i++)
                GFX.Device.Textures[i] = noiseTextures[i];

            GFX.Device.SetVertexShaderConstant(1, textureMatrix);

            //Set swizzle axis to the z axis
            GFX.Device.SetPixelShaderConstant(1, Vector4.One * 2);
            
            //Here we setup our render target. 
            //This is used to fetch what is rendered to our screen and store it in a texture.
            srcTarget = new RenderTarget2D(GFX.Device, DensityFieldWidth, DensityFieldHeight, 1, GFX.Inst.ByteSurfaceFormat);
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;

            for (int z = 0; z < DensityFieldDepth; z++)
            {
                Vector4 depth = Vector4.One * (float)z / (float)(DensityFieldDepth - 1);
                GFX.Device.SetVertexShaderConstant(0, depth); //Set our current depth
                
                GFX.Device.SetRenderTarget(0, srcTarget);
                GFX.Device.Clear(Color.TransparentBlack);

                GFXPrimitives.Quad.Render();
    
                GFX.Device.SetRenderTarget(0, null);

                //Now the copying stage.
                ExtractDensityTextureData(ref DensityField, z);

            }
            GFX.Device.DepthStencilBuffer = dsOld;
            /*
            for(int z = 0; z < DensityFieldSize; z++)
            {
                for(int y = 0; y < DensityFieldSize; y++)
                {
                    for (int x = 0; x < DensityFieldSize; x++)
                    {
                        int index = x + (y + z * DensityFieldSize) * DensityFieldSize;
                        DensityField[index] = (byte) ((y < 10) ? 255 : 0);
                    }
                }
            }
            */
            InitializeSurfaceIndices();

            InitializeVoxels();
        }

        void ExtractDensityTextureData(ref byte[] byteField, int z)
        {
            //In the lines below, we copy the texture data into the density field buffer
            if (GFX.Inst.ByteSurfaceDataType == GFXTextureDataType.BYTE)
                srcTarget.GetTexture().GetData<byte>(byteField, z * DensityFieldWidth * DensityFieldHeight, DensityFieldWidth * DensityFieldHeight);
            else
            {
                byte[] densityData = new byte[srcTarget.Width * srcTarget.Height];
                switch (GFX.Inst.ByteSurfaceDataType)
                {
                    case GFXTextureDataType.COLOR:
                        Color[] colorData = new Color[densityData.Length];
                        srcTarget.GetTexture().GetData<Color>(colorData);
                        for (int i = 0; i < colorData.Length; i++)
                            densityData[i] = colorData[i].R;
                        Array.Copy(densityData, 0, byteField, z * DensityFieldWidth * DensityFieldHeight, DensityFieldWidth * DensityFieldHeight);
                        break;
                    case GFXTextureDataType.HALFSINGLE:
                        HalfSingle[] hsingData = new HalfSingle[densityData.Length];
                        srcTarget.GetTexture().GetData<HalfSingle>(hsingData);
                        for (int i = 0; i < hsingData.Length; i++)
                            densityData[i] = (byte)(hsingData[i].ToSingle() * 255.0f);
                        Array.Copy(densityData, 0, byteField, z * DensityFieldWidth * DensityFieldHeight, DensityFieldWidth * DensityFieldHeight);
                        break;
                    case GFXTextureDataType.SINGLE:
                        float[] singData = new float[densityData.Length];
                        srcTarget.GetTexture().GetData<float>(singData);
                        for (int i = 0; i < singData.Length; i++)
                            densityData[i] = (byte)(singData[i] * 255.0f);
                        Array.Copy(densityData, 0, byteField, z * DensityFieldWidth * DensityFieldHeight, DensityFieldWidth * DensityFieldHeight);
                        break;
                }
            }
        }

        void InitializeFieldData()
        {
            int fieldSize = DensityFieldWidth * DensityFieldHeight * DensityFieldDepth;
            DensityField = new byte[fieldSize];
        }

        void InitializeSurfaceIndices()
        {
            surfaceIndices = new int[DensityFieldWidth * DensityFieldDepth];
            for (int z = 0; z < DensityFieldDepth; z++)
            {
                int zOff = z * DensityFieldWidth * DensityFieldHeight;
                for (int x = 0; x < DensityFieldWidth; x++)
                {
                    int indexSurface = x + z * DensityFieldWidth;
                    for (int y = DensityFieldHeight - 1; y >= 0; y--)
                    {
                        int index = x + y * DensityFieldWidth + zOff;
                        surfaceIndices[indexSurface] = -1;
                        if (DensityField[index] >= IsoValue)
                        {
                            surfaceIndices[indexSurface] = y;
                            break;
                        }
                    }
                }
            }
        }

        void InitializeVoxels()
        {
            int voxelCountX = (DensityFieldWidth - 1) / VoxelGridSize;
            int voxelCountY = (DensityFieldHeight - 1) / VoxelGridSize;
            int voxelCountZ = (DensityFieldDepth - 1) / VoxelGridSize;
            Voxels = new VoxelGeometry[voxelCountX * voxelCountY * voxelCountZ];
            VoxelBounds = new BoundingBox[Voxels.Length];
            Vector3 ratio = Vector3.One * 2.0f * (float)VoxelGridSize / new Vector3(DensityFieldWidth-1,DensityFieldHeight-1,DensityFieldDepth-1);

            for (int z = 0; z < voxelCountZ; z++)
            {
                int zOff = voxelCountX * voxelCountY * z;
                for (int y = 0; y < voxelCountY; y++)
                {
                    int yOff = voxelCountX * y;

                    for (int x = 0; x < voxelCountX; x++)
                    {
                        int idx = x + yOff + zOff;

                        VoxelBounds[idx] = new BoundingBox(new Vector3(x, y, z) * ratio - Vector3.One, new Vector3(x + 1, y + 1, z + 1) * ratio - Vector3.One);
                        VoxelBounds[idx].Min = Vector3.Transform(VoxelBounds[idx].Min, Transformation.GetTransform());
                        VoxelBounds[idx].Max = Vector3.Transform(VoxelBounds[idx].Max, Transformation.GetTransform());

                        Voxels[idx] = new VoxelGeometry();
                        Voxels[idx].renderElement.Transform = new Matrix[1] { Transformation.GetTransform() };
                        Vector3 geometryRatio = 2.0f*Vector3.One / new Vector3(DensityFieldWidth-1,DensityFieldHeight-1,DensityFieldDepth-1);
                        Voxels[idx].GenerateGeometry(ref DensityField, IsoValue, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, VoxelGridSize, VoxelGridSize, VoxelGridSize, x * VoxelGridSize, y * VoxelGridSize, z * VoxelGridSize, geometryRatio, this.Transformation.GetTransform());
                    }
                }
            }
        }

        void GenerateCollisionMesh(VoxelGeometry geometry)
        {
            List<Vector3> vertices = new List<Vector3>(geometry.verts.Length);
            List<TriangleVertexIndices> indices = new List<TriangleVertexIndices>(geometry.ib.Length / 3);
            Matrix transform = this.Transformation.GetTransform();
            for (int i = 0; i < geometry.verts.Length; i++)
                vertices.Add(Vector3.Transform(geometry.verts[i].Position, transform));
            for (int i = 0; i < geometry.ib.Length; i += 3)
            {
                TriangleVertexIndices tri = new TriangleVertexIndices(geometry.ib[i + 2], geometry.ib[i + 1], geometry.ib[i]);
                indices.Add(tri);
            }

            TriangleMesh collisionMesh = new TriangleMesh(vertices, indices);
            CollisionSkin collision = new CollisionSkin(null);
            collision.AddPrimitive(collisionMesh, (int)MaterialTable.MaterialID.NotBouncyRough);
            scene.GetPhysicsEngine().CollisionSystem.AddCollisionSkin(collision);

            /*
            Octree tree = new Octree(vertices, indices);
            TriangleMeshShape collisionMesh = new TriangleMeshShape(tree);
            */
            /*
            KDTreeTriangles tree = new KDTreeTriangles(indices, vertices);
            TriangleMeshShapeKD collisionMesh = new TriangleMeshShapeKD(tree);
            
            collisionMesh.FlipNormals = true;
            Jitter.Dynamics.RigidBody body = new Jitter.Dynamics.RigidBody(collisionMesh);
            body.IsStatic = true;
            body.Material.KineticFriction = 0.3f;
            body.Material.StaticFriction = 0.6f;
            body.Material.Restitution = 0.13f;
            scene.GetPhysicsEngine().AddBody(body);
            */
        }

        public override BoundingBox GetWorldSpaceBoundsAtPoint(Vector3 point, int size)
        {
            Vector3 offset = TerrainSize * Vector3.One * (float)size / new Vector3(DensityFieldWidth - 1, DensityFieldHeight - 1, DensityFieldDepth - 1);
            BoundingBox bounds = new BoundingBox();
            bounds.Min = point - offset;
            bounds.Max = point + offset;

            return bounds;
        }

        public override void CarveTerrainAtPoint(Vector3 point, int size, int isoBrush)
        {
            List<VoxelGeometry> UpdateVoxels = new List<VoxelGeometry>();
            int DensityFieldSqr = DensityFieldWidth * DensityFieldHeight;

            Vector3 pointObjSpace = Vector3.Transform(point, Transformation.GetObjectSpace()) * 0.5f + Vector3.One * 0.5f;
            pointObjSpace *= new Vector3(DensityFieldWidth, DensityFieldHeight, DensityFieldDepth);

            int xW = (int)MathHelper.Clamp(pointObjSpace.X, 0, DensityFieldWidth - 1);
            int yW = (int)MathHelper.Clamp(pointObjSpace.Y, 0, DensityFieldHeight - 1);
            int zW = (int)MathHelper.Clamp(pointObjSpace.Z, 0, DensityFieldDepth - 1);

            List<int[]> UpdateShifts = new List<int[]>();
            int xStart = (int)MathHelper.Clamp(xW - size, 0, DensityFieldWidth - 1);
            int xEnd = (int)MathHelper.Clamp(xW + size, 0, DensityFieldWidth - 1);
            int yStart = (int)MathHelper.Clamp(yW - size, 0, DensityFieldHeight - 1);
            int yEnd = (int)MathHelper.Clamp(yW + size, 0, DensityFieldHeight - 1);
            int zStart = (int)MathHelper.Clamp(zW - size, 0, DensityFieldDepth - 1);
            int zEnd = (int)MathHelper.Clamp(zW + size, 0, DensityFieldDepth - 1);
            for (int x = xStart; x < xEnd; x++)
            {
                for (int y = yStart; y < yEnd; y++)
                {
                    int yStride = y * DensityFieldWidth;
                    for (int z = zStart; z < zEnd; z++)
                    {
                        int idx = x + yStride + z * DensityFieldSqr;
                        
                        int density = (int)(isoBrush * MathHelper.Clamp(1 - Vector3.Distance(new Vector3(x, y, z), new Vector3(xW, yW, zW)) / (float)size, 0.0f, 1.0f)) + (int)DensityField[idx];
                        DensityField[idx] = (byte)MathHelper.Clamp(density, 0, 255);
                    }
                }
            }

            int voxelCountX = (DensityFieldWidth - 1) / VoxelGridSize;
            int voxelCountY = (DensityFieldHeight - 1) / VoxelGridSize;
            int voxelCountZ = (DensityFieldDepth - 1) / VoxelGridSize;
            int voxelCountSqr = voxelCountX * voxelCountY;

            int bSizeP1 = (int)(size * 1.5f);
            int bSizeN1 = bSizeP1;// brushSize * 2;
            for (int x = xW - bSizeN1; x < xW + bSizeP1; x++)
            {
                for (int y = yW - bSizeN1; y < yW + bSizeP1; y++)
                {
                    for (int z = zW - bSizeN1; z < zW + bSizeP1; z++)
                    {
                        int xV = (int)MathHelper.Clamp((float)voxelCountX * ((float)x / (float)DensityFieldWidth), 0, voxelCountX - 1);
                        int yV = (int)MathHelper.Clamp((float)voxelCountY * ((float)y / (float)DensityFieldHeight), 0, voxelCountY - 1);
                        int zV = (int)MathHelper.Clamp((float)voxelCountZ * ((float)z / (float)DensityFieldDepth), 0, voxelCountZ - 1);
                        int voxelIndex = xV + yV * voxelCountX + zV * voxelCountSqr;
                        if (!UpdateVoxels.Contains(Voxels[voxelIndex]))
                        {
                            UpdateVoxels.Add(Voxels[voxelIndex]);
                            UpdateShifts.Add(new int[] { xV, yV, zV });
                        }
                    }
                }
            }

            Vector3 ratio = 2.0f * (float)VoxelGridSize * Vector3.One / new Vector3(DensityFieldWidth - 1, DensityFieldHeight - 1, DensityFieldDepth - 1);
            for (int i = 0; i < UpdateVoxels.Count; i++)
            {
                UpdateVoxels[i].GenerateGeometry(ref DensityField, IsoValue, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, VoxelGridSize, VoxelGridSize, VoxelGridSize, UpdateShifts[i][0] * VoxelGridSize, UpdateShifts[i][1] * VoxelGridSize, UpdateShifts[i][2] * VoxelGridSize, ratio, this.Transformation.GetTransform());
            }
        }

        public override void OnAdd(Scene scene)
        {
            //for(int i = 0; i < Voxels.Length; i++)
            //    VoxelCollisions[i] = new VoxelCollision(Voxels[i], this.Transformation, VoxelBounds[i], scene);
            base.OnAdd(scene);

            for (int i = 0; i < Voxels.Length; i++)
            {
                if (Voxels[i].CanRender)
                    GenerateCollisionMesh(Voxels[i]);
            }
        }

        public override void OnUpdate()
        {
            //HandleCameraMotion();
           
            base.OnUpdate();
        }

        public override void OnRender(RenderView view)
        {
            BoundingFrustum frustum = view.GetFrustum();
            for (int i = 0; i < Voxels.Length; i++)
            {
                if (frustum.Contains(VoxelBounds[i]) != ContainmentType.Disjoint && Voxels[i].CanRender)
                {
                    view.AddElement(terrainMaterial, Voxels[i].renderElement);
                }
            }
            base.OnRender(view);
        }
    }
}
