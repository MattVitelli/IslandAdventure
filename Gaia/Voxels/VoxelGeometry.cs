using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Core;
using Gaia.Rendering;

namespace Gaia.Voxels
{
    public class VoxelGeometry
    {
        public VertexPN[] verts = null;
        public ushort[] ib = null;
        int PrimitiveCount = 0;
        SortedList<int, List<TriangleGraph>> collisionGraph = new SortedList<int, List<TriangleGraph>>();

        KDTree<TriangleGraph> collisionTree = new KDTree<TriangleGraph>(CompareCollisionNodesKD);

        public KDTree<TriangleGraph> GetCollisionTree()
        {
            return collisionTree;
        }

        public RenderElement renderElement;

        public bool CanRender = false;

        public VoxelGeometry()
        {
            InitializeRenderElement();
        }

        void InitializeRenderElement()
        {
            renderElement = new RenderElement();
            renderElement.VertexDec = GFXVertexDeclarations.PNDec;
            renderElement.VertexStride = VertexPN.SizeInBytes;
            renderElement.StartVertex = 0;
        }

        void DestroyBuffers()
        {
            if (renderElement.VertexBuffer != null)
                renderElement.VertexBuffer.Dispose();
            renderElement.VertexBuffer = null;

            if (renderElement.IndexBuffer != null)
                renderElement.IndexBuffer.Dispose();
            renderElement.IndexBuffer = null;

            verts = null;
            ib = null;
            PrimitiveCount = 0;

            CanRender = false;
        }

        static int CompareCollisionNodesKD(TriangleGraph nodeA, TriangleGraph nodeB, int axis)
        {
            float nodeAValue = (axis == 0) ? nodeA.Centroid.X : (axis == 1) ? nodeA.Centroid.Y : nodeA.Centroid.Z;
            
            float nodeBValue = (axis == 0) ? nodeB.Centroid.X : (axis == 1) ? nodeB.Centroid.Y : nodeB.Centroid.Z;

            if (nodeAValue < nodeBValue)
                return -1;
            if (nodeAValue > nodeBValue)
                return 1;

            return 0;
        }

        public bool GetCollisionNodesAtPoint(out SortedList<ulong, TriangleGraph> nodes, ref byte[] DensityField, byte IsoValue, int DensityFieldWidth, int DensityFieldHeight, int x, int y, int z)
        {
            if (CanRender)
            {
                int cubeindex = 0;
                int sliceArea = DensityFieldWidth * DensityFieldHeight;
                int idx = x + y * DensityFieldWidth + z * sliceArea;
                if (DensityField[idx] > IsoValue) cubeindex |= 1;
                if (DensityField[idx + 1] > IsoValue) cubeindex |= 2;
                if (DensityField[idx + 1 + DensityFieldWidth] > IsoValue) cubeindex |= 4;
                if (DensityField[idx + DensityFieldWidth] > IsoValue) cubeindex |= 8;
                if (DensityField[idx + sliceArea] > IsoValue) cubeindex |= 16;
                if (DensityField[idx + 1 + sliceArea] > IsoValue) cubeindex |= 32;
                if (DensityField[idx + 1 + sliceArea + DensityFieldWidth] > IsoValue) cubeindex |= 64;
                if (DensityField[idx + sliceArea + DensityFieldWidth] > IsoValue) cubeindex |= 128;

                /* Cube is entirely in/out of the surface */
                if (cubeindex != 0 && cubeindex != 255)
                {
                    nodes = new SortedList<ulong, TriangleGraph>();
                    for (int i = 0; VoxelHelper.NewTriangleTable2[cubeindex, i] != -1; i += 3)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int edgeID = GetEdgeId(DensityFieldWidth, DensityFieldHeight, x, y, z, VoxelHelper.NewTriangleTable2[cubeindex, i + j]);
                            if (collisionGraph.ContainsKey(edgeID))
                            {
                                for (int k = 0; k < collisionGraph[edgeID].Count; k++)
                                {
                                    ulong graphID = collisionGraph[edgeID][k].ID;
                                    if (!nodes.ContainsKey(graphID))
                                        nodes.Add(graphID, collisionGraph[edgeID][k]);
                                }
                            }
                        }
                    }
                    return true;
                }
            }

