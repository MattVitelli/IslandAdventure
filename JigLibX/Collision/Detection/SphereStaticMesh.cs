#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Geometry;
using JigLibX.Math;
using System.Runtime.InteropServices;
#endregion

namespace JigLibX.Collision
{

    /// <summary>
    /// DetectFunctor for SphereStaticMesh collison detection.
    /// </summary>
    public class CollDetectSphereStaticMesh : DetectFunctor
    {

        /// <summary>
        /// 
        /// </summary>
        public CollDetectSphereStaticMesh()
            : base("SphereStaticMesh", (int)PrimitiveType.Sphere, (int)PrimitiveType.TriangleMesh)
        {
        }

        public static void CollDetectSphereStaticMeshOverlap(BoundingSphere oldSphere, BoundingSphere newSphere,
            TriangleMesh mesh, CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            Vector3 body0Pos = (info.Skin0.Owner != null) ? info.Skin0.Owner.OldPosition : Vector3.Zero;
            Vector3 body1Pos = (info.Skin1.Owner != null) ? info.Skin1.Owner.OldPosition : Vector3.Zero;

            float sphereTolR = collTolerance + newSphere.Radius;
            float sphereTolR2 = sphereTolR * sphereTolR;

            unsafe
            {
#if USE_STACKALLOC
                SmallCollPointInfo* collPts = stackalloc SmallCollPointInfo[MaxLocalStackSCPI];
                int* potentialTriangles = stackalloc int[MaxLocalStackTris];
                {
                    {
#else
                SmallCollPointInfo[] collPtArray = SCPIStackAlloc();
                fixed (SmallCollPointInfo* collPts = collPtArray)
                {
                    int[] potTriArray = IntStackAlloc();
                    fixed( int* potentialTriangles = potTriArray)
                    {
#endif     
                        int numCollPts = 0;

                        Vector3 collNormal = Vector3.Zero;

                        BoundingBox bb = BoundingBoxHelper.InitialBox;
                        BoundingBoxHelper.AddSphere(newSphere, ref bb);
                        int numTriangles = mesh.GetTrianglesIntersectingtAABox(potentialTriangles, MaxLocalStackTris, ref bb);

                        // Deano : get the spheres centers in triangle mesh space
                        Vector3 newSphereCen = Vector3.Transform(newSphere.Center, mesh.InverseTransformMatrix);
                        Vector3 oldSphereCen = Vector3.Transform(oldSphere.Center, mesh.InverseTransformMatrix);

                        for (int iTriangle = 0; iTriangle < numTriangles; ++iTriangle)
                        {
                            IndexedTriangle meshTriangle = mesh.GetTriangle(potentialTriangles[iTriangle]);
                            float distToCentre = meshTriangle.Plane.DotCoordinate(newSphereCen);

                            if (distToCentre <= 0.0f)
                                continue;
                            if (distToCentre >= sphereTolR)
                                continue;
                            int i0, i1, i2;
                            meshTriangle.GetVertexIndices(out i0, out i1, out i2);

                            Triangle triangle = new Triangle(mesh.GetVertex(i0), mesh.GetVertex(i1), mesh.GetVertex(i2));

                            float s, t;
                            float newD2 = Distance.PointTriangleDistanceSq(out s, out t, newSphereCen, triangle);

                            if (newD2 < sphereTolR2)
                            {
                                // have overlap - but actually report the old intersection
                                float oldD2 = Distance.PointTriangleDistanceSq(out s, out t, oldSphereCen, triangle);
                                float dist = (float)System.Math.Sqrt((float)oldD2);
                                float depth = oldSphere.Radius - dist;

                                Vector3 triPointSTNorm = oldSphereCen - triangle.GetPoint(s, t);
                                JiggleMath.NormalizeSafe(ref triPointSTNorm);

                                Vector3 collisionN = (dist > float.Epsilon) ? triPointSTNorm : triangle.Normal;

                                // since impulse get applied at the old position
                                Vector3 pt = oldSphere.Center - oldSphere.Radius * collisionN;

                                if (numCollPts < MaxLocalStackSCPI)
                                {
                                    collPts[numCollPts++] = new SmallCollPointInfo(pt - body0Pos, pt - body1Pos, depth);
                                }
                                collNormal += collisionN;
                            }
                        }

                        if (numCollPts > 0)
                        {
                            JiggleMath.NormalizeSafe(ref collNormal);
                            collisionFunctor.CollisionNotify(ref info, ref collNormal, collPts, numCollPts);
                        }
#if USE_STACKALLOC
                    }
               }
#else

                        FreeStackAlloc(potTriArray);
                    }
                    FreeStackAlloc(collPtArray);
                }
#endif
            }
        }

