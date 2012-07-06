#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
using JigLibX.Geometry;
#endregion

namespace JigLibX.Geometry
{

    public sealed class Distance
    {
        private Distance() { }

        #region SegmentTriangleDistanceSq
        public static float SegmentTriangleDistanceSq(out float segT, out float triT0, out float triT1, Segment seg, Triangle triangle)
        {
            // compare segment to all three edges of the triangle
            float distSq = float.MaxValue;

            if (Intersection.SegmentTriangleIntersection(out segT, out triT0, out triT1, seg, triangle))
            {
                segT = triT0 = triT1 = 0.0f;
                return 0.0f;
            }

            float s, t, u;
            float distEdgeSq;
            distEdgeSq = SegmentSegmentDistanceSq(out s, out t, seg, new Segment(triangle.Origin, triangle.Edge0));
            if (distEdgeSq < distSq)
            {
                distSq = distEdgeSq;
                segT = s;
                triT0 = t;
                triT1 = 0.0f;
            }
            distEdgeSq = SegmentSegmentDistanceSq(out s, out t, seg, new Segment(triangle.Origin, triangle.Edge1));
            if (distEdgeSq < distSq)
            {
                distSq = distEdgeSq;
                segT = s;
                triT0 = 0.0f;
                triT1 = t;
            }
            distEdgeSq = SegmentSegmentDistanceSq(out s, out t, seg, new Segment(triangle.Origin + triangle.Edge0, triangle.Edge2));
            if (distEdgeSq < distSq)
            {
                distSq = distEdgeSq;
                segT = s;
                triT0 = 1.0f - t;
                triT1 = t;
            }

            // compare segment end points to triangle interior
            float startTriSq = PointTriangleDistanceSq(out t, out u, seg.Origin, triangle);
            if (startTriSq < distSq)
            {
                distSq = startTriSq;
                segT = 0.0f;
                triT0 = t;
                triT1 = u;
            }
            float endTriSq = PointTriangleDistanceSq(out t, out u, seg.GetEnd(), triangle);
            if (endTriSq < distSq)
            {
                distSq = endTriSq;
                segT = 1.0f;
                triT0 = t;
                triT1 = u;
            }
            return distSq;
        }
        #endregion

        #region PointTriangleDistanceSq
        public static float PointTriangleDistanceSq(out float pfSParam, out float pfTParam, Vector3 rkPoint, Triangle rkTri)
        {
            Vector3 kDiff = rkTri.Origin - rkPoint;
            float fA00 = rkTri.Edge0.LengthSquared();
            float fA01 = Vector3.Dot(rkTri.Edge0, rkTri.Edge1);
            float fA11 = rkTri.Edge1.LengthSquared();
            float fB0 = Vector3.Dot(kDiff, rkTri.Edge0);
            float fB1 = Vector3.Dot(kDiff, rkTri.Edge1);
            float fC = kDiff.LengthSquared();
            float fDet = System.Math.Abs(fA00 * fA11 - fA01 * fA01);
            float fS = fA01 * fB1 - fA11 * fB0;
            float fT = fA01 * fB0 - fA00 * fB1;
            float fSqrDist;

            if (fS + fT <= fDet)
            {
                if (fS < 0.0f)
                {
                    if (fT < 0.0f)  // region 4
                    {
                        if (fB0 < 0.0f)
                        {
                            fT = 0.0f;
                            if (-fB0 >= fA00)
                            {
                                fS = 1.0f;
                                fSqrDist = fA00 + 2.0f * fB0 + fC;
                            }
                            else
                            {
                                fS = -fB0 / fA00;
                                fSqrDist = fB0 * fS + fC;
                            }
                        }
                        else
                        {
                            fS = 0.0f;
                            if (fB1 >= 0.0f)
                            {
                                fT = 0.0f;
                                fSqrDist = fC;
                            }
                            else if (-fB1 >= fA11)
                            {
                                fT = 1.0f;
                                fSqrDist = fA11 + 2.0f * fB1 + fC;
                            }
                            else
                            {
                                fT = -fB1 / fA11;
                                fSqrDist = fB1 * fT + fC;
                            }
                        }
                    }
                    else  // region 3
                    {
                        fS = 0.0f;
                        if (fB1 >= 0.0f)
                        {
                            fT = 0.0f;
                            fSqrDist = fC;
                        }
                        else if (-fB1 >= fA11)
                        {
                            fT = 1.0f;
                            fSqrDist = fA11 + 2.0f * fB1 + fC;
                        }
                        else
                        {
                            fT = -fB1 / fA11;
                            fSqrDist = fB1 * fT + fC;
                        }
                    }
                }
                else if (fT < 0.0f)  // region 5
                {
                    fT = 0.0f;
                    if (fB0 >= 0.0f)
                    {
                        fS = 0.0f;
                        fSqrDist = fC;
                    }
                    else if (-fB0 >= fA00)
                    {
                        fS = 1.0f;
                        fSqrDist = fA00 + 2.0f * fB0 + fC;
                    }
                    else
                    {
                        fS = -fB0 / fA00;
                        fSqrDist = fB0 * fS + fC;
                    }
                }
                else  // region 0
                {
                    // minimum at interior point
                    float fInvDet = 1.0f / fDet;
                    fS *= fInvDet;
                    fT *= fInvDet;
                    fSqrDist = fS * (fA00 * fS + fA01 * fT + 2.0f * fB0) +
                      fT * (fA01 * fS + fA11 * fT + 2.0f * fB1) + fC;
                }
            }
            else
            {
                float fTmp0, fTmp1, fNumer, fDenom;

                if (fS < 0.0f)  // region 2
                {
                    fTmp0 = fA01 + fB0;
                    fTmp1 = fA11 + fB1;
                    if (fTmp1 > fTmp0)
                    {
                        fNumer = fTmp1 - fTmp0;
                        fDenom = fA00 - 2.0f * fA01 + fA11;
                        if (fNumer >= fDenom)
                        {
                            fS = 1.0f;
                            fT = 0.0f;
                            fSqrDist = fA00 + 2.0f * fB0 + fC;
                        }
                        else
                        {
                            fS = fNumer / fDenom;
                            fT = 1.0f - fS;
                            fSqrDist = fS * (fA00 * fS + fA01 * fT + 2.0f * fB0) +
                              fT * (fA01 * fS + fA11 * fT + 2.0f * fB1) + fC;
                        }
                    }
                    else
                    {
                        fS = 0.0f;
                        if (fTmp1 <= 0.0f)
                        {
                            fT = 1.0f;
                            fSqrDist = fA11 + 2.0f * fB1 + fC;
                        }
                        else if (fB1 >= 0.0f)
                        {
                            fT = 0.0f;
                            fSqrDist = fC;
                        }
                        else
                        {
                            fT = -fB1 / fA11;
                            fSqrDist = fB1 * fT + fC;
                        }
                    }
                }
                else if (fT < 0.0f)  // region 6
                {
                    fTmp0 = fA01 + fB1;
                    fTmp1 = fA00 + fB0;
                    if (fTmp1 > fTmp0)
                    {
                        fNumer = fTmp1 - fTmp0;
                        fDenom = fA00 - 2.0f * fA01 + fA11;
                        if (fNumer >= fDenom)
                        {
                            fT = 1.0f;
                            fS = 0.0f;
                            fSqrDist = fA11 + 2.0f * fB1 + fC;
                        }
                        else
                        {
                            fT = fNumer / fDenom;
                            fS = 1.0f - fT;
                            fSqrDist = fS * (fA00 * fS + fA01 * fT + 2.0f * fB0) +
                              fT * (fA01 * fS + fA11 * fT + 2.0f * fB1) + fC;
                        }
                    }
                    else
                    {
                        fT = 0.0f;
                        if (fTmp1 <= 0.0f)
                        {
                            fS = 1.0f;
                            fSqrDist = fA00 + 2.0f * fB0 + fC;
                        }
                        else if (fB0 >= 0.0f)
                        {
                            fS = 0.0f;
                            fSqrDist = fC;
                        }
                        else
                        {
                            fS = -fB0 / fA00;
                            fSqrDist = fB0 * fS + fC;
                        }
                    }
                }
                else  // region 1
                {
                    fNumer = fA11 + fB1 - fA01 - fB0;
                    if (fNumer <= 0.0f)
                    {
                        fS = 0.0f;
                        fT = 1.0f;
                        fSqrDist = fA11 + 2.0f * fB1 + fC;
                    }
                    else
                    {
                        fDenom = fA00 - 2.0f * fA01 + fA11;
                        if (fNumer >= fDenom)
                        {
                            fS = 1.0f;
                            fT = 0.0f;
                            fSqrDist = fA00 + 2.0f * fB0 + fC;
                        }
                        else
                        {
                            fS = fNumer / fDenom;
                            fT = 1.0f - fS;
                            fSqrDist = fS * (fA00 * fS + fA01 * fT + 2.0f * fB0) +
                              fT * (fA01 * fS + fA11 * fT + 2.0f * fB1) + fC;
                        }
                    }
                }
            }

            pfSParam = fS;
            pfTParam = fT;

            return System.Math.Abs(fSqrDist);
        }
        #endregion

        #region PointPlaneDistance
        public static float PointPlaneDistance(Vector3 pt, Plane plane)
        {
            return Vector3.Dot(plane.Normal, pt) + plane.D;
        }

