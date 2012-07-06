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
    /// DetectFunctor for SphereSphere collison detection.
    /// </summary>
    public class CollDetectSphereSphere : DetectFunctor
    {

        private Random random = new Random();

        /// <summary>
        /// 
        /// </summary>
        public CollDetectSphereSphere()
            : base("SphereSphere", (int)PrimitiveType.Sphere, (int)PrimitiveType.Sphere)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="collTolerance"></param>
        /// <param name="collisionFunctor"></param>
        public override void CollDetect(CollDetectInfo info, float collTolerance, CollisionFunctor collisionFunctor)
        {
            Vector3 body0Pos = (info.Skin0.Owner != null) ? info.Skin0.Owner.OldPosition : Vector3.Zero;
            Vector3 body1Pos = (info.Skin1.Owner != null) ? info.Skin1.Owner.OldPosition : Vector3.Zero;
            
            // todo - proper swept test
            Sphere oldSphere0 = (Sphere) info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0);
            Sphere newSphere0 = (Sphere)info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0);
            Sphere oldSphere1 = (Sphere)info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1);
            Sphere newSphere1 = (Sphere)info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1);

            Vector3 oldDelta = oldSphere0.Position - oldSphere1.Position;
            Vector3 newDelta = newSphere0.Position - oldSphere1.Position;

            float oldDistSq = oldDelta.LengthSquared();
            float newDistSq = newDelta.LengthSquared();

            float radSum = newSphere0.Radius + newSphere1.Radius;

            if (System.Math.Min(oldDistSq, newDistSq) < ((radSum + collTolerance) * (radSum + collTolerance)))
            {
                float oldDist = (float)System.Math.Sqrt((float)oldDistSq);
                float depth = radSum - oldDist;

                if (oldDist > JiggleMath.Epsilon)
                {
                    oldDelta /= oldDist;
                }
                else
                {
                    // TODO - make this not random...!
                    oldDelta = Vector3.Transform(Vector3.Backward, Matrix.CreateFromAxisAngle(Vector3.Up,MathHelper.ToRadians(random.Next(360))));
                }

                Vector3 worldPos = oldSphere1.Position +
                    (oldSphere1.Radius - 0.5f * depth) * oldDelta;

                unsafe
                {
                    SmallCollPointInfo collInfo = new SmallCollPointInfo(worldPos - body0Pos, worldPos - body1Pos, depth);

                    collisionFunctor.CollisionNotify(ref info, ref oldDelta, &collInfo, 1);
                }
            }

        }
    }
}
