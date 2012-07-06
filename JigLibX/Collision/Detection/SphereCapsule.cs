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
    /// DetectFunctor for SphereCapsule collison detection.
    /// </summary>
    public class CollDetectSphereCapsule : DetectFunctor
    {
        private Random random = new Random();


        /// <summary>
        /// 
        /// </summary>
        public CollDetectSphereCapsule()
            : base("SphereCapsule", (int)PrimitiveType.Sphere, (int)PrimitiveType.Capsule)
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

            Vector3 body0Pos = (info.Skin0.Owner != null) ? info.Skin0.Owner.OldPosition : Vector3.Zero;
            Vector3 body1Pos = (info.Skin1.Owner != null) ? info.Skin1.Owner.OldPosition : Vector3.Zero;

            // todo - proper swept test
            Sphere oldSphere = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Sphere;
            Sphere newSphere = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Sphere;

            Capsule oldCapsule = info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1) as Capsule;
            Capsule newCapsule = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as Capsule;

            Segment oldSeg = new Segment(oldCapsule.Position, oldCapsule.Length * oldCapsule.Orientation.Backward);
            Segment newSeg = new Segment(oldCapsule.Position, newCapsule.Length * newCapsule.Orientation.Backward);

            float radSum = newCapsule.Radius + newSphere.Radius;

            float oldt, newt;
            float oldDistSq = Distance.PointSegmentDistanceSq(out oldt, oldSphere.Position, oldSeg);
            float newDistSq = Distance.PointSegmentDistanceSq(out newt, newSphere.Position, newSeg);

            if (MathHelper.Min(oldDistSq, newDistSq) < (radSum + collTolerance) * (radSum + collTolerance))
            {
                Vector3 segPos = oldSeg.GetPoint(oldt);
                Vector3 delta = oldSphere.Position - segPos;

                float dist = (float)System.Math.Sqrt((float)oldDistSq);
                float depth = radSum - dist;

                if (dist > JiggleMath.Epsilon)
                {
                    delta /= dist;
                }
                else
                {
                    // todo - make this not random
                    delta = Vector3.Transform(Vector3.Backward, Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(random.Next(360))));
                }

                Vector3 worldPos = segPos +
                    ((oldCapsule.Radius - 0.5f * depth) * delta);
                unsafe
                {
                    SmallCollPointInfo collInfo = new SmallCollPointInfo(worldPos - body0Pos,
                        worldPos - body1Pos, depth);

                    collisionFunctor.CollisionNotify(ref info, ref delta, &collInfo, 1);
                }
            }

        }

    }
}