        public static float PointPlaneDistance(ref Vector3 pt, Plane plane)
        {
            float num0;
            Vector3.Dot(ref plane.normal, ref pt, out num0);
            return plane.D + num0;
        }
        #endregion

        #region PointPointDistanceSq

        public static float PointPointDistanceSq(Vector3 pt1, Vector3 pt2)
        {
            float num3 = pt1.X - pt2.X;
            float num2 = pt1.Y - pt2.Y;
            float num0 = pt1.Z - pt2.Z;
            return ((num3 * num3) + (num2 * num2)) + (num0 * num0);
        }

        public static void PointPointDistanceSq(ref Vector3 pt1, ref Vector3 pt2, out float result)
        {
            float num3 = pt1.X - pt2.X;
            float num2 = pt1.Y - pt2.Y;
            float num0 = pt1.Z - pt2.Z;
            result = ((num3 * num3) + (num2 * num2)) + (num0 * num0);
        }

        #endregion

        #region PointPointDistance

        public static float PointPointDistance(Vector3 pt1, Vector3 pt2)
        {
            float num3 = pt1.X - pt2.X;
            float num2 = pt1.Y - pt2.Y;
            float num0 = pt1.Z - pt2.Z;
            float num4 = ((num3 * num3) + (num2 * num2)) + (num0 * num0);
            return (float)System.Math.Sqrt((double)num4);
        }

        public static void PointPointDistance(ref Vector3 pt1, ref Vector3 pt2, out float result)
        {
            float num3 = pt1.X - pt2.X;
            float num2 = pt1.Y - pt2.Y;
            float num0 = pt1.Z - pt2.Z;
            float num4 = ((num3 * num3) + (num2 * num2)) + (num0 * num0);
            result = (float)System.Math.Sqrt((double)num4);
        }

        #endregion

        #region PointSegmentDistanceSq

        public static float PointSegmentDistanceSq(out float t, Vector3 pt, Segment seg)
        {
            Vector3 kDiff;
            float fT;

            Vector3.Subtract(ref pt, ref seg.Origin, out kDiff);
            Vector3.Dot(ref kDiff, ref seg.Delta, out fT);

            if (fT <= 0.0f)
            {
                fT = 0.0f;
            }
            else
            {
                float sqrLen = seg.Delta.LengthSquared();
                if (fT >= sqrLen)
                {
                    fT = 1.0f;
                    kDiff = kDiff - seg.Delta;
                }
                else
                {
                    fT = fT / sqrLen;
                    kDiff = kDiff - (fT * seg.Delta);
                }
            }

            t = fT;

            return kDiff.LengthSquared();
        }

        public static float PointSegmentDistanceSq(Vector3 pt, Segment seg)
        {
            Vector3 kDiff;
            float fT;

            Vector3.Subtract(ref pt, ref seg.Origin, out kDiff);
            Vector3.Dot(ref kDiff, ref seg.Delta, out fT);

            if (fT <= 0.0f)
            {
                fT = 0.0f;
            }
            else
            {
                float sqrLen = seg.Delta.LengthSquared();
                if (fT >= sqrLen)
                {
                    fT = 1.0f;
                    kDiff = kDiff - seg.Delta;
                }
                else
                {
                    fT = fT / sqrLen;
                    kDiff = kDiff - (fT * seg.Delta);
                }
            }

            return kDiff.LengthSquared();
        }

        public static void PointSegmentDistanceSq(out float t, out float result, ref Vector3 pt, ref Segment seg)
        {
            Vector3 kDiff;
            float fT;

            Vector3.Subtract(ref pt, ref seg.Origin, out kDiff);
            Vector3.Dot(ref kDiff, ref seg.Delta, out fT);

            if (fT <= 0.0f)
            {
                fT = 0.0f;
            }
            else
            {
                float sqrLen = seg.Delta.LengthSquared();
                if (fT >= sqrLen)
                {
                    fT = 1.0f;
                    kDiff = kDiff - seg.Delta;
                }
                else
                {
                    fT = fT / sqrLen;
                    kDiff = kDiff - (fT * seg.Delta);
                }
            }

            t = fT;

            result = kDiff.LengthSquared();
        }

        public static void PointSegmentDistanceSq(ref Vector3 pt, ref Segment seg, out float result)
        {
            Vector3 kDiff;
            float fT;

            Vector3.Subtract(ref pt, ref seg.Origin, out kDiff);
            Vector3.Dot(ref kDiff, ref seg.Delta, out fT);

            if (fT <= 0.0f)
            {
                fT = 0.0f;
            }
            else
            {
                float sqrLen = seg.Delta.LengthSquared();
                if (fT >= sqrLen)
                {
                    fT = 1.0f;
                    kDiff = kDiff - seg.Delta;
                }
                else
                {
                    fT = fT / sqrLen;
                    kDiff = kDiff - (fT * seg.Delta);
                }
            }

            result = kDiff.LengthSquared();
        }

        #endregion

        #region PointSegmentDistance

        public static float PointSegmentDistance(out float t, Vector3 pt, Segment seg)
        {
            Vector3 kDiff;
            float fT;

            Vector3.Subtract(ref pt, ref seg.Origin, out kDiff);
            Vector3.Dot(ref kDiff, ref seg.Delta, out fT);

            if (fT <= 0.0f)
            {
                fT = 0.0f;
            }
            else
            {
                float sqrLen = seg.Delta.LengthSquared();
                if (fT >= sqrLen)
                {
                    fT = 1.0f;
                    kDiff = kDiff - seg.Delta;
                }
                else
                {
                    fT = fT / sqrLen;
                    kDiff = kDiff - (fT * seg.Delta);
                }
            }

            t = fT;

            return kDiff.Length();
        }

        public static float PointSegmentDistance(Vector3 pt, Segment seg)
        {
            Vector3 kDiff;
            float fT;

            Vector3.Subtract(ref pt, ref seg.Origin, out kDiff);
            Vector3.Dot(ref kDiff, ref seg.Delta, out fT);

            if (fT <= 0.0f)
            {
                fT = 0.0f;
            }
            else
            {
                float sqrLen = seg.Delta.LengthSquared();
                if (fT >= sqrLen)
                {
                    fT = 1.0f;
                    kDiff = kDiff - seg.Delta;
                }
                else
                {
                    fT = fT / sqrLen;
                    kDiff = kDiff - (fT * seg.Delta);
                }
            }

            return kDiff.Length();
        }

        public static void PointSegmentDistance(out float t, out float result, ref Vector3 pt, ref Segment seg)
        {
            Vector3 kDiff;
            float fT;

            Vector3.Subtract(ref pt, ref seg.Origin, out kDiff);
            Vector3.Dot(ref kDiff, ref seg.Delta, out fT);

            if (fT <= 0.0f)
            {
                fT = 0.0f;
            }
            else
            {
                float sqrLen = seg.Delta.LengthSquared();
                if (fT >= sqrLen)
                {
                    fT = 1.0f;
                    kDiff = kDiff - seg.Delta;
                }
                else
                {
                    fT = fT / sqrLen;
                    kDiff = kDiff - (fT * seg.Delta);
                }
            }

            t = fT;

            result = kDiff.Length();
        }

        public static void PointSegmentDistance(ref Vector3 pt, ref Segment seg, out float result)
        {
            Vector3 kDiff;
            float fT;

            Vector3.Subtract(ref pt, ref seg.Origin, out kDiff);
            Vector3.Dot(ref kDiff, ref seg.Delta, out fT);

            if (fT <= 0.0f)
            {
                fT = 0.0f;
            }
            else
            {
                float sqrLen = seg.Delta.LengthSquared();
                if (fT >= sqrLen)
                {
                    fT = 1.0f;
                    kDiff = kDiff - seg.Delta;
                }
                else
                {
                    fT = fT / sqrLen;
                    kDiff = kDiff - (fT * seg.Delta);
                }
            }

            result = kDiff.Length();
        }

        #endregion

        #region SegmentBoxDistanceSq
        public static float SegmentBoxDistanceSq(out float pfLParam,
            out float pfBParam0, out float pfBParam1, out float pfBParam2,
            Segment rkSeg, Box rkBox)
        {
            pfLParam = pfBParam0 = pfBParam1 = pfBParam2 = 0.0f;

            Line line = new Line(rkSeg.Origin, rkSeg.Delta);

            float fLP, fBP0, fBP1, fBP2;
            float fSqrDistance = SqrDistance(line, rkBox, out fLP, out fBP0, out  fBP1, out fBP2);

            if (fLP >= 0.0f)
            {
                if (fLP <= 1.0f)
                {
                    pfLParam = fLP;
                    pfBParam0 = fBP0;// + 0.5f;
                    pfBParam1 = fBP1;// + 0.5f;
                    pfBParam2 = fBP2;// + 0.5f;

                    return MathHelper.Max(fSqrDistance, 0.0f);
                }
                else
                {
                    fSqrDistance = SqrDistance(rkSeg.Origin + rkSeg.Delta,
                                               rkBox, out pfBParam0, out pfBParam1, out  pfBParam2);

                    pfLParam = 1.0f;
                    return MathHelper.Max(fSqrDistance, 0.0f);
                }
            }
            else
            {
                fSqrDistance = SqrDistance(rkSeg.Origin, rkBox, out  pfBParam0, out  pfBParam1, out  pfBParam2);

                pfLParam = 0.0f;

                return MathHelper.Max(fSqrDistance, 0.0f);
            }

        }
        #endregion

