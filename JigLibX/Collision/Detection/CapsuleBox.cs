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
    /// DetectFunctor for CapsuleBox collison detection.
    /// </summary>
    public class CollDetectCapsuleBox : DetectFunctor
    {
        private Random random = new Random();

        /// <summary>
        /// 
        /// </summary>
        public CollDetectCapsuleBox()
            : base("CapsuleBox", (int)PrimitiveType.Capsule, (int)PrimitiveType.Box)
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
            // get the skins in the order that we're expectiing
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
            Segment oldSeg = new Segment(oldCapsule.Position, oldCapsule.Length * oldCapsule.Orientation.Backward);
            Segment newSeg = new Segment(newCapsule.Position, newCapsule.Length * newCapsule.Orientation.Backward);

            float radius = oldCapsule.Radius;

            Box oldBox = info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1) as Box;
            Box newBox = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as Box;

            float oldSegT;
            float oldBoxT0, oldBoxT1, oldBoxT2;
            float oldDistSq = Distance.SegmentBoxDistanceSq(out oldSegT, out oldBoxT0, out oldBoxT1, out oldBoxT2,oldSeg, oldBox);
            float newSegT;
            float newBoxT0, newBoxT1, newBoxT2;
            float newDistSq = Distance.SegmentBoxDistanceSq(out newSegT, out newBoxT0, out newBoxT1, out newBoxT2,newSeg, newBox);

            if (MathHelper.Min(oldDistSq, newDistSq) < ((radius + collTolerance) * (radius + collTolerance)))
            {
                Vector3 segPos = oldSeg.GetPoint(oldSegT);
                Vector3 boxPos = oldBox.GetCentre() + oldBoxT0 * oldBox.Orientation.Right +
                    oldBoxT1 * oldBox.Orientation.Up + oldBoxT2 * oldBox.Orientation.Backward;

                float dist = (float)System.Math.Sqrt((float)oldDistSq);
                float depth = radius - dist;

                Vector3 dir;

                if (dist > JiggleMath.Epsilon)
                {
                    dir = segPos - boxPos;
                    JiggleMath.NormalizeSafe(ref dir);
                }
                else if ((segPos - oldBox.GetCentre()).LengthSquared() > JiggleMath.Epsilon)
                {
                    dir = segPos - oldBox.GetCentre();
                    JiggleMath.NormalizeSafe(ref dir);
                }
                else
                {
                    // todo - make this not random
                    dir = Vector3.Transform(Vector3.Backward, Matrix.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(random.Next(360))));
                }

                unsafe
                {
                    SmallCollPointInfo collInfo = new SmallCollPointInfo(boxPos - body0Pos, boxPos - body1Pos, depth);

                    collisionFunctor.CollisionNotify(ref info, ref dir, &collInfo, 1);
                }

            }

        }
    }
}
