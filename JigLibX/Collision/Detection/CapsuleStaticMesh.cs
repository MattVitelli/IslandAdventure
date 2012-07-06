#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Geometry;
using JigLibX.Math;
#endregion

namespace JigLibX.Collision
{

    /// <summary>
    /// DetectFunctor for CapsuleStaticMesh collison detection.
    /// </summary>
    public class CollDetectCapsuleStaticMesh : DetectFunctor
    {

        /// <summary>
        /// 
        /// </summary>
        public CollDetectCapsuleStaticMesh()
            : base("CapsuleStaticMesh", (int)PrimitiveType.Capsule, (int)PrimitiveType.TriangleMesh)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="collTolerance"></param>
        /// <param name="collisionFunctor"></param>
        public override void CollDetect(CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            // get the skins in the order that we're expecting
            if (info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0).Type == this.Type1)
            {
                CollisionSkin skinSwap = info.Skin0;
                info.Skin0 = info.Skin1;
                info.Skin1 = skinSwap;
                int primSwap = info.IndexPrim0;
                info.IndexPrim0 = info.IndexPrim1;
                info.IndexPrim1 = primSwap;
            }

            if((info.Skin0.CollisionSystem != null) && info.Skin0.CollisionSystem.UseSweepTests)
                CollDetectSweep(info, collTolerance, collisionFunctor);
            else
                CollDetectOverlap(info, collTolerance, collisionFunctor);
        }

