#region Using statements
using System;
using System.Collections.Generic;
using System.Text;
using JigLibX.Geometry;
using Microsoft.Xna.Framework;
using JigLibX.Math;
#endregion

namespace JigLibX.Collision
{

    /// <summary>
    /// DetectFunctor for BoxStaticMesh collison detection.
    /// </summary>
    public class CollDetectBoxStaticMesh : DetectFunctor
    {

        /// <summary>
        /// Constructor of BoxStaticMesh Collision DetectFunctor.
        /// <seealso cref="DetectFunctor"/>
        /// </summary>
        public CollDetectBoxStaticMesh()
            : base("BoxStaticMesh", (int)PrimitiveType.Box, (int)PrimitiveType.TriangleMesh)
        {

        }

        private void CollDetectOverlap(ref CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            // note - mesh is static and its triangles are in world space
            TriangleMesh mesh = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as TriangleMesh;

            Box oldBox = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Box;
            Box newBox = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Box;

            CollDetectBoxStaticMeshOverlap(oldBox, newBox, mesh, ref info, collTolerance, collisionFunctor);
        }

        private void CollDetectSweep(ref CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            // todo - proper swept test
            // note - mesh is static and its triangles are in world space
            TriangleMesh mesh = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as TriangleMesh;

            Box oldBox = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Box;
            Box newBox = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Box;

            Vector3 oldCentre;
            oldBox.GetCentre(out oldCentre);
            Vector3 newCentre;
            newBox.GetCentre(out newCentre);

            Vector3 delta;
            Vector3.Subtract(ref newCentre, ref oldCentre, out delta);

            float boxMinLen = 0.5f * System.Math.Min(newBox.SideLengths.X, System.Math.Min(newBox.SideLengths.Y, newBox.SideLengths.Z));
            int nPositions = 1 + (int)(delta.Length() / boxMinLen);
            // limit the max positions...
            if (nPositions > 50)
            {
                System.Diagnostics.Debug.WriteLine("Warning - clamping max positions in swept box test");
                nPositions = 50;
            }
            if (nPositions == 1)
            {
                CollDetectBoxStaticMeshOverlap(oldBox, newBox, mesh,ref info, collTolerance, collisionFunctor);
            }
            else
            {
                BoundingBox bb = BoundingBoxHelper.InitialBox;
                BoundingBoxHelper.AddBox(oldBox, ref bb);
                BoundingBoxHelper.AddBox(newBox, ref bb);
                unsafe
                {
#if USE_STACKALLOC
                    int* potentialTriangles = stackalloc int[MaxLocalStackTris];
                    {
#else
                    int[] potTriArray = IntStackAlloc();
                    fixed( int* potentialTriangles = potTriArray)
                    {
#endif
                        int numTriangles = mesh.GetTrianglesIntersectingtAABox(potentialTriangles, MaxLocalStackTris, ref bb);
                        if (numTriangles > 0)
                        {
                            for (int i = 0; i <= nPositions; ++i)
                            {
                                float frac = ((float)i) / nPositions;
                                Vector3 centre;
                                Vector3.Multiply(ref delta, frac, out centre);
                                Vector3.Add(ref centre, ref oldCentre, out centre);

                                Matrix orient = Matrix.Add(Matrix.Multiply(oldBox.Orientation, 1.0f - frac), Matrix.Multiply(newBox.Orientation, frac));

                                Box box = new Box(centre - 0.5f * Vector3.Transform(newBox.SideLengths, orient), orient, newBox.SideLengths);
                                // ideally we'd break if we get one collision... but that stops us getting multiple collisions
                                // when we enter a corner (two walls meeting) - can let us pass through
                                CollDetectBoxStaticMeshOverlap(oldBox, box, mesh, ref info, collTolerance, collisionFunctor);
                            }
                        }
#if USE_STACKALLOC
                    }
#else
                    }
                    FreeStackAlloc(potTriArray);
#endif                
                }
            }
        }
        /// <summary>
        /// Detect BoxStaticMesh Collisions.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="collTolerance"></param>
        /// <param name="collisionFunctor"></param>
        public override void CollDetect(CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            if (info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0).Type == Type1)
            {
                CollisionSkin skinSwap = info.Skin0;
                info.Skin0 = info.Skin1;
                info.Skin1 = skinSwap;
                int primSwap = info.IndexPrim0;
                info.IndexPrim0 = info.IndexPrim1;
                info.IndexPrim1 = primSwap;
            }

            if (info.Skin0.CollisionSystem != null && info.Skin0.CollisionSystem.UseSweepTests)
                CollDetectSweep(ref info, collTolerance, collisionFunctor);
            else
                CollDetectOverlap(ref info, collTolerance, collisionFunctor);
        }

