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
    /// DetectFunctor for SpherePlane  collison detection.
    /// </summary>
    public class CollDetectSpherePlane : DetectFunctor
    {

        /// <summary>
        /// 
        /// </summary>
        public CollDetectSpherePlane()
            : base("SpherePlane", (int)PrimitiveType.Sphere, (int)PrimitiveType.Plane)
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

            JigLibX.Geometry.Plane oldPlane = info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1) as JigLibX.Geometry.Plane;
            JigLibX.Geometry.Plane newPlane = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as JigLibX.Geometry.Plane;

            Matrix newPlaneInvTransform = newPlane.InverseTransformMatrix;
            Matrix oldPlaneInvTransform = oldPlane.InverseTransformMatrix;

            Vector3 oldSpherePos = Vector3.Transform(oldSphere.Position, oldPlaneInvTransform);
            Vector3 newSpherePos = Vector3.Transform(newSphere.Position, newPlaneInvTransform);

            // consider it a contact if either old or new are touching
            float oldDist = Distance.PointPlaneDistance(oldSpherePos, oldPlane);
            float newDist = Distance.PointPlaneDistance(newSpherePos, newPlane);

            if (System.Math.Min(newDist, oldDist) > collTolerance + newSphere.Radius)
                return;

            // collision - record depth using the old values
            float oldDepth = oldSphere.Radius - oldDist;

            // calc the world position based on the old position(s)
            Vector3 worldPos = oldSphere.Position - oldSphere.Radius * oldPlane.Normal;

            unsafe
            {
                SmallCollPointInfo collInfo = new SmallCollPointInfo(worldPos - body0Pos, worldPos - body1Pos, oldDepth);
                collisionFunctor.CollisionNotify(ref info, ref oldPlane.normal, &collInfo, 1);
            }

        }
    }
}