        private void CollDetectCapsuleStaticMeshOverlap(Capsule oldCapsule, Capsule newCapsule,
            TriangleMesh mesh, CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {

            Vector3 body0Pos = (info.Skin0.Owner != null) ? info.Skin0.Owner.OldPosition : Vector3.Zero;
            Vector3 body1Pos = (info.Skin1.Owner != null) ? info.Skin1.Owner.OldPosition : Vector3.Zero;

            float capsuleTolR = collTolerance + newCapsule.Radius;
            float capsuleTolR2 = capsuleTolR * capsuleTolR;

            Vector3 collNormal = Vector3.Zero;

            BoundingBox bb = BoundingBoxHelper.InitialBox;
            BoundingBoxHelper.AddCapsule(newCapsule, ref bb);

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
                    fixed (int* potentialTriangles = potTriArray)
                    {
#endif
                        int numCollPts = 0;

                        int numTriangles = mesh.GetTrianglesIntersectingtAABox(potentialTriangles, MaxLocalStackTris, ref bb);

                        Vector3 capsuleStart = newCapsule.Position;
                        Vector3 capsuleEnd = newCapsule.GetEnd();
                        Matrix meshInvTransform = mesh.InverseTransformMatrix;

                        Vector3 meshSpaceCapsuleStart = Vector3.Transform(capsuleStart, meshInvTransform);
                        Vector3 meshSpaceCapsuleEnd = Vector3.Transform(capsuleEnd, meshInvTransform);

                        for (int iTriangle = 0; iTriangle < numTriangles; ++iTriangle)
                        {
                            IndexedTriangle meshTriangle = mesh.GetTriangle(potentialTriangles[iTriangle]);

                            // we do the plane test using the capsule in mesh space
                            float distToStart = meshTriangle.Plane.DotCoordinate(meshSpaceCapsuleStart);
                            float distToEnd = meshTriangle.Plane.DotCoordinate(meshSpaceCapsuleEnd);

                            if (distToStart > capsuleTolR && distToEnd > capsuleTolR)
                                continue;
                            if (distToStart < 0.0f && distToEnd < 0.0f)
                                continue;

                            // we now transform the triangle into world space (we could keep leave the mesh alone
                            // but at this point 3 vector transforms is probably not a major slow down)
                            int i0, i1, i2;
                            meshTriangle.GetVertexIndices(out i0, out i1, out i2);

                            Vector3 triVec0;
                            Vector3 triVec1;
                            Vector3 triVec2;
                            mesh.GetVertex(i0, out triVec0);
                            mesh.GetVertex(i1, out triVec1);
                            mesh.GetVertex(i2, out triVec2);

                            // Deano move tri into world space
                            Matrix transformMatrix = mesh.TransformMatrix;
                            Vector3.Transform(ref triVec0, ref transformMatrix, out triVec0);
                            Vector3.Transform(ref triVec1, ref transformMatrix, out triVec1);
                            Vector3.Transform(ref triVec2, ref transformMatrix, out triVec2);
                            Triangle triangle = new Triangle(ref triVec0, ref triVec1, ref triVec2);

                            Segment seg = new Segment(capsuleStart, capsuleEnd - capsuleStart);

                            float tS, tT0, tT1;
                            float d2 = Distance.SegmentTriangleDistanceSq(out tS, out tT0, out tT1, seg, triangle);

                            if (d2 < capsuleTolR2)
                            {
                                Vector3 oldCapsuleStart = oldCapsule.Position;
                                Vector3 oldCapsuleEnd = oldCapsule.GetEnd();
                                Segment oldSeg = new Segment(oldCapsuleStart, oldCapsuleEnd - oldCapsuleStart);
                                d2 = Distance.SegmentTriangleDistanceSq(out tS, out tT0, out tT1, oldSeg, triangle);
                                // report result from old position
                                float dist = (float)System.Math.Sqrt(d2);
                                float depth = oldCapsule.Radius - dist;
                                Vector3 pt = triangle.GetPoint(tT0, tT1);
                                Vector3 collisionN = (d2 > JiggleMath.Epsilon) ? JiggleMath.NormalizeSafe(oldSeg.GetPoint(tS) - pt) :
                                    meshTriangle.Plane.Normal;
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
                    }
                    FreeStackAlloc(potTriArray);
                }
                FreeStackAlloc(collPtArray);
#endif
            }
        }

        private void CollDetectOverlap(CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            Capsule oldCapsule = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Capsule;
            Capsule newCapsule = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Capsule;

            // todo - proper swept test
            // note - mesh is static and its triangles are in world space
            TriangleMesh mesh = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as TriangleMesh;

            CollDetectCapsuleStaticMeshOverlap(oldCapsule, newCapsule, mesh, info, collTolerance, collisionFunctor);
        }

        private void CollDetectCapsulseStaticMeshSweep(Capsule oldCapsule, Capsule newCapsule,
            TriangleMesh mesh, CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            // really use a swept test - or overlap?
            Vector3 delta = newCapsule.Position - oldCapsule.Position;
            if (delta.LengthSquared() < (0.25f * newCapsule.Radius * newCapsule.Radius))
            {
                CollDetectCapsuleStaticMeshOverlap(oldCapsule, newCapsule, mesh, info, collTolerance, collisionFunctor);
            }
            else
            {
                float capsuleLen = oldCapsule.Length;
                float capsuleRadius = oldCapsule.Radius;

                int nSpheres = 2 + (int)(capsuleLen / (2.0f * oldCapsule.Radius));
                for (int iSphere = 0; iSphere < nSpheres; ++iSphere)
                {
                    float offset = ((float)iSphere) * capsuleLen / ((float)nSpheres - 1.0f);
                    BoundingSphere oldSphere = new BoundingSphere(oldCapsule.Position + oldCapsule.Orientation.Backward * offset, capsuleRadius);
                    BoundingSphere newSphere = new BoundingSphere(newCapsule.Position + newCapsule.Orientation.Backward * offset, capsuleRadius);
                    CollDetectSphereStaticMesh.CollDetectSphereStaticMeshSweep(oldSphere, newSphere, mesh, info, collTolerance, collisionFunctor);
                }
            }
        }

        private void CollDetectSweep(CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            Capsule oldCapsule = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Capsule;
            Capsule newCapsule = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Capsule;

            // todo - proper swept test
            // note - mesh is static and its triangles are in world space
            TriangleMesh mesh = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as TriangleMesh;

            CollDetectCapsulseStaticMeshSweep(oldCapsule, newCapsule, mesh, info, collTolerance, collisionFunctor);
        }

    }
}
