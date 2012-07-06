#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
using JigLibX.Utils;
#endregion

namespace JigLibX.Geometry
{
    public sealed class Overlap
    {
        // Missing: SegmentPlaneOverlap

        #region SegmentTriangleOverlap
        /// <summary>
        /// Indicates if a segment intersects a triangle
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="triangle"></param>
        /// <returns></returns>
        public static bool SegmentTriangleOverlap(Segment seg, Triangle triangle)
        {
            /// the parameters - if hit then they get copied into the args
            float u, v, t;

            Vector3 e1 = triangle.Edge0;
            Vector3 e2 = triangle.Edge1;

            Vector3 p = Vector3.Cross(seg.Delta, e2);
            float a = Vector3.Dot(e1, p);

            if (a > -JiggleMath.Epsilon && a < JiggleMath.Epsilon)
                return false;

            float f = 1.0f / a;
            Vector3 s = seg.Origin - triangle.Origin;
            u = f * Vector3.Dot(s, p);

            if (u < 0.0f || u > 1.0f)
                return false;

            Vector3 q = Vector3.Cross(s, e1);
            v = f * Vector3.Dot(seg.Delta, q);

            if (v < 0.0f || (u + v) > 1.0f)
                return false;

            t = f * Vector3.Dot(e2, q);

            if (t < 0.0f || t > 1.0f)
                return false;

            return true;
        }
        #endregion

        // Missing: SweptSpherePlaneOverlap

        #region SegmentAABoxOverlap
        /// <summary>
        /// Indicates if a segment overlaps an AABox
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="AABox"></param>
        /// <returns></returns>
        public static bool SegmentAABoxOverlap(Segment seg, AABox AABox)
        {
            Vector3 p0 = seg.Origin;
            Vector3 p1 = seg.GetEnd();

            float[] faceOffsets = new float[2];

            // The AABox faces are aligned with the world directions. Loop 
            // over the 3 directions and do the two tests.
            for (int iDir = 0; iDir < 3; iDir++)
            {
                int jDir = (iDir + 1) % 3;
                int kDir = (iDir + 2) % 3;

                // one plane goes through the origin, one is offset
                faceOffsets[0] = JiggleUnsafe.Get(AABox.MinPos, iDir);
                faceOffsets[1] = JiggleUnsafe.Get(AABox.MaxPos, iDir);

                for (int iFace = 0; iFace < 2; iFace++)
                {
                    // distance of each point from to the face plane
                    float dist0 = JiggleUnsafe.Get(ref p0, iDir) - faceOffsets[iFace];
                    float dist1 = JiggleUnsafe.Get(ref p1, iDir) - faceOffsets[iFace];
                    float frac = -1.0f;

                    if (dist0 * dist1 < -JiggleMath.Epsilon)
                        frac = -dist0 / (dist1 - dist0);
                    else if (System.Math.Abs(dist0) < JiggleMath.Epsilon)
                        frac = 0.0f;
                    else if (System.Math.Abs(dist1) < JiggleMath.Epsilon)
                        frac = 1.0f;

                    if (frac >= 0.0f)
                    {
                        //Assert(frac <= 1.0f);
                        Vector3 pt = seg.GetPoint(frac);

                        // check the point is within the face rectangle
                        if ((JiggleUnsafe.Get(ref pt, jDir) > JiggleUnsafe.Get(AABox.MinPos, jDir) - JiggleMath.Epsilon) &&
                            (JiggleUnsafe.Get(ref pt, jDir) < JiggleUnsafe.Get(AABox.MaxPos, jDir) + JiggleMath.Epsilon) &&
                            (JiggleUnsafe.Get(ref pt, kDir) > JiggleUnsafe.Get(AABox.MinPos, kDir) - JiggleMath.Epsilon) &&
                            (JiggleUnsafe.Get(ref pt, kDir) < JiggleUnsafe.Get(AABox.MaxPos, kDir) + JiggleMath.Epsilon))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        #endregion

    }
}
