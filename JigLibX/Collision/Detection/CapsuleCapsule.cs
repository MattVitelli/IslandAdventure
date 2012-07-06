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
    /// DetectFunctor for CapsuleCapsule collison detection.
    /// </summary>
    public class CollDetectCapsuleCapsule : DetectFunctor
    {
        private Random random = new Random();

        public CollDetectCapsuleCapsule()
            : base("CapsuleCapsule", (int)PrimitiveType.Capsule, (int)PrimitiveType.Capsule)
        {
        }

        public override void CollDetect(CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            Vector3 body0Pos = (info.Skin0.Owner != null) ? info.Skin0.Owner.OldPosition : Vector3.Zero;
            Vector3 body1Pos = (info.Skin1.Owner != null) ? info.Skin1.Owner.OldPosition : Vector3.Zero;

            // todo - proper swept test
            Capsule oldCapsule0 = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Capsule;
            Capsule newCapsule0 = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Capsule;
            Segment oldSeg0 = new Segment(oldCapsule0.Position, oldCapsule0.Length * oldCapsule0.Orientation.Backward);
            Segment newSeg0 = new Segment(newCapsule0.Position, newCapsule0.Length * newCapsule0.Orientation.Backward);

            Capsule oldCapsule1 = info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1) as Capsule;
            Capsule newCapsule1 = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as Capsule;
            Segment oldSeg1 = new Segment(oldCapsule1.Position, oldCapsule1.Length * oldCapsule1.Orientation.Backward);
            Segment newSeg1 = new Segment(newCapsule1.Position, newCapsule1.Length * newCapsule1.Orientation.Backward);

            float radSum = newCapsule0.Radius + newCapsule1.Radius;

            float oldt0, oldt1;
            float newt0, newt1;
            float oldDistSq = Distance.SegmentSegmentDistanceSq(out oldt0, out oldt1, oldSeg0, oldSeg1);
            float newDistSq = Distance.SegmentSegmentDistanceSq(out newt0, out newt1, newSeg0, newSeg1);

            if (System.Math.Min(oldDistSq, newDistSq) < ((radSum + collTolerance) * (radSum + collTolerance)))
            {
                Vector3 pos0 = oldSeg0.GetPoint(oldt0);
                Vector3 pos1 = oldSeg1.GetPoint(oldt1);

                Vector3 delta = pos0 - pos1;

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

                Vector3 worldPos = pos1 +
                    (oldCapsule1.Radius - 0.5f * depth) * delta;

                unsafe
                {
                    SmallCollPointInfo collInfo = new SmallCollPointInfo(worldPos - body0Pos, worldPos - body1Pos, depth);
                    collisionFunctor.CollisionNotify(ref info, ref delta, &collInfo, 1);
                }

            }


        }
    }
}
