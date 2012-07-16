#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
using JigLibX.Collision;
#endregion

namespace JigLibX.Geometry
{
    public class TriangleMesh : Primitive
    {
        private KDTreeTriangles kdTree;
        private Octree octree;
        private int maxTrianglesPerCell;
        private float minCellSize;

        public TriangleMesh(List<Vector3> vertices, List<TriangleVertexIndices> indices)
            : base((int)PrimitiveType.TriangleMesh)
        {
            kdTree = new KDTreeTriangles(indices, vertices);
        }

        public TriangleMesh()
            : base((int)PrimitiveType.TriangleMesh)
        {
        }

        public void CreateMesh(List<Vector3> vertices,
            List<TriangleVertexIndices> triangleVertexIndices,
            int maxTrianglesPerCell, float minCellSize)
        {
            this.octree = new Octree(vertices, triangleVertexIndices);
            this.maxTrianglesPerCell = maxTrianglesPerCell;
            this.minCellSize = minCellSize;
        }

        public override void GetBoundingBox(out AABox box)
        {
            if (octree == null)
            {
                BoundingBox bounds = kdTree.GetRoot().boundingBox;
                box = new AABox(bounds.Min, bounds.Max);
                box.Transform = Transform;
            }
            else
            {
                box = octree.BoundingBox;
            }
        }

        private Matrix transformMatrix;
        private Matrix invTransform;
        public override Transform Transform
        {
            get
            {
                return base.Transform;
            }
            set
            {
                base.Transform = value;
                transformMatrix = transform.Orientation;
                transformMatrix.Translation = transform.Position;
                invTransform = Matrix.Invert(transformMatrix);
            }
        }

        // use a cached version as this occurs ALOT during triangle mesh traversal
        public override Matrix TransformMatrix
        {
            get
            {
                return transformMatrix;
            }
        }
        // use a cached version as this occurs ALOT during triangle mesh traversal
        public override Matrix InverseTransformMatrix
        {
            get
            {
                return invTransform;
            }
        }
        /*
        public KDTreeTriangles KDTree
        {
            get { return this.kdTree; }
        }
        */
        public int GetNumTriangles()
        {
            return (octree == null)?kdTree.NumTriangles:octree.NumTriangles;
        }

        public IndexedTriangle GetTriangle(int iTriangle)
        {
            return (octree == null)?kdTree.GetIndexedTriangle(iTriangle):octree.GetTriangle(iTriangle);
        }

        public Vector3 GetVertex(int iVertex)
        {
            return (octree == null)?kdTree.GetVertex(iVertex):octree.GetVertex(iVertex);
        }

        public void GetVertex(int iVertex, out Vector3 result)
        {
            result = (octree == null)?kdTree.GetVertex(iVertex):octree.GetVertex(iVertex);
        }

        public unsafe int GetTrianglesIntersectingtAABox(int* triangles, int maxTriangles, ref BoundingBox bb)
        {
            // move segment into octree space           
            Vector3 aabbMin = Vector3.Transform(bb.Min, invTransform);
            Vector3 aabbMax = Vector3.Transform(bb.Max, invTransform);

            // rotated aabb
            BoundingBox rotBB = bb;
            BoundingBoxHelper.AddPoint(ref aabbMin, ref rotBB);
            BoundingBoxHelper.AddPoint(ref aabbMax, ref rotBB);
            return (octree == null)?kdTree.GetTrianglesIntersectingtAABox(triangles, maxTriangles, ref rotBB):octree.GetTrianglesIntersectingtAABox(triangles, maxTriangles, ref rotBB);
        }

        public override Primitive Clone()
        {
            TriangleMesh triangleMesh = new TriangleMesh();
            //            triangleMesh.CreateMesh(vertices, triangleVertexIndices, maxTrianglesPerCell, minCellSize);
            // its okay to share the octree
            triangleMesh.kdTree = this.kdTree;
            triangleMesh.octree = this.octree;
            triangleMesh.Transform = Transform;
            return triangleMesh;
        }

        public override bool SegmentIntersect(out float frac, out Vector3 pos, out Vector3 normal, Segment seg)
        {
            // move segment into octree space
            seg.Origin = Vector3.Transform(seg.Origin, invTransform);
            seg.Delta = Vector3.TransformNormal(seg.Delta, invTransform);


            BoundingBox segBox = BoundingBoxHelper.InitialBox;
            BoundingBoxHelper.AddSegment(seg, ref segBox);

            unsafe
            {
#if USE_STACKALLOC
                int* potentialTriangles = stackalloc int[MaxLocalStackTris];
                {
#else
                int[] potTriArray = DetectFunctor.IntStackAlloc();
                fixed (int* potentialTriangles = potTriArray)
                {
#endif
                    int numTriangles = GetTrianglesIntersectingtAABox(potentialTriangles, DetectFunctor.MaxLocalStackTris, ref segBox);

                    float tv1, tv2;

                    pos = Vector3.Zero;
                    normal = Vector3.Zero;

                    float bestFrac = float.MaxValue;
                    for (int iTriangle = 0; iTriangle < numTriangles; ++iTriangle)
                    {
                        IndexedTriangle meshTriangle = GetTriangle(potentialTriangles[iTriangle]);
                        float thisFrac;
                        Triangle tri = new Triangle(GetVertex(meshTriangle.GetVertexIndex(0)),
                          GetVertex(meshTriangle.GetVertexIndex(1)),
                          GetVertex(meshTriangle.GetVertexIndex(2)));

                        if (Intersection.SegmentTriangleIntersection(out thisFrac, out tv1, out tv2, seg, tri))
                        {
                            if (thisFrac < bestFrac)
                            {
                                bestFrac = thisFrac;
                                // re-project
                                pos = Vector3.Transform(seg.GetPoint(thisFrac), transformMatrix);
                                normal = Vector3.TransformNormal(meshTriangle.Plane.Normal, transformMatrix);
                            }
                        }
                    }

                    frac = bestFrac;
                    if (bestFrac < float.MaxValue)
                    {
                        DetectFunctor.FreeStackAlloc(potTriArray);
                        return true;
                    }
                    else
                    {
                        DetectFunctor.FreeStackAlloc(potTriArray);
                        return false;
                    }
#if USE_STACKALLOC
                }
#else
                }
#endif
            }
        }

        public override float GetVolume()
        {
            return 0.0f;
        }

        public override float GetSurfaceArea()
        {
            return 0.0f;
        }

        public override void GetMassProperties(PrimitiveProperties primitiveProperties, out float mass, out Vector3 centerOfMass, out Matrix inertiaTensor)
        {
            mass = 0.0f;
            centerOfMass = Vector3.Zero;
            inertiaTensor = Matrix.Identity;
        }
    }
}