        #region SqrDistance
        public static float SqrDistance(Vector3 point, Box box,
            out float pfBParam0, out float pfBParam1, out float pfBParam2)
        {
            // compute coordinates of point in box coordinate system
            Vector3 kDiff = point - box.GetCentre();
            Vector3 kClosest = new Vector3(Vector3.Dot(kDiff, box.Orientation.Right),
                              Vector3.Dot(kDiff, box.Orientation.Up),
                              Vector3.Dot(kDiff, box.Orientation.Backward));

            // project test point onto box
            float fSqrDistance = 0.0f;
            float fDelta;

            if (kClosest.X < -box.GetHalfSideLengths().X)
            {
                fDelta = kClosest.X + box.GetHalfSideLengths().X;
                fSqrDistance += fDelta * fDelta;
                kClosest.X = -box.GetHalfSideLengths().X;
            }
            else if (kClosest.X > box.GetHalfSideLengths().X)
            {
                fDelta = kClosest.X - box.GetHalfSideLengths().X;
                fSqrDistance += fDelta * fDelta;
                kClosest.X = box.GetHalfSideLengths().X;
            }

            if (kClosest.Y < -box.GetHalfSideLengths().Y)
            {
                fDelta = kClosest.Y + box.GetHalfSideLengths().Y;
                fSqrDistance += fDelta * fDelta;
                kClosest.Y = -box.GetHalfSideLengths().Y;
            }
            else if (kClosest.Y > box.GetHalfSideLengths().Y)
            {
                fDelta = kClosest.Y - box.GetHalfSideLengths().Y;
                fSqrDistance += fDelta * fDelta;
                kClosest.Y = box.GetHalfSideLengths().Y;
            }

            if (kClosest.Z < -box.GetHalfSideLengths().Z)
            {
                fDelta = kClosest.Z + box.GetHalfSideLengths().Z;
                fSqrDistance += fDelta * fDelta;
                kClosest.Z = -box.GetHalfSideLengths().Z;
            }
            else if (kClosest.Z > box.GetHalfSideLengths().Z)
            {
                fDelta = kClosest.Z - box.GetHalfSideLengths().Z;
                fSqrDistance += fDelta * fDelta;
                kClosest.Z = box.GetHalfSideLengths().Z;
            }


            pfBParam0 = kClosest.X;
            pfBParam1 = kClosest.Y;
            pfBParam2 = kClosest.Z;

            return MathHelper.Max(fSqrDistance, 0.0f);
        }
        #endregion

