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
    /// DetectFunctor for SphereBox collison detection.
    /// </summary>
    public class CollDetectSphereBox : DetectFunctor
    {

        /// <summary>
        /// 
        /// </summary>
        #region public CollDetectSphereBox() 
        public CollDetectSphereBox() 
            : base("SphereBox", (int)PrimitiveType.Sphere, (int)PrimitiveType.Box)
        {
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="infoOrig"></param>
        /// <param name="collTolerance"></param>
        /// <param name="collisionFunctor"></param>
        #region public override void CollDetect(CollDetectInfo infoOrig, float collTolerance, CollisionFunctor collisionFunctor)
        public override void CollDetect(CollDetectInfo infoOrig, float collTolerance, CollisionFunctor collisionFunctor)
        {
           // get the skins in the order that we're expecting
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

            // todo - proper sweep test
            Sphere oldSphere = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Sphere;
            Sphere newSphere = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Sphere;

            Box oldBox = info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1) as Box;
            Box newBox = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as Box;

            Vector3 oldBoxPoint;
            Vector3 newBoxPoint;

            float oldDist = oldBox.GetDistanceToPoint(out oldBoxPoint, oldSphere.Position);
            float newDist = newBox.GetDistanceToPoint(out newBoxPoint, newSphere.Position);

            // normally point will be outside
            float oldDepth = oldSphere.Radius - oldDist;
            float newDepth = newSphere.Radius - newDist;

            if (System.Math.Max(oldDepth, newDepth) > -collTolerance)
            {
                Vector3 dir;
                if (oldDist < -JiggleMath.Epsilon)
                {
                    dir = oldBoxPoint - oldSphere.Position - oldBoxPoint;
                    JiggleMath.NormalizeSafe(ref dir);
                }
                else if (oldDist > JiggleMath.Epsilon)
                {
                    dir = oldSphere.Position - oldBoxPoint;
                    JiggleMath.NormalizeSafe(ref dir);
                }
                else
                {
                    dir = oldSphere.Position - oldBox.GetCentre();
                    JiggleMath.NormalizeSafe(ref dir);
                }

                unsafe
                {
                    SmallCollPointInfo collInfo = new SmallCollPointInfo(oldBoxPoint - body0Pos,
                        oldBoxPoint - body1Pos, oldDepth);

                    
                    collisionFunctor.CollisionNotify(ref info, ref dir, &collInfo, 1);
                }

            }
        #endregion

        }
    }
}
