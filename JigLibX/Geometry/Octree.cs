#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
using System.Diagnostics;
#endregion

namespace JigLibX.Geometry
{
    public class Octree
    {
        /// <summary>
        /// endices into the children - P means "plus" and M means "minus" and the
        /// letters are xyz. So PPM means +ve x, +ve y, -ve z
        /// </summary>
        [Flags]
        internal enum EChild
        {
            XP = 0x1,
            YP = 0x2,
            ZP = 0x4,
            PPP = XP | YP | ZP,
            PPM = XP | YP,
            PMP = XP |      ZP,
            PMM = XP ,
            MPP =      YP | ZP,
            MPM =      YP,
            MMP =           ZP,
            MMM = 0x0,
        }

        struct Node
        {
            public UInt16[] nodeIndices;
            public int[]        triIndices;
            public BoundingBox  box;
        }
        class BuildNode
        {
            public int childType; // will default to MMM (usually ECHild but can also be -1)
            public List<int> nodeIndices = new List<int>();
            public List<int> triIndices = new List<int>();
            public BoundingBox box;
        };
        Vector3[]               positions;
        BoundingBox[]           triBoxes;
        TriangleVertexIndices[] tris;
        Node[]                  nodes;
        BoundingBox             rootNodeBox;
        AABox                   boundingBox;
        UInt16[]                nodeStack;

        // to make compat with old Octree interface
        public Octree()
        {
        }
        public void Clear( bool NOTUSED )
        {
            positions = null;
            triBoxes = null;
            tris = null;
            nodes = null;
            boundingBox = null;
        }
        public void AddTriangles(Vector3[] _positions, TriangleVertexIndices[] _tris)
        {
            // copy the position data into a array
            //positions = new Vector3[_positions.Count];
            positions = _positions;

            // copy the triangles
            //tris = new TriangleVertexIndices[_tris.Count];
            tris = _tris;
        }

        public void BuildOctree( int _maxTrisPerCellNOTUSED, float _minCellSizeNOTUSED)
        {
            // create tri and tri bounding box arrays
            triBoxes = new BoundingBox[tris.Length];

            // create an infinite size root box
            rootNodeBox = new BoundingBox(new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                                           new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity));


            for (int i = 0; i < tris.Length; i++)
            {
                triBoxes[i].Min = Vector3.Min(positions[tris[i].I0], Vector3.Min(positions[tris[i].I1], positions[tris[i].I2]));
                triBoxes[i].Max = Vector3.Max(positions[tris[i].I0], Vector3.Max(positions[tris[i].I1], positions[tris[i].I2]));

                // get size of the root box
                rootNodeBox.Min = Vector3.Min(rootNodeBox.Min, triBoxes[i].Min);
                rootNodeBox.Max = Vector3.Max(rootNodeBox.Max, triBoxes[i].Max);
            }

            boundingBox = new AABox(rootNodeBox.Min, rootNodeBox.Max);

            List<BuildNode> buildNodes = new List<BuildNode>();
            buildNodes.Add(new BuildNode());
            buildNodes[0].box = rootNodeBox;

            BoundingBox[] children = new BoundingBox[8];
            for (int triNum = 0; triNum < tris.Length; triNum++)
            {
                int nodeIndex = 0;
                BoundingBox box = rootNodeBox;
                int iter = 0;
                const int MAX_ITER = 9999;
                while (box.Contains(triBoxes[triNum]) == ContainmentType.Contains && iter <= MAX_ITER)
                {
                    iter++;
                    int childCon = -1;
                    for (int i = 0; i < 8; ++i)
                    {
                        children[i] = CreateAABox(box, (EChild)i);
                        if (children[i].Contains(triBoxes[triNum]) == ContainmentType.Contains)
                        {
                            // this box contains the tri, it can be the only one that does,
                            // so we can stop our child search now and recurse into it
                            childCon = i;
                            break;
                        }
                    }

                    // no child contains this tri completely, so it belong in this node
                    if (childCon == -1)
                    {
                        buildNodes[nodeIndex].triIndices.Add(triNum);
                        break;
                    }
                    else
                    {
                        // do we already have this child
                        int childIndex = -1;
                        for (int index = 0; index < buildNodes[nodeIndex].nodeIndices.Count; ++index)
                        {
                            if (buildNodes[buildNodes[nodeIndex].nodeIndices[index]].childType == childCon)
                            {
                                childIndex = index;
                                break;
                            }
                        }
                        if (childIndex == -1)
                        {
                            // nope create child
                            BuildNode parentNode = buildNodes[nodeIndex];
                            BuildNode newNode = new BuildNode();
                            newNode.childType = childCon;
                            newNode.box = children[childCon];
                            buildNodes.Add(newNode);

                            nodeIndex = buildNodes.Count - 1;
                            box = children[childCon];
                            parentNode.nodeIndices.Add(nodeIndex);
                        }
                        else
                        {
                            nodeIndex = buildNodes[nodeIndex].nodeIndices[childIndex];
                            box = children[childCon];
                        }
                    }
                }
            }

