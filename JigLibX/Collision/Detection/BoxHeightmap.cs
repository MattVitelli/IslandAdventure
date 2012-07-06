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
    /// DetectFunctor for BoxHeightmap collison detection.
    /// </summary>
    public class CollDetectBoxHeightmap : DetectFunctor
    {
        /// <summary>
        /// Constructor of BoxHeightmap Collision DetectFunctor.
        /// </summary>
        public CollDetectBoxHeightmap()
            : base("BoxHeighmap", (int)PrimitiveType.Box, (int)PrimitiveType.Heightmap)
        {
        }

        /// <summary>
        /// Detect BoxHeightmap Collisions.
        /// </summary>
        /// <param name="infoOrig"></param>
        /// <param name="collTolerance"></param>
        /// <param name="collisionFunctor"></param>
        public override void CollDetect(CollDetectInfo infoOrig, float collTolerance, CollisionFunctor collisionFunctor)
        {
            CollDetectInfo info = infoOrig;
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

            // todo - proper swept test
            Box oldBox = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Box;
            Box newBox = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Box;

            Heightmap oldHeightmap = info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1) as Heightmap;
            Heightmap newHeightmap = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as Heightmap;

            Vector3[] oldPts, newPts;
            oldBox.GetCornerPoints(out oldPts);
            newBox.GetCornerPoints(out newPts);

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

                    Vector3 collNormal = Vector3.Zero;

                    for (int i = 0; i < 8; ++i)
                    {
                        Vector3 newPt = newPts[i];
                        float newDist;
                        Vector3 normal;
                        newHeightmap.GetHeightAndNormal(out newDist, out normal, newPt);

                        if (newDist < collTolerance)
                        {
                            Vector3 oldPt = oldPts[i];
                            float oldDist = oldHeightmap.GetHeight(oldPt);

                            #region REFERENCE: collPts.Add(new CollPointInfo(oldPt - body0Pos, oldPt - body1Pos, -oldDist));
                            Vector3 pt0;
                            Vector3 pt1;
                            Vector3.Subtract(ref oldPt, ref body0Pos, out pt0);
                            Vector3.Subtract(ref oldPt, ref body1Pos, out pt1);
                            if (numCollPts < MaxLocalStackSCPI)
                            {
                                collPts[numCollPts++] = new SmallCollPointInfo(ref pt0, ref pt1, -oldDist);
                            }
                            #endregion

                            #region REFERENCE: collNormal += normal;
                            Vector3.Add(ref collNormal, ref normal, out collNormal);
                            #endregion
                        }
                    }

                    if (numCollPts > 0)
                    {
                        JiggleMath.NormalizeSafe(ref collNormal);
                        collisionFunctor.CollisionNotify(ref info, ref collNormal, collPts, numCollPts);
                    }
                }
#if !USE_STACKALLOC
                FreeStackAlloc(collPtArray);
#endif
            }
        }

    }
}