        #region SqrDistance
        public static float SqrDistance(Line line, Box box, out float pfLParam,
            out float pfBParam0, out float pfBParam1, out float pfBParam2)
        {
            // compute coordinates of line in box coordinate system
            Vector3 diff = line.Origin - box.GetCentre();
            Vector3 pnt = new Vector3(Vector3.Dot(diff, box.Orientation.Right),
                Vector3.Dot(diff, box.Orientation.Up),
                Vector3.Dot(diff, box.Orientation.Backward));
            Vector3 kDir = new Vector3(Vector3.Dot(line.Dir, box.Orientation.Right),
                Vector3.Dot(line.Dir, box.Orientation.Up),
                 Vector3.Dot(line.Dir, box.Orientation.Backward));

            // Apply reflections so that direction vector has nonnegative components.
            bool reflect0 = false;
            bool reflect1 = false;
            bool reflect2 = false;
            pfLParam = 0;

            if (kDir.X < 0.0f)
            {
                pnt.X = -pnt.X;
                kDir.X = -kDir.X;
                reflect0 = true;
            }

            if (kDir.Y < 0.0f)
            {
                pnt.Y = -pnt.Y;
                kDir.Y = -kDir.Y;
                reflect1 = true;
            }

            if (kDir.Z < 0.0f)
            {
                pnt.Z = -pnt.Z;
                kDir.Z = -kDir.Z;
                reflect2 = true;
            }

            float sqrDistance = 0.0f;

            if (kDir.X > 0.0f)
            {
                if (kDir.Y > 0.0f)
                {
                    if (kDir.Z > 0.0f)
                    {
                        // (+,+,+)
                        Vector3 kPmE = pnt - box.GetHalfSideLengths();

                        float prodDxPy = kDir.X * kPmE.Y;
                        float prodDyPx = kDir.Y * kPmE.X;
                        float prodDzPx, prodDxPz, prodDzPy, prodDyPz;

                        if (prodDyPx >= prodDxPy)
                        {
                            prodDzPx = kDir.Z * kPmE.X;
                            prodDxPz = kDir.X * kPmE.Z;
                            if (prodDzPx >= prodDxPz)
                            {
                                //Face(0,1,2)
                                FaceA(ref pnt, kDir, box, kPmE, out pfLParam, ref sqrDistance);
                            }
                            else
                            {
                                //Face(2,0,1)
                                FaceB(ref pnt, kDir, box, kPmE, out pfLParam, ref sqrDistance);
                            }
                        }
                        else
                        {
                            prodDzPy = kDir.Z * kPmE.Y;
                            prodDyPz = kDir.Y * kPmE.Z;
                            if (prodDzPy >= prodDyPz)
                            {
                                //Face(1,2,0)
                                FaceC(ref pnt, kDir, box, kPmE, out pfLParam, ref sqrDistance);
                            }
                            else
                            {
                                //Face(2,0,1)
                                FaceB(ref pnt, kDir, box, kPmE, out pfLParam, ref sqrDistance);
                            }
                        }
                    }
                    else
                    {
                        // (+,+,0)
                        float pmE0 = pnt.X - box.GetHalfSideLengths().X;
                        float pmE1 = pnt.Y - box.GetHalfSideLengths().Y;
                        float prod0 = kDir.Y * pmE0;
                        float prod1 = kDir.X * pmE1;
                        float delta, invLSqur, inv;

                        if (prod0 >= prod1)
                        {
                            // line intersects P[i0] = e[i0]
                            pnt.X = box.GetHalfSideLengths().X;

                            float ppE1 = pnt.Y + box.GetHalfSideLengths().Y;
                            delta = prod0 - kDir.X * ppE1;

                            if (delta >= 0.0f)
                            {
                                invLSqur = 1.0f / (kDir.X * kDir.X + kDir.Y * kDir.Y);
                                sqrDistance += delta * delta * invLSqur;

                                pnt.Y = -box.GetHalfSideLengths().Y;
                                pfLParam = -(kDir.X * pmE0 + kDir.Y * ppE1) * invLSqur;
                            }
                            else
                            {
                                inv = 1.0f / kDir.X;
                                pnt.Y -= prod0 * inv;
                                pfLParam = -pmE0 * inv;
                            }
                        }
                        else
                        {
                            // line intersects P[i1] = e[i1]
                            pnt.Y = box.GetHalfSideLengths().Y;

                            float ppE0 = pnt.X + box.GetHalfSideLengths().X;
                            delta = prod1 - kDir.Y * ppE0;
                            if (delta >= 0.0f)
                            {
                                invLSqur = 1.0f / (kDir.X * kDir.X + kDir.Y * kDir.Y);
                                sqrDistance += delta * delta * invLSqur;

                                pnt.X = -box.GetHalfSideLengths().X;
                                pfLParam = -(kDir.X * ppE0 + kDir.Y * pmE1) * invLSqur;
                            }
                            else
                            {
                                inv = 1.0f / kDir.Y;
                                pnt.X -= prod1 * inv;
                                pfLParam = -pmE1 * inv;
                            }

                        }

                        if (pnt.Z < -box.GetHalfSideLengths().Z)
                        {
                            delta = pnt.Z + box.GetHalfSideLengths().Z;
                            sqrDistance += delta * delta;
                            pnt.Z = -box.GetHalfSideLengths().Z;
                        }
                        else if (pnt.Z > box.GetHalfSideLengths().Z)
                        {
                            delta = pnt.Z - box.GetHalfSideLengths().Z;
                            sqrDistance += delta * delta;
                            pnt.Z = box.GetHalfSideLengths().Z;
                        }

                    }
                }
                else
                {
                    if (kDir.Z > 0.0f)
                    {
                        // (+,0,+)
                        float pmE0 = pnt.X - box.GetHalfSideLengths().X;
                        float pmE1 = pnt.Z - box.GetHalfSideLengths().Z;
                        float prod0 = kDir.Z * pmE0;
                        float prod1 = kDir.X * pmE1;
                        float delta, invLSqur, inv;

                        if (prod0 >= prod1)
                        {
                            // line intersects P[i0] = e[i0]
                            pnt.X = box.GetHalfSideLengths().X;

                            float ppE1 = pnt.Z + box.GetHalfSideLengths().Z;
                            delta = prod0 - kDir.X * ppE1;

                            if (delta >= 0.0f)
                            {
                                invLSqur = 1.0f / (kDir.X * kDir.X + kDir.Z * kDir.Z);
                                sqrDistance += delta * delta * invLSqur;

                                pnt.Z = -box.GetHalfSideLengths().Z;
                                pfLParam = -(kDir.X * pmE0 + kDir.Z * ppE1) * invLSqur;
                            }
                            else
                            {
                                inv = 1.0f / kDir.X;
                                pnt.Z -= prod0 * inv;
                                pfLParam = -pmE0 * inv;
                            }
                        }
                        else
                        {
                            // line intersects P[i1] = e[i1]
                            pnt.Z = box.GetHalfSideLengths().Z;

                            float ppE0 = pnt.X + box.GetHalfSideLengths().X;
                            delta = prod1 - kDir.Z * ppE0;
                            if (delta >= 0.0f)
                            {
                                invLSqur = 1.0f / (kDir.X * kDir.X + kDir.Z * kDir.Z);
                                sqrDistance += delta * delta * invLSqur;

                                pnt.X = -box.GetHalfSideLengths().X;
                                pfLParam = -(kDir.X * ppE0 + kDir.Z * pmE1) * invLSqur;
                            }
                            else
                            {
                                inv = 1.0f / kDir.Z;
                                pnt.X -= prod1 * inv;
                                pfLParam = -pmE1 * inv;
                            }

                        }

                        if (pnt.Y < -box.GetHalfSideLengths().Y)
                        {
                            delta = pnt.Y + box.GetHalfSideLengths().Y;
                            sqrDistance += delta * delta;
                            pnt.Y = -box.GetHalfSideLengths().Y;
                        }
                        else if (pnt.Y > box.GetHalfSideLengths().Y)
                        {
                            delta = pnt.Y - box.GetHalfSideLengths().Y;
                            sqrDistance += delta * delta;
                            pnt.Y = box.GetHalfSideLengths().Y;
                        }
                    }
                    else
                    {
                        // (+,0,0)
                        float pmE0 = pnt.X - box.GetHalfSideLengths().X;
                        float pmE1 = pnt.Y - box.GetHalfSideLengths().Y;
                        float prod0 = kDir.Y * pmE0;
                        float prod1 = kDir.X * pmE1;
                        float delta, invLSqur, inv;

                        if (prod0 >= prod1)
                        {
                            // line intersects P[i0] = e[i0]
                            pnt.X = box.GetHalfSideLengths().X;

                            float ppE1 = pnt.Y + box.GetHalfSideLengths().Y;
                            delta = prod0 - kDir.X * ppE1;

                            if (delta >= 0.0f)
                            {
                                invLSqur = 1.0f / (kDir.X * kDir.X + kDir.Y * kDir.Y);
                                sqrDistance += delta * delta * invLSqur;

                                pnt.Y = -box.GetHalfSideLengths().Y;
                                pfLParam = -(kDir.X * pmE0 + kDir.Y * ppE1) * invLSqur;
                            }
                            else
                            {
                                inv = 1.0f / kDir.X;
                                pnt.Y -= prod0 * inv;
                                pfLParam = -pmE0 * inv;
                            }
                        }
                        else
                        {
                            // line intersects P[i1] = e[i1]
                            pnt.Y = box.GetHalfSideLengths().Y;

                            float ppE0 = pnt.X + box.GetHalfSideLengths().X;
                            delta = prod1 - kDir.Y * ppE0;
                            if (delta >= 0.0f)
                            {
                                invLSqur = 1.0f / (kDir.X * kDir.X + kDir.Y * kDir.Y);
                                sqrDistance += delta * delta * invLSqur;

                                pnt.X = -box.GetHalfSideLengths().X;
                                pfLParam = -(kDir.X * ppE0 + kDir.Y * pmE1) * invLSqur;
                            }
                            else
                            {
                                inv = 1.0f / kDir.Y;
                                pnt.X -= prod1 * inv;
                                pfLParam = -pmE1 * inv;
                            }

                        }

                        if (pnt.Z < -box.GetHalfSideLengths().Z)
                        {
                            delta = pnt.Z + box.GetHalfSideLengths().Z;
                            sqrDistance += delta * delta;
                            pnt.Z = -box.GetHalfSideLengths().Z;
                        }
                        else if (pnt.Z > box.GetHalfSideLengths().Z)
                        {
                            delta = pnt.Z - box.GetHalfSideLengths().Z;
                            sqrDistance += delta * delta;
                            pnt.Z = box.GetHalfSideLengths().Z;
                        }
                    }
                }
            }
            else
            {
                if (kDir.Y > 0.0f)
                {
                    if (kDir.Z > 0.0f)
                    {
                        float pmE0 = pnt.Y - box.GetHalfSideLengths().Y;
                        float pmE1 = pnt.Z - box.GetHalfSideLengths().Z;
                        float prod0 = kDir.Z * pmE0;
                        float prod1 = kDir.Y * pmE1;
                        float delta, invLSqur, inv;

                        if (prod0 >= prod1)
                        {
                            // line intersects P[i0] = e[i0]
                            pnt.Y = box.GetHalfSideLengths().Y;

                            float ppE1 = pnt.Z + box.GetHalfSideLengths().Z;
                            delta = prod0 - kDir.Y * ppE1;

                            if (delta >= 0.0f)
                            {
                                invLSqur = 1.0f / (kDir.Y * kDir.Y + kDir.Z * kDir.Z);
                                sqrDistance += delta * delta * invLSqur;

                                pnt.Z = -box.GetHalfSideLengths().Z;
                                pfLParam = -(kDir.Y * pmE0 + kDir.Z * ppE1) * invLSqur;
                            }
                            else
                            {
                                inv = 1.0f / kDir.Y;
                                pnt.Z -= prod0 * inv;
                                pfLParam = -pmE0 * inv;
                            }
                        }
                        else
                        {
                            // line intersects P[i1] = e[i1]
                            pnt.Z = box.GetHalfSideLengths().Z;

                            float ppE0 = pnt.Y + box.GetHalfSideLengths().Y;
                            delta = prod1 - kDir.Z * ppE0;
                            if (delta >= 0.0f)
                            {
                                invLSqur = 1.0f / (kDir.Y * kDir.Y + kDir.Z * kDir.Z);
                                sqrDistance += delta * delta * invLSqur;

                                pnt.Y = -box.GetHalfSideLengths().Y;
                                pfLParam = -(kDir.Y * ppE0 + kDir.Z * pmE1) * invLSqur;
                            }
                            else
                            {
                                inv = 1.0f / kDir.Z;
                                pnt.Y -= prod1 * inv;
                                pfLParam = -pmE1 * inv;
                            }

                        }

                        if (pnt.X < -box.GetHalfSideLengths().X)
                        {
                            delta = pnt.X + box.GetHalfSideLengths().X;
                            sqrDistance += delta * delta;
                            pnt.X = -box.GetHalfSideLengths().X;
                        }
                        else if (pnt.X > box.GetHalfSideLengths().X)
                        {
                            delta = pnt.X - box.GetHalfSideLengths().X;
                            sqrDistance += delta * delta;
                            pnt.X = box.GetHalfSideLengths().X;
                        }

                    }
                    else
                    {
                        // (0,+,0)
                        float delta;

                        pfLParam = (box.GetHalfSideLengths().Y - pnt.Y) / kDir.Y;

                        pnt.Y = box.GetHalfSideLengths().Y;

                        if (pnt.X < -box.GetHalfSideLengths().X)
                        {
                            delta = pnt.X + box.GetHalfSideLengths().X;
                            sqrDistance += delta * delta;
                            pnt.X = -box.GetHalfSideLengths().X;
                        }
                        else if (pnt.X > box.GetHalfSideLengths().X)
                        {
                            delta = pnt.X - box.GetHalfSideLengths().X;
                            sqrDistance += delta * delta;
                            pnt.X = box.GetHalfSideLengths().X;
                        }

                        if (pnt.Z < -box.GetHalfSideLengths().Z)
                        {
                            delta = pnt.Z + box.GetHalfSideLengths().Z;
                            sqrDistance += delta * delta;
                            pnt.Z = -box.GetHalfSideLengths().Z;
                        }
                        else if (pnt.Z > box.GetHalfSideLengths().Z)
                        {
                            delta = pnt.Z - box.GetHalfSideLengths().Z;
                            sqrDistance += delta * delta;
                            pnt.Z = box.GetHalfSideLengths().Z;
                        }
                    }
                }
                else
                {
                    if (kDir.Z > 0.0f)
                    {
                        float delta;

                        pfLParam = (box.GetHalfSideLengths().Z - pnt.Z) / kDir.Z;

                        pnt.Z = box.GetHalfSideLengths().Z;

                        if (pnt.X < -box.GetHalfSideLengths().X)
                        {
                            delta = pnt.X + box.GetHalfSideLengths().X;
                            sqrDistance += delta * delta;
                            pnt.X = -box.GetHalfSideLengths().X;
                        }
                        else if (pnt.X > box.GetHalfSideLengths().X)
                        {
                            delta = pnt.X - box.GetHalfSideLengths().X;
                            sqrDistance += delta * delta;
                            pnt.X = box.GetHalfSideLengths().X;
                        }

                        if (pnt.Y < -box.GetHalfSideLengths().Y)
                        {
                            delta = pnt.Y + box.GetHalfSideLengths().Y;
                            sqrDistance += delta * delta;
                            pnt.Y = -box.GetHalfSideLengths().Y;
                        }
                        else if (pnt.Y > box.GetHalfSideLengths().Y)
                        {
                            delta = pnt.Y - box.GetHalfSideLengths().Y;
                            sqrDistance += delta * delta;
                            pnt.Y = box.GetHalfSideLengths().Y;
                        }
                    }
                    else
                    {
                        // (0,0,0)
                        float delta;

                        if (pnt.X < -box.GetHalfSideLengths().X)
                        {
                            delta = pnt.X + box.GetHalfSideLengths().X;
                            sqrDistance += delta * delta;
                            pnt.X = -box.GetHalfSideLengths().X;
                        }
                        else if (pnt.X > box.GetHalfSideLengths().X)
                        {
                            delta = pnt.X - box.GetHalfSideLengths().X;
                            sqrDistance += delta * delta;
                            pnt.X = box.GetHalfSideLengths().X;
                        }

                        if (pnt.Y < -box.GetHalfSideLengths().Y)
                        {
                            delta = pnt.Y + box.GetHalfSideLengths().Y;
                            sqrDistance += delta * delta;
                            pnt.Y = -box.GetHalfSideLengths().Y;
                        }
                        else if (pnt.Y > box.GetHalfSideLengths().Y)
                        {
                            delta = pnt.Y - box.GetHalfSideLengths().Y;
                            sqrDistance += delta * delta;
                            pnt.Y = box.GetHalfSideLengths().Y;
                        }

                        if (pnt.Z < -box.GetHalfSideLengths().Z)
                        {
                            delta = pnt.Z + box.GetHalfSideLengths().Z;
                            sqrDistance += delta * delta;
                            pnt.Z = -box.GetHalfSideLengths().Z;
                        }
                        else if (pnt.Z > box.GetHalfSideLengths().Z)
                        {
                            delta = pnt.Z - box.GetHalfSideLengths().Z;
                            sqrDistance += delta * delta;
                            pnt.Z = box.GetHalfSideLengths().Z;
                        }
                    }
                }
            }

            // undo reflections
            if (reflect0) pnt.X = -pnt.X;
            if (reflect1) pnt.Y = -pnt.Y;
            if (reflect2) pnt.Z = -pnt.Z;

            pfBParam0 = pnt.X;
            pfBParam1 = pnt.Y;
            pfBParam2 = pnt.Z;

            return MathHelper.Max(sqrDistance, 0.0f);
        }