            //Debug.Assert(buildNodes.Count < 0xFFFF);
            
            // now convert tosd the tighter Node from BuildNodes
            if (buildNodes.Count > 0)
            {
                nodes = new Node[buildNodes.Count];
                nodeStack = new UInt16[buildNodes.Count];
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i].nodeIndices = new UInt16[buildNodes[i].nodeIndices.Count];
                    for (int index = 0; index < nodes[i].nodeIndices.Length; ++index)
                    {
                        nodes[i].nodeIndices[index] = (UInt16)buildNodes[i].nodeIndices[index];
                    }

                    nodes[i].triIndices = new int[buildNodes[i].triIndices.Count];
                    buildNodes[i].triIndices.CopyTo(nodes[i].triIndices);
                    nodes[i].box = buildNodes[i].box;
                }
            }
            buildNodes = null;

        }

        public Octree(List<Vector3> _positions, List<TriangleVertexIndices> _tris)
        {
            AddTriangles(_positions.ToArray(), _tris.ToArray());
            BuildOctree( 16, 1.0f );
        }
        /// <summary>
        /// Create a bounding box appropriate for a child, based on a parents AABox
        /// </summary>
        /// <param name="aabb"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        private BoundingBox CreateAABox(BoundingBox aabb, EChild child)
        {
            Vector3 dims = 0.5f * (aabb.Max - aabb.Min);
            Vector3 offset = new Vector3();

            switch (child)
            {
                case EChild.PPP: offset = new Vector3(1, 1, 1); break;
                case EChild.PPM: offset = new Vector3(1, 1, 0); break;
                case EChild.PMP: offset = new Vector3(1, 0, 1); break;
                case EChild.PMM: offset = new Vector3(1, 0, 0); break;
                case EChild.MPP: offset = new Vector3(0, 1, 1); break;
                case EChild.MPM: offset = new Vector3(0, 1, 0); break;
                case EChild.MMP: offset = new Vector3(0, 0, 1); break;
                case EChild.MMM: offset = new Vector3(0, 0, 0); break;

                default:
                    System.Diagnostics.Debug.WriteLine("Octree.CreateAABox  got impossible child");
                    //TRACE("tOctree::CreateAABox Got impossible child: %d", child);
                    //offset.Set(0, 0, 0);
                    break;
            }

            BoundingBox result = new BoundingBox();
            result.Min = (aabb.Min + new Vector3(offset.X * dims.X, offset.Y * dims.Y, offset.Z * dims.Z));
            result.Max = (result.Min + dims);

            // expand it just a tiny bit just to be safe!
            float extra = 0.00001f;

            result.Min = (result.Min - extra * dims);
            result.Max = (result.Max + extra * dims);

            return result;
        }
        private void GatherTriangles(int _nodeIndex, ref List<int> _tris)
        {
            // add this nodes triangles
            _tris.AddRange(nodes[_nodeIndex].triIndices);

            // recurse into this nodes children
            int numChildren = nodes[_nodeIndex].nodeIndices.Length;
            for (int i = 0; i < numChildren; ++i)
            {
                int childNodeIndex = nodes[_nodeIndex].nodeIndices[i];
                GatherTriangles(childNodeIndex, ref _tris);
            }
        }


        public unsafe int GetTrianglesIntersectingtAABox(int* triangles, int maxTriangles, ref BoundingBox testBox)
        {
            if (nodes.Length == 0)
                return 0;
            int curStackIndex = 0;
            int endStackIndex = 1;
            nodeStack[0] = 0;

            int triCount = 0;
            while (curStackIndex < endStackIndex)
            {
                UInt16 nodeIndex = nodeStack[curStackIndex];
                curStackIndex++;
                if (nodes[nodeIndex].box.Contains(testBox) != ContainmentType.Disjoint)
                {
                    for (int i = 0; i < nodes[nodeIndex].triIndices.Length; ++i)
                    {
                        if (triBoxes[nodes[nodeIndex].triIndices[i]].Contains(testBox) != ContainmentType.Disjoint)
                        {
                            if (triCount < maxTriangles)
                            {
                                triangles[triCount++] = nodes[nodeIndex].triIndices[i];
                            }
                        }
                    }

                    int numChildren = nodes[nodeIndex].nodeIndices.Length;
                    for (int i = 0; i < numChildren; ++i)
                    {
                        nodeStack[endStackIndex++] = nodes[nodeIndex].nodeIndices[i];
                    }
                }
            }
            return triCount;
        }

        public AABox BoundingBox
        {
            get
            {
                return boundingBox;
            }
        }
        public IndexedTriangle GetTriangle(int _index)
        {
            return new IndexedTriangle(tris[_index].I0, tris[_index].I1, tris[_index].I2, positions);
        }
        /// <summary>
        /// Get a vertex
        /// </summary>
        /// <param name="iVertex"></param>
        /// <returns></returns>
        public Vector3 GetVertex(int iVertex)
        {
            return positions[iVertex];
        }

        public void GetVertex(int iVertex, out Vector3 result)
        {
            result = positions[iVertex];
        }

        /// <summary>
        /// Gets the number of triangles
        /// </summary>
        public int NumTriangles
        {
            get { return tris.Length; }
        }


    }
