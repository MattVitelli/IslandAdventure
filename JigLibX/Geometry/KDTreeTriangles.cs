using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using JigLibX.Math;

namespace JigLibX.Geometry
{
    public class KDNode
    {
        public BoundingBox boundingBox;

        public BoundingBox triangleBox;

        public TriangleVertexIndicesKD element;

        public KDNode leftChild;

        public KDNode rightChild;
    }

    /// <summary>
    /// structure used to set up the mesh
    /// </summary>
    #region public struct TriangleVertexIndices
    public struct TriangleVertexIndicesKD
    {
        /// <summary>
        /// The first index.
        /// </summary>
        public int I0;
        /// <summary>
        /// The second index.
        /// </summary>
        public int I1;
        /// <summary>
        /// The third index.
        /// </summary>
        public int I2;

        /// <summary>
        /// The triangle's centroid/midpoint
        /// </summary>
        public Vector3 Centroid;

        public Vector3 Normal;

        /// <summary>
        /// Initializes a new instance of the TriangleVertexIndex structure.
        /// </summary>
        /// <param name="i0">The index of the first vertex.</param>
        /// <param name="i1">The index of the second vertex.</param>
        /// <param name="i2">The index of the third vertex.</param>
        public TriangleVertexIndicesKD(int i0, int i1, int i2, Vector3 centroid, Vector3 normal)
        {
            this.I0 = i0;
            this.I1 = i1;
            this.I2 = i2;
            this.Centroid = centroid;
            this.Normal = normal;
        }
    }
    #endregion

    public class KDTreeTriangles
    {
        KDNode rootNode;

        int maxDimension = 3;

        List<TriangleVertexIndicesKD> indices;
        List<Vector3> vertices;
        SortedList<ulong, int> triMap;

        public KDTreeTriangles(List<TriangleVertexIndices> indices, List<Vector3> vertices)
        {
            this.rootNode = new KDNode();
            ConvertToKD(indices, vertices);
            ConstructKDTree(rootNode, 0, this.indices.ToArray());
        }

        public KDNode GetRoot()
        {
            return rootNode;
        }

        public TriangleVertexIndicesKD GetTriangle(int index)
        {
            return indices[index];
        }

        public Vector3 GetVertex(int index)
        {
            return vertices[index];
        }

        public IndexedTriangle GetIndexedTriangle(int index)
        {
            TriangleVertexIndicesKD tri = indices[index];
            IndexedTriangle triangle = new IndexedTriangle(tri.I0, tri.I1, tri.I2, vertices);
            return triangle;
        }

        public int NumTriangles { get { return indices.Count; } }

        void ConvertToKD(List<TriangleVertexIndices> indices, List<Vector3> vertices)
        {
            this.vertices = vertices;
            this.indices = new List<TriangleVertexIndicesKD>(indices.Count);
            this.triMap = new SortedList<ulong, int>();
            for (int i = 0; i < indices.Count; i++)
            {
                TriangleVertexIndices currTri = indices[i];
                Vector3[] vecs = new Vector3[3];
                vecs[0] = vertices[currTri.I0];
                vecs[1] = vertices[currTri.I1];
                vecs[2] = vertices[currTri.I2];

                Vector3 sum = vecs[0];
                Vector3.Add(ref sum, ref vecs[1], out sum);
                Vector3.Add(ref sum, ref vecs[2], out sum);
                Vector3.Multiply(ref sum, 1.0f / 3.0f, out sum);

                Vector3 center = sum;
                Vector3 normal;

                Vector3.Subtract(ref vecs[1], ref vecs[0], out sum);
                Vector3.Subtract(ref vecs[2], ref vecs[0], out normal);
                Vector3.Cross(ref sum, ref normal, out normal);

                TriangleVertexIndicesKD newTri = new TriangleVertexIndicesKD(currTri.I0, currTri.I1, currTri.I2, vecs[0], normal);
                this.triMap.Add(GetID(ref newTri), i);
                this.indices.Add(newTri);
                
            }
        }

        ulong GetID(ref TriangleVertexIndicesKD triangle)
        {
            ulong id0 = (ushort)triangle.I0;
            ulong id1 = (ushort)triangle.I1;
            ulong id2 = (ushort)triangle.I2;
            ulong ID = (ulong)id0;
            int shift = sizeof(ushort) * 8;
            ID |= (id1 << shift);
            ID |= (id2 << (shift * 2));
            return ID;
        }

        private static int CompareX(TriangleVertexIndicesKD elemA, TriangleVertexIndicesKD elemB)
        {
            if (elemA.Centroid.X < elemB.Centroid.X)
                return -1;
            if (elemA.Centroid.X > elemB.Centroid.X)
                return 1;

            return 0;
        }

