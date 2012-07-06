#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Geometry;
using JigLibX.Math;
using JPlane = JigLibX.Geometry.Plane;
#endregion

namespace JigLibX.Collision
{

    /// <summary>
    /// DetectFunctor for BoxPlane collision detection.
    /// </summary>
    public class CollDetectBoxPlane : DetectFunctor
    {
        /// <summary>
        /// Constructor of BoxPlane Collision DetectFunctor.
        /// </summary>
        public CollDetectBoxPlane()
            : base("BoxPlane", (int)PrimitiveType.Box, (int)PrimitiveType.Plane)
        {
        }
        Vector3[] oldTransPts = new Vector3[8];

        /// <summary>
        /// Detect BoxPlane Collisions.
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

            Vector3 body0Pos = (info.Skin0.Owner != null) ? info.Skin0.Owner.OldPosition : Vector3.Zero;
            Vector3 body1Pos = (info.Skin1.Owner != null) ? info.Skin1.Owner.OldPosition : Vector3.Zero;

            Box oldBox = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Box;
            Box newBox = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Box;

            JPlane oldPlane = info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1) as JPlane;
            JPlane newPlane = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as JPlane;

            Matrix newPlaneInvTransform = newPlane.InverseTransformMatrix;
            Vector3 newBoxCen = Vector3.Transform(newBox.GetCentre(), newPlaneInvTransform);

            // quick check
            float centreDist = Distance.PointPlaneDistance(newBoxCen, newPlane);
            if (centreDist > collTolerance + newBox.GetBoundingRadiusAroundCentre())
                return;

            Matrix oldPlaneInvTransform = oldPlane.InverseTransformMatrix;

            Vector3[] newPts;
            newBox.GetCornerPoints(out newPts);
            Vector3[] oldPts;
            oldBox.GetCornerPoints(out oldPts);

            unsafe
            {
#if USE_STACKALLOC
                SmallCollPointInfo* collPts = stackalloc SmallCollPointInfo[MaxLocalStackSCPI];
#else
                SmallCollPointInfo[] collPtArray = SCPIStackAlloc();
                fixed (SmallCollPointInfo* collPts = collPtArray)
#endif
                {
                    int numCollPts = 0;

                    for (int i = 0; i < 8; ++i)
                    {
                        Vector3.Transform(ref oldPts[i], ref oldPlaneInvTransform, out oldTransPts[i]);
                        Vector3.Transform(ref newPts[i], ref newPlaneInvTransform, out newPts[i]);

                        float oldDepth = -Distance.PointPlaneDistance(ref oldTransPts[i], oldPlane);
                        float newDepth = -Distance.PointPlaneDistance(ref newPts[i], newPlane);

                        if (MathHelper.Max(oldDepth, newDepth) > -collTolerance)
                        {
                            if (numCollPts < MaxLocalStackSCPI)
                            {
                                collPts[numCollPts++] = new SmallCollPointInfo(oldPts[i] - body0Pos, oldPts[i] - body1Pos, oldDepth);
                            }
                        }
                    }

                    if (numCollPts > 0)
                    {
                        collisionFunctor.CollisionNotify(ref info, ref oldPlane.normal, collPts, numCollPts);
                    }
                }
#if !USE_STACKALLOC
                FreeStackAlloc(collPtArray);
#endif
            }

        }

    }
}