            nodes = null;
            return false;

        }

        static int TriangleCompareFunction(TriangleGraph elementA, TriangleGraph elementB, int axis)
        {
            float valueA = (axis == 0) ? elementA.Centroid.X : (axis == 1) ? elementA.Centroid.Y : elementA.Centroid.Z;

            float valueB = (axis == 0) ? elementB.Centroid.X : (axis == 1) ? elementB.Centroid.Y : elementB.Centroid.Z;

            if (valueA < valueB)
                return -1;
            if (valueA > valueB)
                return 1;
            
            return 0;
        }

        public void GenerateGeometry(ref byte[] DensityField, byte IsoValue, int DensityFieldWidth, int DensityFieldHeight, int DensityFieldDepth, int Width, int Height, int Depth, int xOrigin, int yOrigin, int zOrigin, float ratio, Matrix transform)
        {
            DestroyBuffers();
            
            List<VertexPN> _vertices = new List<VertexPN>();
            List<ushort> _indices = new List<ushort>();
            SortedList<int, ushort> _edgeToIndices = new SortedList<int, ushort>();
            collisionGraph.Clear();

            int width = DensityFieldWidth;
            int sliceArea = width * DensityFieldHeight; 

            byte[] DensityCache = new byte[8];
            Vector3[] VectorCache = new Vector3[8];
            for (int z = zOrigin; z < zOrigin + Depth; z++)
            {
                for (int y = yOrigin; y < yOrigin + Height; y++)
                {

                    int dIdx = z * sliceArea + y * width + xOrigin;

                    VectorCache[0] = new Vector3(xOrigin, y, z) * ratio - Vector3.One;
                    VectorCache[3] = new Vector3(xOrigin, y + 1, z) * ratio - Vector3.One;
                    VectorCache[4] = new Vector3(xOrigin, y, z + 1) * ratio - Vector3.One;
                    VectorCache[7] = new Vector3(xOrigin, y + 1, z + 1) * ratio - Vector3.One;
                    DensityCache[0] = DensityField[dIdx];
                    DensityCache[3] = DensityField[dIdx + width];
                    DensityCache[4] = DensityField[dIdx + sliceArea];
                    DensityCache[7] = DensityField[dIdx + sliceArea + width];

                    for (int x = xOrigin + 1; x <= xOrigin + Width; x++)
                    {
                        dIdx = z * sliceArea + y * width + x;

                        VectorCache[1] = new Vector3(x, y, z) * ratio - Vector3.One;
                        VectorCache[2] = new Vector3(x, y + 1, z) * ratio - Vector3.One;
                        VectorCache[5] = new Vector3(x, y, z + 1) * ratio - Vector3.One;
                        VectorCache[6] = new Vector3(x, y + 1, z + 1) * ratio - Vector3.One;
                        DensityCache[1] = DensityField[dIdx];
                        DensityCache[2] = DensityField[dIdx + width];
                        DensityCache[5] = DensityField[dIdx + sliceArea];
                        DensityCache[6] = DensityField[dIdx + sliceArea + width];
                        /*
                           Determine the index into the edge table which
                           tells us which vertices are inside of the surface
                        */
                        int cubeindex = 0;
                        if (DensityCache[0] > IsoValue) cubeindex |= 1;
                        if (DensityCache[1] > IsoValue) cubeindex |= 2;
                        if (DensityCache[2] > IsoValue) cubeindex |= 4;
                        if (DensityCache[3] > IsoValue) cubeindex |= 8;
                        if (DensityCache[4] > IsoValue) cubeindex |= 16;
                        if (DensityCache[5] > IsoValue) cubeindex |= 32;
                        if (DensityCache[6] > IsoValue) cubeindex |= 64;
                        if (DensityCache[7] > IsoValue) cubeindex |= 128;

                        /* Cube is entirely in/out of the surface */
                        if (cubeindex != 0 && cubeindex != 255)
                        {
                            /*0-r
                            1-r+x
                            2-r+x+y
                            3-r+y
                            4-r+z
                            5-r+x+z
                            6-r+x+y+z
                            7-r+y+z
                            */
                            //Now lets generate some normal vectors!
                            Vector3[] NormalCache = new Vector3[8];
                            NormalCache[0] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x - 1, y, z);
                            NormalCache[1] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x, y, z);
                            NormalCache[2] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x, y + 1, z);
                            NormalCache[3] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x - 1, y + 1, z);
                            NormalCache[4] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x - 1, y, z + 1);
                            NormalCache[5] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x, y, z + 1);
                            NormalCache[6] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x, y + 1, z + 1);
                            NormalCache[7] = ComputeNormal(ref DensityField, DensityFieldWidth, DensityFieldHeight, DensityFieldDepth, x - 1, y + 1, z + 1);
                            for (int i = 0; VoxelHelper.NewTriangleTable2[cubeindex, i] != -1; i += 3)
                            {
                                int[] edgeIndices = new int[3];
                                for (int j = 0; j < 3; j++)
                                {
                                    int idx = GetEdgeId(DensityFieldWidth, DensityFieldHeight, x, y, z, VoxelHelper.NewTriangleTable2[cubeindex, i + j]);
                                    edgeIndices[j] = idx;
                                    if(!collisionGraph.ContainsKey(idx))
                                        collisionGraph.Add(idx, new List<TriangleGraph>());
                                    if (!_edgeToIndices.ContainsKey(idx))
                                    {
                                        _edgeToIndices.Add(idx, (ushort)_vertices.Count);
                                        
                                        _vertices.Add(GenerateVertex(VoxelHelper.NewTriangleTable2[cubeindex, i + j], VectorCache, NormalCache, DensityCache, IsoValue));
                                    }
                                    _indices.Add(_edgeToIndices[idx]);
                                }
                                ushort id0 = _indices[_indices.Count - 3];
                                ushort id1 = _indices[_indices.Count - 2];
                                ushort id2 = _indices[_indices.Count - 1];
                                Vector3 v0 = Vector3.Transform(_vertices[id0].Position, transform);
                                Vector3 v1 = Vector3.Transform(_vertices[id1].Position, transform);
                                Vector3 v2 = Vector3.Transform(_vertices[id2].Position, transform);
                                TriangleGraph graph = new TriangleGraph(id0, id1, id2, v0, v1, v2);
                                for (int j = 0; j < edgeIndices.Length; j++)
                                    collisionGraph[edgeIndices[j]].Add(graph);

                                PrimitiveCount++;
                            }
                        }
                        //Swap our caches
                        VectorCache[0] = VectorCache[1];
                        VectorCache[3] = VectorCache[2];
                        VectorCache[4] = VectorCache[5];
                        VectorCache[7] = VectorCache[6];
                        DensityCache[0] = DensityCache[1];
                        DensityCache[3] = DensityCache[2];
                        DensityCache[4] = DensityCache[5];
                        DensityCache[7] = DensityCache[6];
                    }
                }
            }

            verts = _vertices.ToArray();
            ib = _indices.ToArray();
            if (verts.Length > 0)
            {
                renderElement.VertexCount = verts.Length;
                renderElement.VertexBuffer = new VertexBuffer(GFX.Device, verts.Length * VertexPN.SizeInBytes, BufferUsage.WriteOnly);
                renderElement.VertexBuffer.SetData<VertexPN>(verts);
            }
            if (ib.Length > 0)
            {
                renderElement.IndexBuffer = new IndexBuffer(GFX.Device, ib.Length * sizeof(ushort), BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                renderElement.IndexBuffer.SetData<ushort>(ib);
                renderElement.PrimitiveCount = PrimitiveCount;
            }

            collisionTree = new KDTree<TriangleGraph>(TriangleCompareFunction);
            for (int i = 0; i < collisionGraph.Values.Count; i++)
            {
                List<TriangleGraph> entries = collisionGraph.Values[i];
                collisionTree.AddElementRange(entries.ToArray(), false);
            }
            collisionTree.BuildTree();

            CanRender = (PrimitiveCount > 0);
        }

        int GetEdgeId(int DensityFieldWidth, int DensityFieldHeight, int x, int y, int z, int edgeName)
        {
            switch (edgeName)
            {
                case 0:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z);
                case 1:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x + 1, y, z) + 1;
                case 2:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y + 1, z);
                case 3:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z) + 1;
                case 4:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z + 1);
                case 5:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x + 1, y, z + 1) + 1;
                case 6:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y + 1, z + 1);
                case 7:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z + 1) + 1;
                case 8:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z) + 2;
                case 9:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x + 1, y, z) + 2;
                case 10:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x + 1, y + 1, z) + 2;
                case 11:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y + 1, z) + 2;
                default:
                    return GetVertexId(DensityFieldWidth, DensityFieldHeight, x, y, z) + 3;
            }
        }

        int GetVertexId(int DensityFieldWidth, int DensityFieldHeight, int x, int y, int z)
        {
            return 4 * (DensityFieldWidth * (z * DensityFieldHeight + y) + x); //Vertex centroids
        }

        VertexPN VertexInterp(byte IsoValue, Vector3 p1, Vector3 p2, Vector3 n1, Vector3 n2, byte valp1, byte valp2)
        {
            float eps = 0.001f;
            float b = valp2 - valp1;
            if (Math.Abs(IsoValue - valp1) < eps || Math.Abs(b) < eps)
                return new VertexPN(p1, n1);//N1, T1);
            if (Math.Abs(IsoValue - valp2) < eps)
                return new VertexPN(p2, n2);//N2, T2);

            float mu = (float)(IsoValue - valp1) / b;

            return new VertexPN(p1 + mu * (p2 - p1), n1 + mu * (n2 - n1));
        }

        VertexPN GenerateVertex(int edge, Vector3[] vecs, Vector3[] normals, byte[] densities, byte isoValue)
        {
            switch (edge)
            {
                case 0:
                    return VertexInterp(isoValue, vecs[0], vecs[1], normals[0], normals[1], densities[0], densities[1]);
                case 1:
                    return VertexInterp(isoValue, vecs[1], vecs[2], normals[1], normals[2], densities[1], densities[2]);
                case 2:
                    return VertexInterp(isoValue, vecs[2], vecs[3], normals[2], normals[3], densities[2], densities[3]);
                case 3:
                    return VertexInterp(isoValue, vecs[3], vecs[0], normals[3], normals[0], densities[3], densities[0]);
                case 4:
                    return VertexInterp(isoValue, vecs[4], vecs[5], normals[4], normals[5], densities[4], densities[5]);
                case 5:
                    return VertexInterp(isoValue, vecs[5], vecs[6], normals[5], normals[6], densities[5], densities[6]);
                case 6:
                    return VertexInterp(isoValue, vecs[6], vecs[7], normals[6], normals[7], densities[6], densities[7]);
                case 7:
                    return VertexInterp(isoValue, vecs[7], vecs[4], normals[7], normals[4], densities[7], densities[4]);
                case 8:
                    return VertexInterp(isoValue, vecs[0], vecs[4], normals[0], normals[4], densities[0], densities[4]);
                case 9:
                    return VertexInterp(isoValue, vecs[1], vecs[5], normals[1], normals[5], densities[1], densities[5]);
                case 10:
                    return VertexInterp(isoValue, vecs[2], vecs[6], normals[2], normals[6], densities[2], densities[6]);
                case 11:
                    return VertexInterp(isoValue, vecs[3], vecs[7], normals[3], normals[7], densities[3], densities[7]);
                default:
                    return new VertexPN((vecs[0] + vecs[6]) * 0.5f, Vector3.Lerp(normals[0], normals[6], 0.5f));//Centroid
            }
        }

        Vector3 ComputeNormal(ref byte[] DensityField, int DensityFieldWidth, int DensityFieldHeight, int DensityFieldDepth, int x, int y, int z)
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
    }
}