        private static int CompareY(TriangleVertexIndicesKD elemA, TriangleVertexIndicesKD elemB)
        {
            if (elemA.Centroid.Y < elemB.Centroid.Y)
                return -1;
            if (elemA.Centroid.Y > elemB.Centroid.Y)
                return 1;

            return 0;
        }

        private static int CompareZ(TriangleVertexIndicesKD elemA, TriangleVertexIndicesKD elemB)
        {
            if (elemA.Centroid.Z < elemB.Centroid.Z)
                return -1;
            if (elemA.Centroid.Z > elemB.Centroid.Z)
                return 1;

            return 0;
        }

        void ConstructKDTree(KDNode currNode, int depth, TriangleVertexIndicesKD[] entities)
        {
            if (entities.Length == 0)
                return;

            int axis = depth % maxDimension;

            List<TriangleVertexIndicesKD> sortedList = new List<TriangleVertexIndicesKD>();
            sortedList.AddRange(entities);
            switch (axis)
            {
                case 0:
                    sortedList.Sort(CompareX);
                    break;
                case 1:
                    sortedList.Sort(CompareY);
                    break;
                case 2:
                    sortedList.Sort(CompareZ);
                    break;
            }

            BoundingBox bounds = new BoundingBox(vertices[sortedList[0].I0], vertices[sortedList[0].I0]);
            Vector3[] vecs = new Vector3[3];
            for (int i = 0; i < sortedList.Count; i++)
            {
                vecs[0] = vertices[sortedList[i].I0];
                vecs[1] = vertices[sortedList[i].I1];
                vecs[2] = vertices[sortedList[i].I2];
                for (int j = 0; j < vecs.Length; j++)
                {
                    bounds.Max = Vector3.Max(bounds.Max, vecs[j]);
                    bounds.Min = Vector3.Min(bounds.Min, vecs[j]);
                }
            }

            int medianIndex = sortedList.Count / 2;

            vecs[0] = vertices[sortedList[medianIndex].I0];
            vecs[1] = vertices[sortedList[medianIndex].I1];
            vecs[2] = vertices[sortedList[medianIndex].I2];
            BoundingBox triBounds = new BoundingBox(vecs[0], vecs[0]);
            for (int j = 0; j < vecs.Length; j++)
            {
                triBounds.Max = Vector3.Max(triBounds.Max, vecs[j]);
                triBounds.Min = Vector3.Min(triBounds.Min, vecs[j]);
            }

            float extra = 1.000001f;
            bounds.Min = bounds.Min * extra;
            bounds.Max = bounds.Max * extra;
            triBounds.Min = triBounds.Min * extra;
            triBounds.Max = triBounds.Max * extra;

            if (sortedList.Count > 1)
            {
                depth++;
                int leftCount = medianIndex;
                if (leftCount > 0)
                {
                    TriangleVertexIndicesKD[] leftEntities = new TriangleVertexIndicesKD[leftCount];
                    sortedList.CopyTo(0, leftEntities, 0, leftCount);
                    currNode.leftChild = new KDNode();
                    ConstructKDTree(currNode.leftChild, depth, leftEntities);
                }

                int rightCount = sortedList.Count - (medianIndex + 1);
                if (rightCount > 0)
                {
                    TriangleVertexIndicesKD[] rightEntities = new TriangleVertexIndicesKD[rightCount];
                    sortedList.CopyTo(medianIndex + 1, rightEntities, 0, rightCount);
                    currNode.rightChild = new KDNode();
                    ConstructKDTree(currNode.rightChild, depth, rightEntities);
                }
            }
            currNode.element = sortedList[medianIndex];
            currNode.boundingBox = bounds;
            currNode.triangleBox = triBounds;
        }

        unsafe void GetTrianglesIntersectingAABox(KDNode node, int *triangles, ref int triCount, ref BoundingBox testBox)
        {
            if (node.boundingBox.Contains(testBox) != ContainmentType.Disjoint)
            {
                if (node.triangleBox.Contains(testBox) != ContainmentType.Disjoint)
                {
                    ulong ID = GetID(ref node.element);
                    triangles[triCount] = triMap[ID];//.Add(node.element);
                    triCount++;
                }
                if (node.leftChild != null)
                    GetTrianglesIntersectingAABox(node.leftChild, triangles, ref triCount, ref testBox);
                if (node.rightChild != null)
                    GetTrianglesIntersectingAABox(node.rightChild, triangles, ref triCount, ref testBox);
            }
        }

        public unsafe int GetTrianglesIntersectingtAABox(int *triangles, int maxTriangles, ref BoundingBox testBox)
        {
            int triCount = 0;
            if (rootNode != null)
                GetTrianglesIntersectingAABox(rootNode, triangles, ref triCount, ref testBox);

            return triCount;
        }
    }
}