#if OLD_OCTREE
    /// <summary>
    /// Stores world collision data in an octree structure for quick ray testing
    /// during CVolumeNavRegion processing.
    /// </summary>
    public class Octree
    {

        #region Octree Cell
        /// <summary>
        /// Internally we don't store pointers but store indices into a single contiguous
        /// array of cells and triangles owned by Octree (so that the vectors can get resized).
        ///
        /// Each cell will either contain children OR contain triangles. 
        /// </summary>
        struct Cell
        {
            /// <summary>
            /// endices into the children - P means "plus" and M means "minus" and the
            /// letters are xyz. So PPM means +ve x, +ve y, -ve z
            /// </summary>
            internal enum EChild
            {
                PPP,
                PPM,
                PMP,
                PMM,
                MPP,
                MPM,
                MMP,
                MMM,
                NumChildren
            }

            /// indices of the children (if not leaf). Will be -1 if there is no child
            internal int[] mChildCellIndices;

            /// indices of the triangles (if leaf)
            internal List<int> mTriangleIndices;

            /// Bounding box for the space we own
            internal AABox mAABox;

            /// <summary>
            /// constructor clears everything
            /// </summary>
            /// <param name="aabb"></param>
            public Cell(AABox aabb)
            {
                mAABox = aabb;
                mTriangleIndices = new List<int>();
                mChildCellIndices = new int[NumChildren];

                Clear();
            }

            /// <summary>
            /// Sets all child indices to -1 and clears the triangle indices.
            /// </summary>
            public void Clear()
            {
                for (int i = 0; i < NumChildren; i++)
                    mChildCellIndices[i] = -1;

                mTriangleIndices.Clear();

            }

            /// <summary>
            /// constructor clears everything
            /// </summary>
            public bool IsLeaf
            {
                get { return mChildCellIndices[0] == -1; }
            }


        }
        #endregion

        #region private fields

        private const int NumChildren = (int)Cell.EChild.NumChildren;

        /// All our cells. The only thing guaranteed about this is that m_cell[0] (if
        /// it exists) is the root cell.
        private List<Octree.Cell> cells;

        /// the vertices
        private List<Vector3> vertices;
        /// All our triangles.
        private List<IndexedTriangle> triangles;

        private AABox boundingBox = new AABox();

        /// During intersection testing we keep a stack of cells to test (rather than recursing) - 
        /// to avoid excessive memory allocation we don't free the memory between calls unless
        /// the user calls FreeTemporaryMemory();
        private Stack<int> mCellsToTest;

        /// Counter used to prevent multiple tests when triangles are contained in more than
        /// one cell
        private int testCounter;

        #endregion

        /// <summary>
        /// On creation the extents are defined - if anything is subsequently added
        /// that lies entirely outside this bbox it will not get added.
        /// </summary>
        public Octree()
        {
            cells = new List<Cell>();
            vertices = new List<Vector3>();
            triangles = new List<IndexedTriangle>();
            mCellsToTest = new Stack<int>();
        }

        /// <summary>
        /// Clears triangles and cells. If freeMemory is set to true, the
        /// triangle index array will be freed, otherwise it will be reset
        /// preserving the allocated memory.
        /// </summary>
        /// <param name="freeMemory"></param>
        public void Clear(bool freeMemory)
        {
            cells.Clear();
            vertices.Clear();
            triangles.Clear();
        }

        public AABox BoundingBox
        {
            get { return this.boundingBox; }
        }

        /// <summary>
        /// Add the triangles - doesn't actually build the octree
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="numVertices"></param>
        /// <param name="triangleVertexIndices"></param>
        /// <param name="numTriangles"></param>
        public void AddTriangles(List<Vector3> vertices, List<TriangleVertexIndices> triangleVertexIndices)
        {
            NewOctree test = new NewOctree(vertices, triangleVertexIndices);

            this.vertices.Clear();
            this.triangles.Clear();
            this.cells.Clear();

            int numTriangles = triangleVertexIndices.Count;

            this.vertices = vertices;

            for (int iTriangle = 0; iTriangle < numTriangles; iTriangle++)
            {
                int i0 = triangleVertexIndices[iTriangle].I0;
                int i1 = triangleVertexIndices[iTriangle].I1;
                int i2 = triangleVertexIndices[iTriangle].I2;

                //Assert(i0 < numVertices);
                //Assert(i1 < numVertices);
                //Assert(i2 < numVertices);

                Vector3 dr1 = vertices[i1] - vertices[i0];
                Vector3 dr2 = vertices[i2] - vertices[i0];
                Vector3 N = Vector3.Cross(dr1, dr2);

                float NLen = N.Length();

                // only add if it's not degenerate. Note that this could be a problem it we use connectivity info
                // since we're actually making a hole in the mesh...
                if (NLen > JiggleMath.Epsilon)
                {
                    IndexedTriangle tri = new IndexedTriangle();
                    tri.SetVertexIndices(i0, i1, i2, vertices);

                    triangles.Add(tri);

                    //mTriangles.back().SetVertexIndices(i0, i1, i2, vertices);
                }
            }
        }

        /// <summary>
        /// Builds the octree from scratch (not incrementally) - deleting
        /// any previous tree.  Building the octree will involve placing
        /// all triangles into the root cell.  Then this cell gets pushed
        /// onto a stack of cells to examine. This stack will get parsed
        /// and every cell containing more than maxTrianglesPerCell will
        /// get split into 8 children, and all the original triangles in
        /// that cell will get partitioned between the children. A
        /// triangle can end up in multiple cells (possibly a lot!) if it
        /// straddles a boundary. Therefore when intersection tests are
        /// done tIndexedTriangle::m_counter can be set/tested using a
        /// counter to avoid properly testing the triangle multiple times
        /// (the counter _might_ wrap around, so when it wraps ALL the
        /// triangle flags should be cleared! Could do this
        /// incrementally...).
        /// </summary>
        /// <param name="maxTrianglesPerCell"></param>
        /// <param name="minCellSize"></param>
        public void BuildOctree(int maxTrianglesPerCell, float minCellSize)
        {
            boundingBox.Clear();

            for (int i = 0; i < vertices.Count; i++)
                boundingBox.AddPoint(vertices[i]);

            // clear any existing cells
            cells.Clear();

            // set up the root

            Octree.Cell rootCell = new Octree.Cell(boundingBox);

            cells.Add(rootCell);
            int numTriangles = triangles.Count;

            //rootCell.mTriangleIndices.resize(numTriangles);

            for (int i = 0; i < numTriangles; i++)
                rootCell.mTriangleIndices.Add(i);

            // rather than doing things recursively, use a stack of cells that need
            // to be processed - for each cell if it contains too many triangles we 
            // create child cells and move the triangles down into them (then we
            // clear the parent triangles).
            Stack<int> cellsToProcess = new Stack<int>();
            cellsToProcess.Push(0);

            // bear in mind during this that any time a new cell gets created any pointer
            // or reference to an existing cell may get invalidated - so use indexing.
            while (cellsToProcess.Count != 0)
            {
                int cellIndex = cellsToProcess.Pop();

                if ((cells[cellIndex].mTriangleIndices.Count <= maxTrianglesPerCell) ||
                     (cells[cellIndex].mAABox.GetRadiusAboutCentre() < minCellSize))
                    continue;

                // we need to put these triangles into the children
                for (int iChild = 0; iChild < NumChildren; iChild++)
                {
                    cells[cellIndex].mChildCellIndices[iChild] = cells.Count;
                    cellsToProcess.Push(cells.Count);

                    Octree.Cell childCell = new Octree.Cell(CreateAABox(cells[cellIndex].mAABox, (Octree.Cell.EChild)iChild));

                    cells.Add(childCell);

                    int numTris = cells[cellIndex].mTriangleIndices.Count;

                    for (int i = 0; i < numTris; i++)
                    {
                        int iTri = cells[cellIndex].mTriangleIndices[i];
                        IndexedTriangle tri = triangles[iTri];

                        if (DoesTriangleIntersectCell(tri, childCell))
                        {
                            childCell.mTriangleIndices.Add(iTri);
                        }
                    }
                }

                // the children handle all the triangles now - we no longer need them
                cells[cellIndex].mTriangleIndices.Clear();
            }
        }

        /// <summary>
        /// Create a bounding box appropriate for a child, based on a parents AABox
        /// </summary>
        /// <param name="aabb"></param>
        /// <param name="child"></param>
        /// <returns></returns>
        private AABox CreateAABox(AABox aabb, Octree.Cell.EChild child)
        {
            Vector3 dims = 0.5f * (aabb.MaxPos - aabb.MinPos);
            Vector3 offset= new Vector3();

            switch (child)
            {
                case Octree.Cell.EChild.PPP: offset = new Vector3(1, 1, 1); break;
                case Octree.Cell.EChild.PPM: offset = new Vector3(1, 1, 0); break;
                case Octree.Cell.EChild.PMP: offset = new Vector3(1, 0, 1); break;
                case Octree.Cell.EChild.PMM: offset = new Vector3(1, 0, 0); break;
                case Octree.Cell.EChild.MPP: offset = new Vector3(0, 1, 1); break;
                case Octree.Cell.EChild.MPM: offset = new Vector3(0, 1, 0); break;
                case Octree.Cell.EChild.MMP: offset = new Vector3(0, 0, 1); break;
                case Octree.Cell.EChild.MMM: offset = new Vector3(0, 0, 0); break;

                default:
                    System.Diagnostics.Debug.WriteLine("Octree.CreateAABox  got impossible child");
                    //TRACE("tOctree::CreateAABox Got impossible child: %d", child);
                    //offset.Set(0, 0, 0);
                    break;
            }

            AABox result = new AABox();
            result.MinPos = (aabb.MinPos + new Vector3(offset.X * dims.X, offset.Y * dims.Y, offset.Z * dims.Z));
            result.MaxPos = (result.MinPos + dims);

            // expand it just a tiny bit just to be safe!
            float extra = 0.00001f;

            result.MinPos = (result.MinPos - extra * dims);
            result.MaxPos = (result.MaxPos + extra * dims);

            return result;
        }

        public int GetTrianglesIntersectingtAABox(List<int> triangles, AABox aabb)
        {
            if (cells.Count == 0)
                return 0;

            triangles.Clear();
            mCellsToTest.Clear();
            mCellsToTest.Push(0);

            IncrementTestCounter();

            while (mCellsToTest.Count != 0) // while it is not empty
            {
                int cellIndex = mCellsToTest.Pop();
                //mCellsToTest.pop_back();

                Octree.Cell cell = cells[cellIndex];

                if (!AABox.OverlapTest(aabb, cell.mAABox))
                    continue;

                if (cell.IsLeaf)
                {
                    int nTris = cell.mTriangleIndices.Count;

                    for (int i = 0; i < nTris; i++)
                    {
                        IndexedTriangle triangle = GetTriangle(cell.mTriangleIndices[i]);

                        if (triangle.counter != testCounter)
                        {
                            triangle.counter = testCounter;

                            if (AABox.OverlapTest(aabb, triangle.BoundingBox))
                                triangles.Add(cell.mTriangleIndices[i]);
                        }
                    }
                }
                else
                {
                    // if non-leaf, just add the children to check
                    for (int iChild = 0; iChild < Octree.NumChildren; iChild++)
                    {
                        int childIndex = cell.mChildCellIndices[iChild];
                        mCellsToTest.Push(childIndex);
                    }
                }
            }
            return triangles.Count;
        }

        private bool DoesTriangleIntersectCell(IndexedTriangle triangle, Octree.Cell cell)
        {
            if (!AABox.OverlapTest(triangle.BoundingBox, cell.mAABox))
                return false;

            // quick test
            if (cell.mAABox.IsPointInside(GetVertex(triangle.GetVertexIndex(0))) ||
                cell.mAABox.IsPointInside(GetVertex(triangle.GetVertexIndex(1))) ||
                cell.mAABox.IsPointInside(GetVertex(triangle.GetVertexIndex(2))))
                return true;

            // all points are outside... so if there is intersection it must be due to the
            // box edges and the triangle...
            Triangle tri = new Triangle(GetVertex(triangle.GetVertexIndex(0)), GetVertex(triangle.GetVertexIndex(1)), GetVertex(triangle.GetVertexIndex(2)));

            Box box = new Box(cell.mAABox.MinPos, Matrix.Identity, cell.mAABox.GetSideLengths());
            Vector3[] pts;// = new Vector3[8];

            box.GetCornerPoints(out pts);
            Box.Edge[] edges;
            box.GetEdges(out edges);

            for (int i = 0; i < 12; i++)
            {
                Box.Edge edge = edges[i];

                Segment seg = new Segment(pts[(int)edge.Ind0], pts[(int)edge.Ind1] - pts[(int)edge.Ind0]);

                if (Overlap.SegmentTriangleOverlap(seg, tri))
                    return true;
            }
            // Unless it's the triangle edges and the box
            //Vector3 pos, n;

            // now each edge of the triangle with the box
            for (int iEdge = 0; iEdge < 3; ++iEdge)
            {
                Vector3 pt0 = tri.GetPoint(iEdge);
                Vector3 pt1 = tri.GetPoint((iEdge + 1) % 3);

                if (Overlap.SegmentAABoxOverlap(new Segment(pt0, pt1 - pt0), cell.mAABox))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Increment our test counter, wrapping around if necessary and zapping the 
        /// triangle counters.
        /// </summary>
        private void IncrementTestCounter()
        {
            ++testCounter;

            if (testCounter == 0)
            {
                // wrap around - clear all the triangle counters
                int numTriangles = triangles.Count;

                for (int i = 0; i < numTriangles; ++i)
                    triangles[i].counter = 0;

                testCounter = 1;
            }
        }

        /// <summary>
        /// Get a triangle
        /// </summary>
        /// <param name="iTriangle"></param>
        /// <returns></returns>
        public IndexedTriangle GetTriangle(int iTriangle)
        {
            return triangles[iTriangle];
        }

        /// <summary>
        /// Get a vertex
        /// </summary>
        /// <param name="iVertex"></param>
        /// <returns></returns>
        public Vector3 GetVertex(int iVertex)
        {
            return vertices[iVertex];
        }

        public void GetVertex(int iVertex,out Vector3 result)
        {
            result = vertices[iVertex];
        }

        /// <summary>
        /// Gets the number of triangles
        /// </summary>
        public int NumTriangles
        {
            get { return triangles.Count; }
        }



    }
#endif
}