        /// <summary>
        /// AddPoint
        /// if pt is less than Sqrt(combinationDistanceSq) from one of the
        /// others the original is replaced with the mean of it
        /// and pt, and false is returned. true means that pt was
        /// added to pts
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="pt"></param>
        /// <param name="combinationDistanceSq"></param>
        /// <returns></returns>
        ///
        private static bool AddPoint(List<Vector3> pts, Vector3 pt, float combinationDistanceSq)
        {
            for (int i = pts.Count; i-- != 0; )
            {
                if (Distance.PointPointDistanceSq(pts[i], pt) < combinationDistanceSq)
                {
                    pts[i] = 0.5f * (pts[i] + pt);
                    return false;
                }
            }
            pts.Add(pt);
            return true;
        }

        /// <summary>
        /// GetBoxTriangleIntersectionPoints
        /// Pushes intersection points onto the back of pts. Returns the
        /// number of points found.
        /// Points that are close together (compared to 
        /// combinationDistance) get combined
        /// </summary>
        /// <param name="pts"></param>
        /// <param name="box"></param>
        /// <param name="triangle"></param>
        /// <param name="combinationDistance"></param>
        /// <returns></returns>
        private static int GetBoxTriangleIntersectionPoints(List<Vector3> pts, Box box, Triangle triangle, float combinationDistance)
        {
            // first intersect each edge of the box with the triangle
            Box.Edge[] edges;
            box.GetEdges(out edges);
            Vector3[] boxPts;
            box.GetCornerPoints(out boxPts);

            float tS;
            float tv1, tv2;

            int iEdge;
            for (iEdge = 0; iEdge < 12; ++iEdge)
            {
                Box.Edge edge = edges[iEdge];
                Segment seg = new Segment(boxPts[(int)edge.Ind0], boxPts[(int)edge.Ind1] - boxPts[(int)edge.Ind0]);
                if (Intersection.SegmentTriangleIntersection(out tS, out tv1, out tv2, seg, triangle))
                {
                     AddPoint(pts, seg.GetPoint(tS), combinationDistance * combinationDistance);
                }
            }

            Vector3 pos, n;
            // now each edge of the triangle with the box
            for (iEdge = 0; iEdge < 3; ++iEdge)
            {
                Vector3 pt0 = triangle.GetPoint(iEdge);
                Vector3 pt1 = triangle.GetPoint((iEdge + 1) % 3);
                Segment s1 = new Segment(pt0, pt1 - pt0);
                Segment s2 = new Segment(pt1, pt0 - pt1);
                if (box.SegmentIntersect(out tS, out pos, out n, s1))
                    AddPoint(pts, pos, combinationDistance * combinationDistance);
                if (box.SegmentIntersect(out tS, out pos, out n, s2))
                    AddPoint(pts, pos, combinationDistance * combinationDistance);
            }

            return pts.Count;
        }

        /// <summary>
        /// Disjoint Returns true if disjoint.  Returns false if intersecting,
        /// and sets the overlap depth, d scaled by the axis length
        /// </summary>
        /// <param name="d"></param>
        /// <param name="axis"></param>
        /// <param name="box"></param>
        /// <param name="triangle"></param>
        /// <param name="collTolerance"></param>
        /// <returns></returns>
        private static bool Disjoint(out float d, Vector3 axis, Box box, Triangle triangle, float collTolerance)
        {
            float min0, max0, min1, max1;

            box.GetSpan(out min0, out max0, axis);
            triangle.GetSpan(out min1, out max1, axis);

            if (min0 > (max1 + collTolerance + JiggleMath.Epsilon) ||
                min1 > (max0 + collTolerance + JiggleMath.Epsilon))
            {
                d = 0.0f;
                return true;
            }

            if ((max0 > max1) && (min1 > min0))
            {
                // triangle is inside - choose the min dist to move it out
                d = System.Math.Min(max0 - min1, max1 - min0);
            }
            else if ((max1 > max0) && (min0 > min1))
            {
                // box is inside - choose the min dist to move it out
                d = System.Math.Min(max1 - min0, max0 - min1);
            }
            else
            {
                // objects overlap
                d = (max0 < max1) ? max0 : max1;
                d -= (min0 > min1) ? min0 : min1;
            }

            return false;
        }
       