        private static void FaceA(ref Vector3 kPnt,
                 Vector3 kDir, Box rkBox,
                 Vector3 kPmE,out float pfLParam,ref float sqrDistance)
        {
            // 0,1,2
            Vector3 kPpE;
            float fLSqr, fInv, fTmp, fParam, fT, fDelta;

            kPpE.Y = kPnt.Y + rkBox.GetHalfSideLengths().Y;
            kPpE.Z = kPnt.Z + rkBox.GetHalfSideLengths().Z;
            if (kDir.X * kPpE.Y >= kDir.Y * kPmE.X)
            {
                if (kDir.X * kPpE.Z >= kDir.Z * kPmE.X)
                {
                    // v.Y >= -e.Y, v.Z >= -e.Z (distance = 0)
                    kPnt.X = rkBox.GetHalfSideLengths().X;
                    fInv = 1.0f / kDir.X;
                    kPnt.Y -= kDir.Y * kPmE.X * fInv;
                    kPnt.Z -= kDir.Z * kPmE.X * fInv;
                    pfLParam = -kPmE.X * fInv;
                }
                else
                {
                    // v.Y >= -e.Y, v.Z < -e.Z
                    fLSqr = kDir.X * kDir.X + kDir.Z * kDir.Z;
                    fTmp = fLSqr * kPpE.Y - kDir.Y * (kDir.X * kPmE.X +
                                                       kDir.Z * kPpE.Z);
                    if (fTmp <= 2.0f * fLSqr * rkBox.GetHalfSideLengths().Y)
                    {
                        fT = fTmp / fLSqr;
                        fLSqr += kDir.Y * kDir.Y;
                        fTmp = kPpE.Y - fT;
                        fDelta = kDir.X * kPmE.X + kDir.Y * fTmp +
                          kDir.Z * kPpE.Z;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.X * kPmE.X + fTmp * fTmp +
                          kPpE.Z * kPpE.Z + fDelta * fParam;

                        pfLParam = fParam;
                        kPnt.X = rkBox.GetHalfSideLengths().X;
                        kPnt.Y = fT - rkBox.GetHalfSideLengths().Y;
                        kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                    }
                    else
                    {
                        fLSqr += kDir.Y * kDir.Y;
                        fDelta = kDir.X * kPmE.X + kDir.Y * kPmE.Y +
                          kDir.Z * kPpE.Z;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.X * kPmE.X + kPmE.Y * kPmE.Y +
                          kPpE.Z * kPpE.Z + fDelta * fParam;

                        pfLParam = fParam;
                        kPnt.X = rkBox.GetHalfSideLengths().X;
                        kPnt.Y = rkBox.GetHalfSideLengths().Y;
                        kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                    }
                }
            }
            else
            {
                if (kDir.X * kPpE.Z >= kDir.Z * kPmE.X)
                {
                    // v.Y < -e.Y, v.Z >= -e.Z
                    fLSqr = kDir.X * kDir.X + kDir.Y * kDir.Y;
                    fTmp = fLSqr * kPpE.Z - kDir.Z * (kDir.X * kPmE.X +
                                                       kDir.Y * kPpE.Y);
                    if (fTmp <= 2.0f * fLSqr * rkBox.GetHalfSideLengths().Z)
                    {
                        fT = fTmp / fLSqr;
                        fLSqr += kDir.Z * kDir.Z;
                        fTmp = kPpE.Z - fT;
                        fDelta = kDir.X * kPmE.X + kDir.Y * kPpE.Y +
                          kDir.Z * fTmp;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.X * kPmE.X + kPpE.Y * kPpE.Y +
                          fTmp * fTmp + fDelta * fParam;

                        pfLParam = fParam;
                        kPnt.X = rkBox.GetHalfSideLengths().X;
                        kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                        kPnt.Z = fT - rkBox.GetHalfSideLengths().Z;

                    }
                    else
                    {
                        fLSqr += kDir.Z * kDir.Z;
                        fDelta = kDir.X * kPmE.X + kDir.Y * kPpE.Y +
                          kDir.Z * kPmE.Z;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.X * kPmE.X + kPpE.Y * kPpE.Y +
                          kPmE.Z * kPmE.Z + fDelta * fParam;


                        pfLParam = fParam;
                        kPnt.X = rkBox.GetHalfSideLengths().X;
                        kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                        kPnt.Z = rkBox.GetHalfSideLengths().Z;
                    }
                }
                else
                {
                    // v.Y < -e.Y, v.Z < -e.Z
                    fLSqr = kDir.X * kDir.X + kDir.Z * kDir.Z;
                    fTmp = fLSqr * kPpE.Y - kDir.Y * (kDir.X * kPmE.X +
                                                       kDir.Z * kPpE.Z);
                    if (fTmp >= 0.0f)
                    {
                        // v.Y-edge is closest
                        if (fTmp <= 2.0f * fLSqr * rkBox.GetHalfSideLengths().Y)
                        {
                            fT = fTmp / fLSqr;
                            fLSqr += kDir.Y * kDir.Y;
                            fTmp = kPpE.Y - fT;
                            fDelta = kDir.X * kPmE.X + kDir.Y * fTmp +
                              kDir.Z * kPpE.Z;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.X * kPmE.X + fTmp * fTmp +
                              kPpE.Z * kPpE.Z + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.X = rkBox.GetHalfSideLengths().X;
                            kPnt.Y = fT - rkBox.GetHalfSideLengths().Y;
                            kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                        }
                        else
                        {
                            fLSqr += kDir.Y * kDir.Y;
                            fDelta = kDir.X * kPmE.X + kDir.Y * kPmE.Y +
                              kDir.Z * kPpE.Z;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.X * kPmE.X + kPmE.Y * kPmE.Y
                              + kPpE.Z * kPpE.Z + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.X = rkBox.GetHalfSideLengths().X;
                            kPnt.Y = rkBox.GetHalfSideLengths().Y;
                            kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                        }
                        return;
                    }

                    fLSqr = kDir.X * kDir.X + kDir.Y * kDir.Y;
                    fTmp = fLSqr * kPpE.Z - kDir.Z * (kDir.X * kPmE.X +
                                                       kDir.Y * kPpE.Y);
                    if (fTmp >= 0.0f)
                    {
                        // v.Z-edge is closest
                        if (fTmp <= (2.0f * fLSqr * rkBox.GetHalfSideLengths().Z))
                        {
                            fT = fTmp / fLSqr;
                            fLSqr += kDir.Z * kDir.Z;
                            fTmp = kPpE.Z - fT;
                            fDelta = kDir.X * kPmE.X + kDir.Y * kPpE.Y +
                              kDir.Z * fTmp;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.X * kPmE.X + kPpE.Y * kPpE.Y +
                              fTmp * fTmp + fDelta * fParam;

                                pfLParam = fParam;
                                kPnt.X = rkBox.GetHalfSideLengths().X;
                                kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                                kPnt.Z = fT - rkBox.GetHalfSideLengths().Z;
                        }
                        else
                        {
                            fLSqr += kDir.Z * kDir.Z;
                            fDelta = kDir.X * kPmE.X + kDir.Y * kPpE.Y +
                              kDir.Z * kPmE.Z;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.X * kPmE.X + kPpE.Y * kPpE.Y +
                              kPmE.Z * kPmE.Z + fDelta * fParam;

                                pfLParam = fParam;
                                kPnt.X = rkBox.GetHalfSideLengths().X;
                                kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                                kPnt.Z = rkBox.GetHalfSideLengths().Z;
                        }
                        return;
                    }

                    // (v.Y,v.Z)-corner is closest
                    fLSqr += kDir.Z * kDir.Z;
                    fDelta = kDir.X * kPmE.X + kDir.Y * kPpE.Y +
                      kDir.Z * kPpE.Z;
                    fParam = -fDelta / fLSqr;
                    sqrDistance += kPmE.X * kPmE.X + kPpE.Y * kPpE.Y +
                      kPpE.Z * kPpE.Z + fDelta * fParam;


                    pfLParam = fParam;
                    kPnt.X = rkBox.GetHalfSideLengths().X;
                    kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                    kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                }
            }
        }

