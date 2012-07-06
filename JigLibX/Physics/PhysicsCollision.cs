#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using JigLibX.Collision;
#endregion

namespace JigLibX.Physics
{
    #region CollisionIsland
    public class CollisionIsland : List<Body>
    {
        public CollisionIsland()
            : base(64)
        {
        }
        private static CollisionIsland empty = new CollisionIsland();
        public static CollisionIsland Empty { get { return empty; } }

        public bool WantsDeactivation(float dt)
        {
            for (int i = 0; i < this.Count; i++)
                if (this[i].GetShouldBeActive()) return false;
            return true;
        }

        public void Deactivate()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++) this[i].SetInactive();
        }

        public void Activate()
        {
            int count = this.Count;
            for (int i = 0; i < count; i++) this[i].SetActive();
        }
    }
    #endregion

    #region BasicCollisionFunctor

    /// <summary>
    /// Derived from the CollisionFunctor class. The BasicCollisionFunctor can be passed to
    /// CollisionSystem.DetectAllCollision method and gets called for every collision found. 
    /// The collisions get added to a list.
    /// </summary>
    public class BasicCollisionFunctor : CollisionFunctor
    {

        private List<CollisionInfo> colls;

        /// <summary>
        /// Constructor of BasicCollisionFunctor.
        /// </summary>
        /// <param name="colls">This list gets filled with collisionInfo entries.</param>
        public BasicCollisionFunctor(List<CollisionInfo> colls)
        {
            this.colls = colls;
        }

        /// <summary>
        /// CollisionNotify gets called by the CollisionSystem each time a
        /// Collision is detected.
        /// </summary>
        /// <param name="collDetectInfo"></param>
        /// <param name="dirToBody0"></param>
        /// <param name="pointInfos"></param>
        public override unsafe void CollisionNotify(ref CollDetectInfo collDetectInfo, ref Vector3 dirToBody0, SmallCollPointInfo* pointInfos, int numCollPts)
        {
            CollisionInfo info;
            // shortcuts to save typing it over and over
            CollisionSkin skin0 = collDetectInfo.Skin0;
            CollisionSkin skin1 = collDetectInfo.Skin1;

            // if more than one point, add another that is in the middle - collision

            if ((skin0 != null) && (skin0.Owner != null))
            {            
                // if either skin say don't generate contact points, then we don't
                bool generateContactPoints = skin0.OnCollisionEvent(skin0,skin1);
                if (skin1 != null)
                {
                    generateContactPoints &= skin1.OnCollisionEvent(skin1,skin0);
                }

                if (generateContactPoints)
                {
                    info = CollisionInfo.GetCollisionInfo(collDetectInfo, dirToBody0, pointInfos, numCollPts);
                    colls.Add(info);
                    skin0.Collisions.Add(info);

                    if ((skin1 != null) && (skin1.Owner != null))
                        skin1.Collisions.Add(info);
                }
            }
            else if ((skin1 != null) && (skin1.Owner != null))
            {
                // if either skin say don't generate contact points, then we don't
                bool generateContactPoints = skin1.OnCollisionEvent(skin1,skin0);
                if (skin0 != null)
                {
                    generateContactPoints &= skin0.OnCollisionEvent(skin0,skin1);
                }

                if (generateContactPoints)
                {
                    info = CollisionInfo.GetCollisionInfo(collDetectInfo, -dirToBody0, pointInfos, numCollPts);
                    colls.Add(info);
                    skin1.Collisions.Add(info);
                    if ((skin0 != null) && (skin0.Owner != null))
                        skin0.Collisions.Add(info);
                }
            }
            else
                System.Diagnostics.Debug.WriteLine("Collision detected with both skin bodies null.");
        }
    }

    #endregion

    #region FrozenCollisionPredicate

    /// <summary>
    /// Derived from CollisionSkinPredicate2. A SkinPredicate2 which can be passed
    /// to CollisionSystem.DetectCollisions. Only active skin owners get considered.
    /// </summary>
    public class FrozenCollisionPredicate : CollisionSkinPredicate2
    {

        private Body body;


        /// <summary>
        /// Constructor of FrozenCollision Predicate.
        /// </summary>
        /// <param name="body">The body itself doesn't get checked.</param>
        public FrozenCollisionPredicate(Body body)
        {
            this.body = body;
        }

        /// <summary>
        /// Considers two skins and returns true if their bodies aren't frozen.
        /// </summary>
        /// <param name="skin0">The first skin of the pair of skins which should be checked.</param>
        /// <param name="skin1">The second skin of the pair of skins which should be checked.</param>
        /// <returns>Returns true if the skinPair owners are active otherwise false.</returns>
        public override bool ConsiderSkinPair(CollisionSkin skin0, CollisionSkin skin1)
        {
            if ((skin0.Owner != null) && (skin0.Owner != body))
                if (!skin0.Owner.IsActive) return true;

            if ((skin1.Owner != null) && (skin1.Owner != body))
                if (!skin1.Owner.IsActive) return true;

            return false;
        }
    }
    #endregion

}