        private static bool DoOverlapBoxTriangleTest(Box oldBox, Box newBox,
            ref IndexedTriangle triangle, TriangleMesh mesh,
            ref CollDetectInfo info, float collTolerance,
            CollisionFunctor collisionFunctor)
        {

            Matrix dirs0 = newBox.Orientation;
            
            #region REFERENCE: Triangle tri = new Triangle(mesh.GetVertex(triangle.GetVertexIndex(0)),mesh.GetVertex(triangle.GetVertexIndex(1)),mesh.GetVertex(triangle.GetVertexIndex(2)));
            Vector3 triVec0;
            Vector3 triVec1;
            Vector3 triVec2;
            mesh.GetVertex(triangle.GetVertexIndex(0), out triVec0);
            mesh.GetVertex(triangle.GetVertexIndex(1), out triVec1);
            mesh.GetVertex(triangle.GetVertexIndex(2), out triVec2);

            // Deano move tri into world space
            Matrix transformMatrix = mesh.TransformMatrix;
            Vector3.Transform(ref triVec0, ref transformMatrix, out triVec0);
            Vector3.Transform(ref triVec1, ref transformMatrix, out triVec1);
            Vector3.Transform(ref triVec2, ref transformMatrix, out triVec2);

            Triangle tri = new Triangle(ref triVec0,ref triVec1,ref triVec2);
            #endregion


            #region REFERENCE Vector3 triEdge0 = (tri.GetPoint(1) - tri.GetPoint(0));
            Vector3 pt0;
            Vector3 pt1;
            tri.GetPoint(0, out pt0);
            tri.GetPoint(1, out pt1);

            Vector3 triEdge0;
            Vector3.Subtract(ref pt1, ref pt0, out triEdge0);
            #endregion

            #region REFERENCE Vector3 triEdge1 = (tri.GetPoint(2) - tri.GetPoint(1));
            Vector3 pt2;
            tri.GetPoint(2, out pt2);

            Vector3 triEdge1;
            Vector3.Subtract(ref pt2, ref pt1, out triEdge1);
            #endregion

            #region REFERENCE Vector3 triEdge2 = (tri.GetPoint(0) - tri.GetPoint(2));
            Vector3 triEdge2;
            Vector3.Subtract(ref pt0, ref pt2, out triEdge2);
            #endregion

            triEdge0.Normalize();
            triEdge1.Normalize();
            triEdge2.Normalize();

            Vector3 triNormal = triangle.Plane.Normal;

            // the 15 potential separating axes
            const int numAxes = 13;
            Vector3[] axes = new Vector3[numAxes];

            axes[0] = triNormal;
            axes[1] = dirs0.Right;
            axes[2] = dirs0.Up;
            axes[3] = dirs0.Backward;
            Vector3.Cross(ref axes[1], ref triEdge0, out axes[4]);
            Vector3.Cross(ref axes[1], ref triEdge1, out axes[5]);
            Vector3.Cross(ref axes[1], ref triEdge2, out axes[6]);
            Vector3.Cross(ref axes[2], ref triEdge0, out axes[7]);
            Vector3.Cross(ref axes[2], ref triEdge1, out axes[8]);
            Vector3.Cross(ref axes[2], ref triEdge2, out axes[9]);
            Vector3.Cross(ref axes[3], ref triEdge0, out axes[10]);
            Vector3.Cross(ref axes[3], ref triEdge1, out axes[11]);
            Vector3.Cross(ref axes[3], ref triEdge2, out axes[12]);

            // the overlap depths along each axis
            float[] overlapDepths = new float[numAxes];

            // see if the boxes are separate along any axis, and if not keep a 
            // record of the depths along each axis
            int i;
            for (i = 0; i < numAxes; ++i)
            {
                overlapDepths[i] = 1.0f;
                if (Disjoint(out overlapDepths[i], axes[i], newBox, tri, collTolerance))
                    return false;
            }

            // The box overlap, find the separation depth closest to 0.
            float minDepth = float.MaxValue;
            int minAxis = -1;

            for (i = 0; i < numAxes; ++i)
            {
                // If we can't normalise the axis, skip it
                float l2 = axes[i].LengthSquared();
                if (l2 < JiggleMath.Epsilon)
                    continue;

                // Normalise the separation axis and the depth
                float invl = 1.0f / (float)System.Math.Sqrt(l2);
                axes[i] *= invl;
                overlapDepths[i] *= invl;

                // If this axis is the minimum, select it
                if (overlapDepths[i] < minDepth)
                {
                    minDepth = overlapDepths[i];
                    minAxis = i;
                }
            }

            if (minAxis == -1)
                return false;

            // Make sure the axis is facing towards the 0th box.
            // if not, invert it
            Vector3 D = newBox.GetCentre() - tri.Centre;
            Vector3 N = axes[minAxis];
            float depth = overlapDepths[minAxis];

            if (Vector3.Dot(D, N) < 0.0f)
               N *= -1;

            Vector3 boxOldPos = (info.Skin0.Owner != null) ? info.Skin0.Owner.OldPosition : Vector3.Zero;
            Vector3 boxNewPos = (info.Skin0.Owner != null) ? info.Skin0.Owner.Position : Vector3.Zero;
            Vector3 meshPos = (info.Skin1.Owner != null) ? info.Skin1.Owner.OldPosition : Vector3.Zero;

            List<Vector3> pts = new List<Vector3>();
            //pts.Clear();

            const float combinationDist = 0.05f;
            GetBoxTriangleIntersectionPoints(pts, newBox, tri, depth + combinationDist);

            // adjust the depth 
            #region REFERENCE: Vector3 delta = boxNewPos - boxOldPos;
            Vector3 delta;
            Vector3.Subtract(ref boxNewPos, ref boxOldPos, out delta);
            #endregion

            #region REFERENCE: float oldDepth = depth + Vector3.Dot(delta, N);
            float oldDepth;
            Vector3.Dot(ref delta, ref N, out oldDepth);
            oldDepth += depth;
            #endregion

            unsafe
            {
                // report collisions
                int numPts = pts.Count;
#if USE_STACKALLOC
                SmallCollPointInfo* collPts = stackalloc SmallCollPointInfo[MaxLocalStackSCPI];
#else
                SmallCollPointInfo[] collPtArray = SCPIStackAlloc();
                fixed (SmallCollPointInfo* collPts = collPtArray)
#endif
                {
                    if (numPts > 0)
                    {
                        if (numPts >= MaxLocalStackSCPI)
                        {
                            numPts = MaxLocalStackSCPI - 1;
                        }

                        // adjust positions
                        for (i = 0; i < numPts; ++i)
                        {
                            collPts[i] = new SmallCollPointInfo(pts[i] - boxNewPos, pts[i] - meshPos, oldDepth);
                        }

                        collisionFunctor.CollisionNotify(ref info, ref N, collPts, numPts);
#if !USE_STACKALLOC
                        FreeStackAlloc(collPtArray);
#endif
                        return true;
                    }
                    else
                    {
#if !USE_STACKALLOC
                        FreeStackAlloc(collPtArray);
#endif                       
                        return false;
                    }
                }

            }
        }