        private static void FaceB(ref Vector3 kPnt,
               Vector3 kDir, Box rkBox,
               Vector3 kPmE, out float pfLParam,ref float sqrDistance)
        {
            // 2,0,1
            Vector3 kPpE;
            float fLSqr, fInv, fTmp, fParam, fT, fDelta;

            kPpE.X = kPnt.X + rkBox.GetHalfSideLengths().X;
            kPpE.Y = kPnt.Y + rkBox.GetHalfSideLengths().Y;
            if (kDir.Z * kPpE.X >= kDir.X * kPmE.Z)
            {
                if (kDir.Z * kPpE.Y >= kDir.Y * kPmE.Z)
                {
                    // v.X >= -e.X, v.Y >= -e.Y (distance = 0)
                    kPnt.Z = rkBox.GetHalfSideLengths().Z;
                    fInv = 1.0f / kDir.Z;
                    kPnt.X -= kDir.X * kPmE.Z * fInv;
                    kPnt.Y -= kDir.Y * kPmE.Z * fInv;
                    pfLParam = -kPmE.Z * fInv;
                }
                else
                {
                    // v.X >= -e.X, v.Y < -e.Y
                    fLSqr = kDir.Z * kDir.Z + kDir.Y * kDir.Y;
                    fTmp = fLSqr * kPpE.X - kDir.X * (kDir.Z * kPmE.Z +
                                                       kDir.Y * kPpE.Y);
                    if (fTmp <= 2.0f * fLSqr * rkBox.GetHalfSideLengths().X)
                    {
                        fT = fTmp / fLSqr;
                        fLSqr += kDir.X * kDir.X;
                        fTmp = kPpE.X - fT;
                        fDelta = kDir.Z * kPmE.Z + kDir.X * fTmp +
                          kDir.Y * kPpE.Y;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.Z * kPmE.Z + fTmp * fTmp +
                          kPpE.Y * kPpE.Y + fDelta * fParam;

                        pfLParam = fParam;
                        kPnt.Z = rkBox.GetHalfSideLengths().Z;
                        kPnt.X = fT - rkBox.GetHalfSideLengths().X;
                        kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                    }
                    else
                    {
                        fLSqr += kDir.X * kDir.X;
                        fDelta = kDir.Z * kPmE.Z + kDir.X * kPmE.X +
                          kDir.Y * kPpE.Y;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.Z * kPmE.Z + kPmE.X * kPmE.X +
                          kPpE.Y * kPpE.Y + fDelta * fParam;

                        pfLParam = fParam;
                        kPnt.Z = rkBox.GetHalfSideLengths().Z;
                        kPnt.X = rkBox.GetHalfSideLengths().X;
                        kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                    }
                }
            }
            else
            {
                if (kDir.Z * kPpE.Y >= kDir.Y * kPmE.Z)
                {
                    // v.X < -e.X, v.Y >= -e.Y
                    fLSqr = kDir.Z * kDir.Z + kDir.X * kDir.X;
                    fTmp = fLSqr * kPpE.Y - kDir.Y * (kDir.Z * kPmE.Z +
                                                       kDir.X * kPpE.X);
                    if (fTmp <= 2.0f * fLSqr * rkBox.GetHalfSideLengths().Y)
                    {
                        fT = fTmp / fLSqr;
                        fLSqr += kDir.Y * kDir.Y;
                        fTmp = kPpE.Y - fT;
                        fDelta = kDir.Z * kPmE.Z + kDir.X * kPpE.X +
                          kDir.Y * fTmp;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.Z * kPmE.Z + kPpE.X * kPpE.X +
                          fTmp * fTmp + fDelta * fParam;

                        pfLParam = fParam;
                        kPnt.Z = rkBox.GetHalfSideLengths().Z;
                        kPnt.X = -rkBox.GetHalfSideLengths().X;
                        kPnt.Y = fT - rkBox.GetHalfSideLengths().Y;

                    }
                    else
                    {
                        fLSqr += kDir.Y * kDir.Y;
                        fDelta = kDir.Z * kPmE.Z + kDir.X * kPpE.X +
                          kDir.Y * kPmE.Y;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.Z * kPmE.Z + kPpE.X * kPpE.X +
                          kPmE.Y * kPmE.Y + fDelta * fParam;


                        pfLParam = fParam;
                        kPnt.Z = rkBox.GetHalfSideLengths().Z;
                        kPnt.X = -rkBox.GetHalfSideLengths().X;
                        kPnt.Y = rkBox.GetHalfSideLengths().Y;
                    }
                }
                else
                {
                    // v.X < -e.X, v.Y < -e.Y
                    fLSqr = kDir.Z * kDir.Z + kDir.Y * kDir.Y;
                    fTmp = fLSqr * kPpE.X - kDir.X * (kDir.Z * kPmE.Z +
                                                       kDir.Y * kPpE.Y);
                    if (fTmp >= 0.0f)
                    {
                        // v.X-edge is closest
                        if (fTmp <= 2.0f * fLSqr * rkBox.GetHalfSideLengths().X)
                        {
                            fT = fTmp / fLSqr;
                            fLSqr += kDir.X * kDir.X;
                            fTmp = kPpE.X - fT;
                            fDelta = kDir.Z * kPmE.Z + kDir.X * fTmp +
                              kDir.Y * kPpE.Y;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.Z * kPmE.Z + fTmp * fTmp +
                              kPpE.Y * kPpE.Y + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.Z = rkBox.GetHalfSideLengths().Z;
                            kPnt.X = fT - rkBox.GetHalfSideLengths().X;
                            kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                        }
                        else
                        {
                            fLSqr += kDir.X * kDir.X;
                            fDelta = kDir.Z * kPmE.Z + kDir.X * kPmE.X +
                              kDir.Y * kPpE.Y;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.Z * kPmE.Z + kPmE.X * kPmE.X
                              + kPpE.Y * kPpE.Y + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.Z = rkBox.GetHalfSideLengths().Z;
                            kPnt.X = rkBox.GetHalfSideLengths().X;
                            kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                        }
                        return;
                    }

                    fLSqr = kDir.Z * kDir.Z + kDir.X * kDir.X;
                    fTmp = fLSqr * kPpE.Y - kDir.Y * (kDir.Z * kPmE.Z +
                                                       kDir.X * kPpE.X);
                    if (fTmp >= 0.0f)
                    {
                        // v.Y-edge is closest
                        if (fTmp <= (2.0f * fLSqr * rkBox.GetHalfSideLengths().Y))
                        {
                            fT = fTmp / fLSqr;
                            fLSqr += kDir.Y * kDir.Y;
                            fTmp = kPpE.Y - fT;
                            fDelta = kDir.Z * kPmE.Z + kDir.X * kPpE.X +
                              kDir.Y * fTmp;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.Z * kPmE.Z + kPpE.X * kPpE.X +
                              fTmp * fTmp + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.Z = rkBox.GetHalfSideLengths().Z;
                            kPnt.X = -rkBox.GetHalfSideLengths().X;
                            kPnt.Y = fT - rkBox.GetHalfSideLengths().Y;
                        }
                        else
                        {
                            fLSqr += kDir.Y * kDir.Y;
                            fDelta = kDir.Z * kPmE.Z + kDir.X * kPpE.X +
                              kDir.Y * kPmE.Y;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.Z * kPmE.Z + kPpE.X * kPpE.X +
                              kPmE.Y * kPmE.Y + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.Z = rkBox.GetHalfSideLengths().Z;
                            kPnt.X = -rkBox.GetHalfSideLengths().X;
                            kPnt.Y = rkBox.GetHalfSideLengths().Y;
                        }
                        return;
                    }

                    // (v.X,v.Y)-corner is closest
                    fLSqr += kDir.Y * kDir.Y;
                    fDelta = kDir.Z * kPmE.Z + kDir.X * kPpE.X +
                      kDir.Y * kPpE.Y;
                    fParam = -fDelta / fLSqr;
                    sqrDistance += kPmE.Z * kPmE.Z + kPpE.X * kPpE.X +
                      kPpE.Y * kPpE.Y + fDelta * fParam;


                    pfLParam = fParam;
                    kPnt.Z = rkBox.GetHalfSideLengths().Z;
                    kPnt.X = -rkBox.GetHalfSideLengths().X;
                    kPnt.Y = -rkBox.GetHalfSideLengths().Y;
                }
            }
        }