        private void CollDetectOverlap(CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            // todo - proper swept test
            Sphere oldSphere = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Sphere;
            Sphere newSphere = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Sphere;
            BoundingSphere oldBSphere = new BoundingSphere(oldSphere.Position, oldSphere.Radius);
            BoundingSphere newBSphere = new BoundingSphere(newSphere.Position, newSphere.Radius);

            // todo - proper swept test
            // note - mesh is static and its triangles are in world space
            TriangleMesh mesh = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as TriangleMesh;

            CollDetectSphereStaticMeshOverlap(oldBSphere, newBSphere, mesh, info, collTolerance, collisionFunctor);
        }

        internal static void CollDetectSphereStaticMeshSweep(BoundingSphere oldSphere, BoundingSphere newSphere, TriangleMesh mesh,
            CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {

            // really use a swept test - or overlap?
            Vector3 delta = newSphere.Center - oldSphere.Center;
            if (delta.LengthSquared() < (0.25f * newSphere.Radius * newSphere.Radius))
            {
                CollDetectSphereStaticMeshOverlap(oldSphere, newSphere, mesh, info, collTolerance, collisionFunctor);
            }
            else
            {
                Vector3 body0Pos = (info.Skin0.Owner != null) ? info.Skin0.Owner.OldPosition : Vector3.Zero;
                Vector3 body1Pos = (info.Skin1.Owner != null) ? info.Skin1.Owner.OldPosition : Vector3.Zero;

                float sphereTolR = collTolerance + oldSphere.Radius;
                float sphereToR2 = sphereTolR * sphereTolR;

                Vector3 collNormal = Vector3.Zero;

                BoundingBox bb = BoundingBoxHelper.InitialBox;
                BoundingBoxHelper.AddSphere(oldSphere, ref bb);
                BoundingBoxHelper.AddSphere(newSphere, ref bb);

                // get the spheres centers in triangle mesh space
                Vector3 newSphereCen = Vector3.Transform(newSphere.Center, mesh.InverseTransformMatrix);
                Vector3 oldSphereCen = Vector3.Transform(oldSphere.Center, mesh.InverseTransformMatrix);

                unsafe
                {
#if USE_STACKALLOC
                    SmallCollPointInfo* collPts = stackalloc SmallCollPointInfo[MaxLocalStackSCPI];
                    int* potentialTriangles = stackalloc int[MaxLocalStackTris];
                    {
                        {
#else
                    SmallCollPointInfo[] collPtArray = SCPIStackAlloc();
                    fixed (SmallCollPointInfo* collPts = collPtArray)
                    {
                        int[] potTriArray = IntStackAlloc();
                        fixed( int* potentialTriangles = potTriArray)
                        {
#endif
                            int numCollPts = 0;

                            int numTriangles = mesh.GetTrianglesIntersectingtAABox(potentialTriangles, MaxLocalStackTris, ref bb);

                            for (int iTriangle = 0; iTriangle < numTriangles; ++iTriangle)
                            {

                                // first test the old sphere for being on the wrong side
                                IndexedTriangle meshTriangle = mesh.GetTriangle(potentialTriangles[iTriangle]);
                                float distToCentreOld = meshTriangle.Plane.DotCoordinate(oldSphereCen);
                                if (distToCentreOld <= 0.0f)
                                    continue;
                                // now test the new sphere for being clear

                                float distToCentreNew = meshTriangle.Plane.DotCoordinate(newSphereCen);
                                if (distToCentreNew > sphereTolR)
                                    continue;

                                int i0, i1, i2;
                                meshTriangle.GetVertexIndices(out i0, out i1, out i2);

                                Triangle triangle = new Triangle(mesh.GetVertex(i0), mesh.GetVertex(i1), mesh.GetVertex(i2));

                                // If the old sphere is intersecting, just use that result
                                float s, t;
                                float d2 = Distance.PointTriangleDistanceSq(out s, out t, oldSphereCen, triangle);

                                if (d2 < sphereToR2)
                                {
                                    float dist = (float)System.Math.Sqrt(d2);
                                    float depth = oldSphere.Radius - dist;
                                    Vector3 triangleN = triangle.Normal;
                                    Vector3 normSafe = oldSphereCen - triangle.GetPoint(s, t);

                                    JiggleMath.NormalizeSafe(ref normSafe);

                                    Vector3 collisionN = (dist > float.Epsilon) ? normSafe : triangleN;
                                    // since impulse gets applied at the old position
                                    Vector3 pt = oldSphere.Center - oldSphere.Radius * collisionN;
                                    if (numCollPts < MaxLocalStackSCPI)
                                    {
                                        collPts[numCollPts++] = new SmallCollPointInfo(pt - body0Pos, pt - body1Pos, depth);
                                    }
                                    collNormal += collisionN;
                                }
                                else if (distToCentreNew < distToCentreOld)
                                {
                                    // old sphere is not intersecting - do a sweep, but only if the sphere is moving into the
                                    // triangle
                                    Vector3 pt, N; // CHECK THIS
                                    float depth;
                                    if (Intersection.SweptSphereTriangleIntersection(out pt, out N, out depth, oldSphere, newSphere, triangle,
                                        distToCentreOld, distToCentreNew, Intersection.EdgesToTest.EdgeAll, Intersection.CornersToTest.CornerAll))
                                    {
                                        // collision point etc must be relative to the old position because that's
                                        //where the impulses are applied
                                        float dist = (float)System.Math.Sqrt(d2);
                                        float depth2 = oldSphere.Radius - dist;
                                        Vector3 triangleN = triangle.Normal;
                                        Vector3 normSafe = oldSphereCen - triangle.GetPoint(s, t);
                                        JiggleMath.NormalizeSafe(ref normSafe);
                                        Vector3 collisionN = (dist > JiggleMath.Epsilon) ? normSafe : triangleN;
                                        // since impulse gets applied at the old position
                                        Vector3 pt2 = oldSphere.Center - oldSphere.Radius * collisionN;
                                        if (numCollPts < MaxLocalStackSCPI)
                                        {
                                            collPts[numCollPts++] = new SmallCollPointInfo(pt2 - body0Pos, pt2 - body1Pos, depth);
                                        }
                                        collNormal += collisionN;
                                    }
                                }
                            }
                            if (numCollPts > 0)
                            {
                                JiggleMath.NormalizeSafe(ref collNormal);
                                collisionFunctor.CollisionNotify(ref info, ref collNormal, collPts, numCollPts);
                            }
                        }
#if USE_STACKALLOC
                    }
               }
#else

                        FreeStackAlloc(potTriArray);
                    }
                    FreeStackAlloc(collPtArray);
                }
#endif
            }
        }

        private void CollDetectSweep(CollDetectInfo info, float collTolerance,
                                                  CollisionFunctor collisionFunctor)
        {
            // todo - proper swept test
            Sphere oldSphere = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Sphere;
            Sphere newSphere = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Sphere;
            BoundingSphere oldBSphere = new BoundingSphere(oldSphere.Position, oldSphere.Radius);
            BoundingSphere newBSphere = new BoundingSphere(newSphere.Position, newSphere.Radius);

            // todo - proper swept test
            // note - mesh is static and its triangles are in world space
            TriangleMesh mesh = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as TriangleMesh;

            CollDetectSphereStaticMeshSweep(oldBSphere, newBSphere, mesh, info, collTolerance, collisionFunctor);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="collTolerance"></param>
        /// <param name="collisionFunctor"></param>
        public override void CollDetect(CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            if (info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0).Type == this.Type1)
            {
                CollisionSkin skinSwap = info.Skin0;
                info.Skin0 = info.Skin1;
                info.Skin1 = skinSwap;
                int primSwap = info.IndexPrim0;
                info.IndexPrim0 = info.IndexPrim1;
                info.IndexPrim1 = primSwap;
            }

            if(info.Skin0.CollisionSystem != null && info.Skin0.CollisionSystem.UseSweepTests)
                CollDetectSweep(info, collTolerance, collisionFunctor);
            else
                CollDetectOverlap(info, collTolerance, collisionFunctor);
        }
    }
}