        private static bool CollDetectBoxStaticMeshOverlap(Box oldBox,
                                           Box newBox,
                                           TriangleMesh mesh,
                                           ref CollDetectInfo info,
                                           float collTolerance,
                                           CollisionFunctor collisionFunctor)
        {
            float boxRadius = newBox.GetBoundingRadiusAroundCentre();

            #region REFERENCE: Vector3 boxCentre = newBox.GetCentre();
            Vector3 boxCentre;
            newBox.GetCentre(out boxCentre);
            // Deano need to trasnform the box center into mesh space
            Matrix invTransformMatrix = mesh.InverseTransformMatrix;
            Vector3.Transform(ref boxCentre, ref invTransformMatrix, out boxCentre);
            #endregion

            BoundingBox bb = BoundingBoxHelper.InitialBox;
            BoundingBoxHelper.AddBox(newBox, ref bb);

            unsafe
            {
                bool collision = false;

#if USE_STACKALLOC
                int* potentialTriangles = stackalloc int[MaxLocalStackTris];
                {
#else
                int[] potTriArray = IntStackAlloc();
                fixed( int* potentialTriangles = potTriArray)
                {
#endif
                    // aabox is done in mesh space and handles the mesh transform correctly
                    int numTriangles = mesh.GetTrianglesIntersectingtAABox(potentialTriangles, MaxLocalStackTris, ref bb);

                    for (int iTriangle = 0; iTriangle < numTriangles; ++iTriangle)
                    {
                        IndexedTriangle meshTriangle = mesh.GetTriangle(potentialTriangles[iTriangle]);

                        // quick early test is done in mesh space
                        float dist = meshTriangle.Plane.DotCoordinate(boxCentre);

                        if (dist > boxRadius || dist < 0.0f)
                            continue;

                        if (DoOverlapBoxTriangleTest(
                              oldBox, newBox,
                              ref meshTriangle,
                              mesh,
                              ref info,
                              collTolerance,
                              collisionFunctor))
                        {
                            collision = true;
                        }
                    }
#if USE_STACKALLOC
                }
#else
                }
                FreeStackAlloc(potTriArray);
#endif
                return collision;
            }
        }
    }
}