        private static void FaceC(ref Vector3 kPnt,
              Vector3 kDir, Box rkBox,
              Vector3 kPmE,out float pfLParam, ref float sqrDistance)
        {
            // 1,2,0
            Vector3 kPpE;
            float fLSqr, fInv, fTmp, fParam, fT, fDelta;

            kPpE.Z = kPnt.Z + rkBox.GetHalfSideLengths().Z;
            kPpE.X = kPnt.X + rkBox.GetHalfSideLengths().X;
            if (kDir.Y * kPpE.Z >= kDir.Z * kPmE.Y)
            {
                if (kDir.Y * kPpE.X >= kDir.X * kPmE.Y)
                {
                    // v.Z >= -e.Z, v.X >= -e.X (distance = 0)
                    kPnt.Y = rkBox.GetHalfSideLengths().Y;
                    fInv = 1.0f / kDir.Y;
                    kPnt.Z -= kDir.Z * kPmE.Y * fInv;
                    kPnt.X -= kDir.X * kPmE.Y * fInv;
                    pfLParam = -kPmE.Y * fInv;
                }
                else
                {
                    // v.Z >= -e.Z, v.X < -e.X
                    fLSqr = kDir.Y * kDir.Y + kDir.X * kDir.X;
                    fTmp = fLSqr * kPpE.Z - kDir.Z * (kDir.Y * kPmE.Y +
                                                       kDir.X * kPpE.X);
                    if (fTmp <= 2.0f * fLSqr * rkBox.GetHalfSideLengths().Z)
                    {
                        fT = fTmp / fLSqr;
                        fLSqr += kDir.Z * kDir.Z;
                        fTmp = kPpE.Z - fT;
                        fDelta = kDir.Y * kPmE.Y + kDir.Z * fTmp +
                          kDir.X * kPpE.X;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.Y * kPmE.Y + fTmp * fTmp +
                          kPpE.X * kPpE.X + fDelta * fParam;

                        pfLParam = fParam;
                        kPnt.Y = rkBox.GetHalfSideLengths().Y;
                        kPnt.Z = fT - rkBox.GetHalfSideLengths().Z;
                        kPnt.X = -rkBox.GetHalfSideLengths().X;
                    }
                    else
                    {
                        fLSqr += kDir.Z * kDir.Z;
                        fDelta = kDir.Y * kPmE.Y + kDir.Z * kPmE.Z +
                          kDir.X * kPpE.X;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.Y * kPmE.Y + kPmE.Z * kPmE.Z +
                          kPpE.X * kPpE.X + fDelta * fParam;

                        pfLParam = fParam;
                        kPnt.Y = rkBox.GetHalfSideLengths().Y;
                        kPnt.Z = rkBox.GetHalfSideLengths().Z;
                        kPnt.X = -rkBox.GetHalfSideLengths().X;
                    }
                }
            }
            else
            {
                if (kDir.Y * kPpE.X >= kDir.X * kPmE.Y)
                {
                    // v.Z < -e.Z, v.X >= -e.X
                    fLSqr = kDir.Y * kDir.Y + kDir.Z * kDir.Z;
                    fTmp = fLSqr * kPpE.X - kDir.X * (kDir.Y * kPmE.Y +
                                                       kDir.Z * kPpE.Z);
                    if (fTmp <= 2.0f * fLSqr * rkBox.GetHalfSideLengths().X)
                    {
                        fT = fTmp / fLSqr;
                        fLSqr += kDir.X * kDir.X;
                        fTmp = kPpE.X - fT;
                        fDelta = kDir.Y * kPmE.Y + kDir.Z * kPpE.Z +
                          kDir.X * fTmp;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.Y * kPmE.Y + kPpE.Z * kPpE.Z +
                          fTmp * fTmp + fDelta * fParam;

                        pfLParam = fParam;
                        kPnt.Y = rkBox.GetHalfSideLengths().Y;
                        kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                        kPnt.X = fT - rkBox.GetHalfSideLengths().X;

                    }
                    else
                    {
                        fLSqr += kDir.X * kDir.X;
                        fDelta = kDir.Y * kPmE.Y + kDir.Z * kPpE.Z +
                          kDir.X * kPmE.X;
                        fParam = -fDelta / fLSqr;
                        sqrDistance += kPmE.Y * kPmE.Y + kPpE.Z * kPpE.Z +
                          kPmE.X * kPmE.X + fDelta * fParam;


                        pfLParam = fParam;
                        kPnt.Y = rkBox.GetHalfSideLengths().Y;
                        kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                        kPnt.X = rkBox.GetHalfSideLengths().X;
                    }
                }
                else
                {
                    // v.Z < -e.Z, v.X < -e.X
                    fLSqr = kDir.Y * kDir.Y + kDir.X * kDir.X;
                    fTmp = fLSqr * kPpE.Z - kDir.Z * (kDir.Y * kPmE.Y +
                                                       kDir.X * kPpE.X);
                    if (fTmp >= 0.0f)
                    {
                        // v.Z-edge is closest
                        if (fTmp <= 2.0f * fLSqr * rkBox.GetHalfSideLengths().Z)
                        {
                            fT = fTmp / fLSqr;
                            fLSqr += kDir.Z * kDir.Z;
                            fTmp = kPpE.Z - fT;
                            fDelta = kDir.Y * kPmE.Y + kDir.Z * fTmp +
                              kDir.X * kPpE.X;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.Y * kPmE.Y + fTmp * fTmp +
                              kPpE.X * kPpE.X + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.Y = rkBox.GetHalfSideLengths().Y;
                            kPnt.Z = fT - rkBox.GetHalfSideLengths().Z;
                            kPnt.X = -rkBox.GetHalfSideLengths().X;
                        }
                        else
                        {
                            fLSqr += kDir.Z * kDir.Z;
                            fDelta = kDir.Y * kPmE.Y + kDir.Z * kPmE.Z +
                              kDir.X * kPpE.X;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.Y * kPmE.Y + kPmE.Z * kPmE.Z
                              + kPpE.X * kPpE.X + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.Y = rkBox.GetHalfSideLengths().Y;
                            kPnt.Z = rkBox.GetHalfSideLengths().Z;
                            kPnt.X = -rkBox.GetHalfSideLengths().X;
                        }
                        return;
                    }

                    fLSqr = kDir.Y * kDir.Y + kDir.Z * kDir.Z;
                    fTmp = fLSqr * kPpE.X - kDir.X * (kDir.Y * kPmE.Y +
                                                       kDir.Z * kPpE.Z);
                    if (fTmp >= 0.0f)
                    {
                        // v.X-edge is closest
                        if (fTmp <= (2.0f * fLSqr * rkBox.GetHalfSideLengths().X))
                        {
                            fT = fTmp / fLSqr;
                            fLSqr += kDir.X * kDir.X;
                            fTmp = kPpE.X - fT;
                            fDelta = kDir.Y * kPmE.Y + kDir.Z * kPpE.Z +
                              kDir.X * fTmp;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.Y * kPmE.Y + kPpE.Z * kPpE.Z +
                              fTmp * fTmp + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.Y = rkBox.GetHalfSideLengths().Y;
                            kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                            kPnt.X = fT - rkBox.GetHalfSideLengths().X;
                        }
                        else
                        {
                            fLSqr += kDir.X * kDir.X;
                            fDelta = kDir.Y * kPmE.Y + kDir.Z * kPpE.Z +
                              kDir.X * kPmE.X;
                            fParam = -fDelta / fLSqr;
                            sqrDistance += kPmE.Y * kPmE.Y + kPpE.Z * kPpE.Z +
                              kPmE.X * kPmE.X + fDelta * fParam;

                            pfLParam = fParam;
                            kPnt.Y = rkBox.GetHalfSideLengths().Y;
                            kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                            kPnt.X = rkBox.GetHalfSideLengths().X;
                        }
                        return;
                    }

                    // (v.Z,v.X)-corner is closest
                    fLSqr += kDir.X * kDir.X;
                    fDelta = kDir.Y * kPmE.Y + kDir.Z * kPpE.Z +
                      kDir.X * kPpE.X;
                    fParam = -fDelta / fLSqr;
                    sqrDistance += kPmE.Y * kPmE.Y + kPpE.Z * kPpE.Z +
                      kPpE.X * kPpE.X + fDelta * fParam;


                    pfLParam = fParam;
                    kPnt.Y = rkBox.GetHalfSideLengths().Y;
                    kPnt.Z = -rkBox.GetHalfSideLengths().Z;
                    kPnt.X = -rkBox.GetHalfSideLengths().X;
                }
            }
        }
        #endregion

