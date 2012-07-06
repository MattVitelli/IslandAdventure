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
    /// DetectFunctor for SphereHeightmap collison detection.
    /// </summary>
    public class CollDetectSphereHeightmap : DetectFunctor
    {

        /// <summary>
        /// 
        /// </summary>
        public CollDetectSphereHeightmap()
            : base("SphereHeightmap", (int)PrimitiveType.Sphere, (int)PrimitiveType.Heightmap)
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
            Sphere oldSphere = info.Skin0.GetPrimitiveOldWorld(info.IndexPrim0) as Sphere;
            Sphere newSphere = info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0) as Sphere;

            Heightmap oldHeightmap = info.Skin1.GetPrimitiveOldWorld(info.IndexPrim1) as Heightmap;
            Heightmap newHeightmap = info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1) as Heightmap;

            float newDist;
            Vector3 normal;
            newHeightmap.GetHeightAndNormal(out newDist, out normal,newSphere.Position);
            if (newDist < collTolerance + newSphere.Radius)
            {
                float oldDist = oldHeightmap.GetHeight(oldSphere.Position);
                float depth = oldSphere.Radius - oldDist;

                // calc the world position when it just hit
                Vector3 oldPt = oldSphere.Position - oldSphere.Radius * normal;
                unsafe
                {
                    SmallCollPointInfo ptInfo = new SmallCollPointInfo(oldPt - body0Pos, oldPt - body1Pos, depth);

                    collisionFunctor.CollisionNotify(ref info, ref normal, &ptInfo, 1);
                }
            }
        }
    }
}
