#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using JigLibX.Physics;
using JigLibX.Collision;
using JigLibX.Geometry;
using Microsoft.Xna.Framework;
using System.Collections.ObjectModel;
#endregion

namespace JigLibX.Collision
{

    /// <summary>
    /// CollisionSystem which checks every skin against each other. For small scenes this is
    /// as fast or even faster as CollisionSystemGrid.
    /// </summary>
    public class CollisionSystemBrute : CollisionSystem
    {

        private List<CollisionSkin> skins = new List<CollisionSkin>();

        /// <summary>
        /// Initializes a new CollisionSystem which checks for collision
        /// by checking each skin against each other.
        /// </summary>
        public CollisionSystemBrute()
        {
        }

        private static bool CheckCollidables(CollisionSkin skin0,
            CollisionSkin skin1)
        {
            List<CollisionSkin> nonColl0 = skin0.NonCollidables;
            List<CollisionSkin> nonColl1 = skin1.NonCollidables;

            //most common case
            if (nonColl0.Count == 0 && nonColl1.Count == 0)
                return true;

            for (int i0 = nonColl0.Count; i0-- != 0; )
            {
                if (nonColl0[i0] == skin1) 
                    return false;
            }

            for (int i1 = nonColl1.Count; i1-- != 0; )
            {
                if (nonColl1[i1] == skin0)
                    return false;
            }

            return true;
        }

        public override ReadOnlyCollection<CollisionSkin> CollisionSkins
        {
            get { return skins.AsReadOnly(); }
        }

        public override void AddCollisionSkin(CollisionSkin skin)
        {
            if (skins.Contains(skin))
                System.Diagnostics.Debug.WriteLine("Warning: tried to add skin to CollisionSystemBrute but it's already registered");
            else
                skins.Add(skin);

            skin.CollisionSystem = this;
        }

        public override bool RemoveCollisionSkin(CollisionSkin skin)
        {
            if (!skins.Contains(skin)) return false;
            skins.Remove(skin);
            return true;
        }

        public override void CollisionSkinMoved(CollisionSkin skin)
        {
            // not needed
        }

        public override void DetectCollisions(Body body, CollisionFunctor collisionFunctor, CollisionSkinPredicate2 collisionPredicate, float collTolerance)
        {
            if (!body.IsActive)
                return;

            CollDetectInfo info = new CollDetectInfo();
            info.Skin0 = body.CollisionSkin; //?!
            if (info.Skin0 == null) return;

            int bodyPrimitves = info.Skin0.NumPrimitives;
            int numSkins = skins.Count;

            for (int skin = 0; skin < numSkins; ++skin)
            {
                info.Skin1 = skins[skin];
                if ((info.Skin0 != info.Skin1) && CheckCollidables(info.Skin0, info.Skin1))
                {
                    int primitives = info.Skin1.NumPrimitives;

                    for (info.IndexPrim0 = 0; info.IndexPrim0 < bodyPrimitves; ++info.IndexPrim0)
                    {
                        for (info.IndexPrim1 = 0; info.IndexPrim1 < primitives; ++info.IndexPrim1)
                        {
                            DetectFunctor f = GetCollDetectFunctor(info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0).Type,
                                info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1).Type);

                            if (f != null) f.CollDetect(info, collTolerance, collisionFunctor);
                        }
                    }
                }
            }

        }

        public override void DetectAllCollisions(List<Body> bodies, CollisionFunctor collisionFunctor, CollisionSkinPredicate2 collisionPredicate, float collTolerance)
        {
            int numSkins = skins.Count;
            int numBodies = bodies.Count;

            CollDetectInfo info = new CollDetectInfo();

            for (int ibody = 0; ibody < numBodies; ++ibody)
            {
                Body body = bodies[ibody];
                if(!body.IsActive)
                    continue;

                info.Skin0 = body.CollisionSkin;
                if (info.Skin0 == null)
                    continue;

                for (int skin = 0; skin < numSkins; ++skin)
                {
                    info.Skin1 = skins[skin];
                    if (info.Skin0 == info.Skin1)
                        continue;

                    // CHANGE
                    if (info.Skin1 == null)
                        continue;      
                    
                    bool skinSleeping = true;

                    if (info.Skin1.Owner != null && info.Skin1.Owner.IsActive)
                        skinSleeping = false;

                    if ((skinSleeping == false) && (info.Skin1.ID < info.Skin0.ID))
                        continue;

                    if((collisionPredicate != null) &&
                        collisionPredicate.ConsiderSkinPair(info.Skin0,info.Skin1) == false)
                    continue;

                    // basic bbox test
                    if(BoundingBoxHelper.OverlapTest(ref info.Skin0.WorldBoundingBox,
                        ref info.Skin1.WorldBoundingBox,collTolerance))
                    {
                        if (CheckCollidables(info.Skin0,info.Skin1))
                        {
                            int bodyPrimitives = info.Skin0.NumPrimitives;
                            int primitves = info.Skin1.NumPrimitives;

                            for(info.IndexPrim0 = 0; info.IndexPrim0 < bodyPrimitives; ++info.IndexPrim0)
                            {
                                for (info.IndexPrim1 = 0; info.IndexPrim1 < primitves; ++info.IndexPrim1)
                                {
                                    DetectFunctor f = GetCollDetectFunctor(info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0).Type,
                                        info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1).Type);
                                    if (f != null)
                                        f.CollDetect(info, collTolerance, collisionFunctor);
                                }
                            }
                        } 
                    } // overlapt test
                } // loop over skins
            } // loop over bodies

        } // void

        public override bool SegmentIntersect(out float fracOut,out CollisionSkin skinOut,out Vector3 posOut,out Vector3 normalOut, Segment seg, CollisionSkinPredicate1 collisionPredicate)
        {
            int numSkins = skins.Count;
            BoundingBox segBox = BoundingBoxHelper.InitialBox;
            BoundingBoxHelper.AddSegment(seg, ref segBox);

            //initialise the outputs
            fracOut = float.MaxValue;
            skinOut = null;
            posOut = normalOut = Vector3.Zero;

            // working vars
            float frac;
            Vector3 pos;
            Vector3 normal;

            for (int iskin = 0; iskin < numSkins; ++iskin)
            {
                CollisionSkin skin = skins[iskin];
                if ((collisionPredicate == null) ||
                    collisionPredicate.ConsiderSkin(skin))
                {
                    // basic bbox test
                    if (BoundingBoxHelper.OverlapTest(ref skin.WorldBoundingBox, ref segBox))
                    {
                        if (skin.SegmentIntersect(out frac, out pos, out normal, seg))
                        {
                            if (frac < fracOut)
                            {
                                posOut = pos;
                                normalOut = normal;
                                skinOut = skin;
                                fracOut = frac;
                            }
                        }

                    }
                }
            }

            if (fracOut > 1.0f) return false;
            fracOut = MathHelper.Clamp(fracOut, 0.0f, 1.0f);
            return true;
        }

    }
}