        #region SegmentSegmentDistanceSq
        /// <summary>
        /// Returns the distance of two segments.
        /// </summary>
        /// <param name="t0">Parametric representation of nearest point on seg0.</param>
        /// <param name="t1">Parametric representation of nearest point on seg0.</param>
        /// <param name="seg0">First segment to test.</param>
        /// <param name="seg1">Second segment to test.</param>
        /// <returns></returns>
        public static float SegmentSegmentDistanceSq(out float t0, out float t1, Segment seg0, Segment seg1)
        {
            Vector3 kDiff = seg0.Origin - seg1.Origin;
            float fA00 = seg0.Delta.LengthSquared();
            float fA01 = -Vector3.Dot(seg0.Delta, seg1.Delta);
            float fA11 = seg1.Delta.LengthSquared();
            float fB0 = Vector3.Dot(kDiff, seg0.Delta);
            float fC = kDiff.LengthSquared();
            float fDet = System.Math.Abs(fA00 * fA11 - fA01 * fA01);
            float fB1, fS, fT, fSqrDist, fTmp;

            if (fDet >= JiggleMath.Epsilon)
            {
                // line segments are not parallel
                fB1 = -Vector3.Dot(kDiff, seg1.Delta);
                fS = fA01 * fB1 - fA11 * fB0;
                fT = fA01 * fB0 - fA00 * fB1;

                if (fS >= 0.0f)
                {
                    if (fS <= fDet)
                    {
                        if (fT >= 0.0f)
                        {
                            if (fT <= fDet)  // region 0 (interior)
                            {
                                // minimum at two interior points of 3D lines
                                float fInvDet = 1.0f / fDet;
                                fS *= fInvDet;
                                fT *= fInvDet;
                                fSqrDist = fS * (fA00 * fS + fA01 * fT + 2.0f * fB0) +
                                  fT * (fA01 * fS + fA11 * fT + 2.0f * fB1) + fC;
                            }
                            else  // region 3 (side)
                            {
                                fT = 1.0f;
                                fTmp = fA01 + fB0;
                                if (fTmp >= 0.0f)
                                {
                                    fS = 0.0f;
                                    fSqrDist = fA11 + (2.0f) * fB1 + fC;
                                }
                                else if (-fTmp >= fA00)
                                {
                                    fS = 1.0f;
                                    fSqrDist = fA00 + fA11 + fC + (2.0f) * (fB1 + fTmp);
                                }
                                else
                                {
                                    fS = -fTmp / fA00;
                                    fSqrDist = fTmp * fS + fA11 + (2.0f) * fB1 + fC;
                                }
                            }
                        }
                        else  // region 7 (side)
                        {
                            fT = 0.0f;
                            if (fB0 >= 0.0f)
                            {
                                fS = 0.0f;
                                fSqrDist = fC;
                            }
                            else if (-fB0 >= fA00)
                            {
                                fS = 1.0f;
                                fSqrDist = fA00 + (2.0f) * fB0 + fC;
                            }
                            else
                            {
                                fS = -fB0 / fA00;
                                fSqrDist = fB0 * fS + fC;
                            }
                        }
                    }
                    else
                    {
                        if (fT >= 0.0f)
                        {
                            if (fT <= fDet)  // region 1 (side)
                            {
                                fS = 1.0f;
                                fTmp = fA01 + fB1;
                                if (fTmp >= 0.0f)
                                {
                                    fT = 0.0f;
                                    fSqrDist = fA00 + (2.0f) * fB0 + fC;
                                }
                                else if (-fTmp >= fA11)
                                {
                                    fT = 1.0f;
                                    fSqrDist = fA00 + fA11 + fC + (2.0f) * (fB0 + fTmp);
                                }
                                else
                                {
                                    fT = -fTmp / fA11;
                                    fSqrDist = fTmp * fT + fA00 + (2.0f) * fB0 + fC;
                                }
                            }
                            else  // region 2 (corner)
                            {
                                fTmp = fA01 + fB0;
                                if (-fTmp <= fA00)
                                {
                                    fT = 1.0f;
                                    if (fTmp >= 0.0f)
                                    {
                                        fS = 0.0f;
                                        fSqrDist = fA11 + (2.0f) * fB1 + fC;
                                    }
                                    else
                                    {
                                        fS = -fTmp / fA00;
                                        fSqrDist = fTmp * fS + fA11 + (2.0f) * fB1 + fC;
                                    }
                                }
                                else
                                {
                                    fS = 1.0f;
                                    fTmp = fA01 + fB1;
                                    if (fTmp >= 0.0f)
                                    {
                                        fT = 0.0f;
                                        fSqrDist = fA00 + (2.0f) * fB0 + fC;
                                    }
                                    else if (-fTmp >= fA11)
                                    {
                                        fT = 1.0f;
                                        fSqrDist = fA00 + fA11 + fC +
                                          (2.0f) * (fB0 + fTmp);
                                    }
                                    else
                                    {
                                        fT = -fTmp / fA11;
                                        fSqrDist = fTmp * fT + fA00 + (2.0f) * fB0 + fC;
                                    }
                                }
                            }
                        }
                        else  // region 8 (corner)
                        {
                            if (-fB0 < fA00)
                            {
                                fT = 0.0f;
                                if (fB0 >= 0.0f)
                                {
                                    fS = 0.0f;
                                    fSqrDist = fC;
                                }
                                else
                                {
                                    fS = -fB0 / fA00;
                                    fSqrDist = fB0 * fS + fC;
                                }
                            }
                            else
                            {
                                fS = 1.0f;
                                fTmp = fA01 + fB1;
                                if (fTmp >= 0.0f)
                                {
                                    fT = 0.0f;
                                    fSqrDist = fA00 + (2.0f) * fB0 + fC;
                                }
                                else if (-fTmp >= fA11)
                                {
                                    fT = 1.0f;
                                    fSqrDist = fA00 + fA11 + fC + (2.0f) * (fB0 + fTmp);
                                }
                                else
                                {
                                    fT = -fTmp / fA11;
                                    fSqrDist = fTmp * fT + fA00 + (2.0f) * fB0 + fC;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (fT >= 0.0f)
                    {
                        if (fT <= fDet)  // region 5 (side)
                        {
                            fS = 0.0f;
                            if (fB1 >= 0.0f)
                            {
                                fT = 0.0f;
                                fSqrDist = fC;
                            }
                            else if (-fB1 >= fA11)
                            {
                                fT = 1.0f;
                                fSqrDist = fA11 + (2.0f) * fB1 + fC;
                            }
                            else
                            {
                                fT = -fB1 / fA11;
                                fSqrDist = fB1 * fT + fC;
                            }
                        }
                        else  // region 4 (corner)
                        {
                            fTmp = fA01 + fB0;
                            if (fTmp < 0.0f)
                            {
                                fT = 1.0f;
                                if (-fTmp >= fA00)
                                {
                                    fS = 1.0f;
                                    fSqrDist = fA00 + fA11 + fC + (2.0f) * (fB1 + fTmp);
                                }
                                else
                                {
                                    fS = -fTmp / fA00;
                                    fSqrDist = fTmp * fS + fA11 + (2.0f) * fB1 + fC;
                                }
                            }
                            else
                            {
                                fS = 0.0f;
                                if (fB1 >= 0.0f)
                                {
                                    fT = 0.0f;
                                    fSqrDist = fC;
                                }
                                else if (-fB1 >= fA11)
                                {
                                    fT = 1.0f;
                                    fSqrDist = fA11 + (2.0f) * fB1 + fC;
                                }
                                else
                                {
                                    fT = -fB1 / fA11;
                                    fSqrDist = fB1 * fT + fC;
                                }
                            }
                        }
                    }
                    else   // region 6 (corner)
                    {
                        if (fB0 < 0.0f)
                        {
                            fT = 0.0f;
                            if (-fB0 >= fA00)
                            {
                                fS = 1.0f;
                                fSqrDist = fA00 + (2.0f) * fB0 + fC;
                            }
                            else
                            {
                                fS = -fB0 / fA00;
                                fSqrDist = fB0 * fS + fC;
                            }
                        }
                        else
                        {
                            fS = 0.0f;
                            if (fB1 >= 0.0f)
                            {
                                fT = 0.0f;
                                fSqrDist = fC;
                            }
                            else if (-fB1 >= fA11)
                            {
                                fT = 1.0f;
                                fSqrDist = fA11 + (2.0f) * fB1 + fC;
                            }
                            else
                            {
                                fT = -fB1 / fA11;
                                fSqrDist = fB1 * fT + fC;
                            }
                        }
                    }
                }
            }
            else
            {
                // line segments are parallel
                if (fA01 > 0.0f)
                {
                    // direction vectors form an obtuse angle
                    if (fB0 >= 0.0f)
                    {
                        fS = 0.0f;
                        fT = 0.0f;
                        fSqrDist = fC;
                    }
                    else if (-fB0 <= fA00)
                    {
                        fS = -fB0 / fA00;
                        fT = 0.0f;
                        fSqrDist = fB0 * fS + fC;
                    }
                    else
                    {
                        fB1 = -Vector3.Dot(kDiff, seg1.Delta);
                        fS = 1.0f;
                        fTmp = fA00 + fB0;
                        if (-fTmp >= fA01)
                        {
                            fT = 1.0f;
                            fSqrDist = fA00 + fA11 + fC + (2.0f) * (fA01 + fB0 + fB1);
                        }
                        else
                        {
                            fT = -fTmp / fA01;
                            fSqrDist = fA00 + (2.0f) * fB0 + fC + fT * (fA11 * fT +
                                                                      (2.0f) * (fA01 + fB1));
                        }
                    }
                }
                else
                {
                    // direction vectors form an acute angle
                    if (-fB0 >= fA00)
                    {
                        fS = 1.0f;
                        fT = 0.0f;
                        fSqrDist = fA00 + (2.0f) * fB0 + fC;
                    }
                    else if (fB0 <= 0.0f)
                    {
                        fS = -fB0 / fA00;
                        fT = 0.0f;
                        fSqrDist = fB0 * fS + fC;
                    }
                    else
                    {
                        fB1 = -Vector3.Dot(kDiff, seg1.Delta);
                        fS = 0.0f;
                        if (fB0 >= -fA01)
                        {
                            fT = 1.0f;
                            fSqrDist = fA11 + (2.0f) * fB1 + fC;
                        }
                        else
                        {
                            fT = -fB0 / fA01;
                            fSqrDist = fC + fT * ((2.0f) * fB1 + fA11 * fT);
                        }
                    }
                }
            }
            t0 = fS;
            t1 = fT;

            return System.Math.Abs(fSqrDist);
        }
        #endregion

    }
}
