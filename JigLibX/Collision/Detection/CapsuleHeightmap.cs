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
    public class CollDetectCapsuleHeightmap : DetectFunctor
    {

        /// <summary>
        /// DetectFunctor for CapsuleHeightmap collison detection.
        /// </summary>
        public CollDetectCapsuleHeightmap()
            : base("CapsuleHeightmap", (int)PrimitiveType.Capsule, (int)PrimitiveType.Heightmap)
        {
        }

        /// <summary>
        /// 
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
            Capsule oldCapsule = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Capsule;
            Capsule newCapsule = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Capsule;

            Heightmap oldHeightmap = info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1) as Heightmap;
            Heightmap newHeightmap = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as Heightmap;

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
                    Vector3 averageNormal = Vector3.Zero;

                    // the start
                    {
                        float oldDist, newDist;
                        Vector3 normal;
                        oldHeightmap.GetHeightAndNormal(out oldDist, out normal, oldCapsule.Position);
                        newHeightmap.GetHeightAndNormal(out newDist, out normal, newCapsule.Position);

                        if (MathHelper.Min(newDist, oldDist) < collTolerance + newCapsule.Radius)
                        {
                            float oldDepth = oldCapsule.Radius - oldDist;
                            // calc the world position based on the old position(s)
                            Vector3 worldPos = oldCapsule.Position - oldCapsule.Radius * normal;
                            if (numCollPts < MaxLocalStackSCPI)
                            {
                                collPts[numCollPts++] = new SmallCollPointInfo(worldPos - body0Pos, worldPos - body1Pos, oldDepth);
                            }
                            averageNormal += normal;
                        }
                    }
                    // the end
                    {
                        Vector3 oldEnd = oldCapsule.GetEnd();
                        Vector3 newEnd = newCapsule.GetEnd();
                        float oldDist, newDist;
                        Vector3 normal;
                        oldHeightmap.GetHeightAndNormal(out oldDist, out normal, oldEnd);
                        newHeightmap.GetHeightAndNormal(out newDist, out normal, newEnd);
                        if (MathHelper.Min(newDist, oldDist) < collTolerance + newCapsule.Radius)
                        {
                            float oldDepth = oldCapsule.Radius - oldDist;
                            // calc the world position based on the old position(s)
                            Vector3 worldPos = oldEnd - oldCapsule.Radius * normal;
                            if (numCollPts < MaxLocalStackSCPI)
                            {
                                collPts[numCollPts++] = new SmallCollPointInfo(worldPos - body0Pos, worldPos - body1Pos, oldDepth);
                            }
                            averageNormal += normal;
                        }
                    }

                    if (numCollPts > 0)
                    {
                        JiggleMath.NormalizeSafe(ref averageNormal);
                        collisionFunctor.CollisionNotify(ref info, ref averageNormal, collPts, numCollPts);
                    }
                }
#if !USE_STACKALLOC
                FreeStackAlloc(collPtArray);
#endif
            }
        }
    }
            
}
