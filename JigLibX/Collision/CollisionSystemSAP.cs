//Originally by Jon Watte.
//Released into the JigLibX project under the JigLibX license.
//Separately released into the public domain by the author.

#region Using Statements
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework;
using JigLibX.Geometry;
#endregion

namespace JigLibX.Collision
{

    /// <summary>
    /// Implementing a collision system (broad-phase test) based on the sweep-and-prune 
    /// algorithm
    /// </summary>
    public class CollisionSystemSAP : CollisionSystem, IComparer<CollisionSkin>
    {
        List<CollisionSkin> skins_ = new List<CollisionSkin>();
        bool dirty_;
        float largestX_;
        List<CollisionSkin> active_ = new List<CollisionSkin>();
        List<Primitive> testing_ = new List<Primitive>();
        List<Primitive> second_ = new List<Primitive>();

        public float LargestX { get { return largestX_; } }

        public CollisionSystemSAP()
        {
        }

        public override void AddCollisionSkin(CollisionSkin collisionSkin)
        {
            collisionSkin.CollisionSystem = this;
            skins_.Add(collisionSkin);
            dirty_ = true;
            float dx = collisionSkin.WorldBoundingBox.Max.X - collisionSkin.WorldBoundingBox.Min.X;
            if (dx > largestX_)
                largestX_ = dx;
        }

        public override bool RemoveCollisionSkin(CollisionSkin collisionSkin)
        {
            int ix = skins_.IndexOf(collisionSkin);
            if (ix >= skins_.Count || ix < 0)
                return false;
            skins_.RemoveAt(ix);
            return true;
        }

        public override ReadOnlyCollection<CollisionSkin> CollisionSkins
        {
            get { return skins_.AsReadOnly(); }
        }

        public override void CollisionSkinMoved(CollisionSkin skin)
        {
            dirty_ = true;
        }

        void Extract(Vector3 min, Vector3 max, List<CollisionSkin> skins)
        {
            if (skins_.Count == 0)
                return;
            MaybeSort();
            int i = bsearch(min.X - largestX_);
            float xMax = max.X;
            while (i < skins_.Count && skins_[i].WorldBoundingBox.Min.X < xMax)
            {
                if (skins_[i].WorldBoundingBox.Max.X > min.X)
                    skins.Add(skins_[i]);
                ++i;
            }
        }

        int bsearch(float x)
        {
            //  It is up to the caller to make sure this isn't called on an empty collection.
            int top = skins_.Count;
            int bot = 0;
            while (top > bot)
            {
                int mid = (top + bot) >> 1;
                if (skins_[mid].WorldBoundingBox.Min.X >= x)
                {
                    System.Diagnostics.Debug.Assert(top > mid);
                    top = mid;
                }
                else
                {
                    System.Diagnostics.Debug.Assert(bot <= mid);
                    bot = mid + 1;
                }
            }
            System.Diagnostics.Debug.Assert(top >= 0 && top <= skins_.Count);
            System.Diagnostics.Debug.Assert(top == 0 || skins_[top - 1].WorldBoundingBox.Min.X < x);
            System.Diagnostics.Debug.Assert(top == skins_.Count || skins_[top].WorldBoundingBox.Min.X >= x);
            return top;
        }

        public override void DetectCollisions(JigLibX.Physics.Body body, CollisionFunctor collisionFunctor, CollisionSkinPredicate2 collisionPredicate, float collTolerance)
        {
            if (!body.IsActive)
                return;

            CollDetectInfo info = new CollDetectInfo();
            info.Skin0 = body.CollisionSkin;
            if (info.Skin0 == null)
                return;

            active_.Clear();
            testing_.Clear();
            Extract(info.Skin0.WorldBoundingBox.Min, info.Skin0.WorldBoundingBox.Max, active_);

            for (int j = 0, m = info.Skin0.NumPrimitives; j != m; ++j)
                testing_.Add(info.Skin0.GetPrimitiveNewWorld(j));

            int nBodyPrims = testing_.Count;

            for (int i = 0, n = active_.Count; i != n; ++i)
            {
                info.Skin1 = active_[i];
                if (info.Skin0 != info.Skin1 && (collisionPredicate == null ||
                    collisionPredicate.ConsiderSkinPair(info.Skin0, info.Skin1)))
                {
                    int nPrim1 = info.Skin1.NumPrimitives;
                    second_.Clear();
                    for (int k = 0; k != nPrim1; ++k)
                        second_.Add(info.Skin1.GetPrimitiveNewWorld(k));
                    for (info.IndexPrim0 = 0; info.IndexPrim0 != nBodyPrims; ++info.IndexPrim0)
                    {
                        for (info.IndexPrim1 = 0; info.IndexPrim1 != nPrim1; ++info.IndexPrim1)
                        {
                            DetectFunctor f =
                              GetCollDetectFunctor(info.Skin0.GetPrimitiveNewWorld(info.IndexPrim0).Type,
                                info.Skin1.GetPrimitiveNewWorld(info.IndexPrim1).Type);
                            if (f != null)
                                f.CollDetect(info, collTolerance, collisionFunctor);
                        }
                    }
                }
            }
        }

        SkinTester skinTester_ = new SkinTester();

        public override void DetectAllCollisions(List<JigLibX.Physics.Body> bodies, CollisionFunctor collisionFunctor, CollisionSkinPredicate2 collisionPredicate, float collTolerance)
        {
            skinTester_.Set(this, collisionFunctor, collisionPredicate, collTolerance);
            MaybeSort();
            //  I know that each skin for the bodies is already in my list of skins.
            //  Thus, I can do collision between all skins, culling out non-active bodies.
            int nSkins = skins_.Count;
            active_.Clear();
            //  sweep the sorted list for potential overlaps
            for (int i = 0; i != nSkins; ++i)
                AddToActive(skins_[i], skinTester_);
        }

        class SkinTester : CollisionSkinPredicate2
        {
            CollisionFunctor collisionFunctor_;
            CollisionSkinPredicate2 collisionPredicate_;
            float collTolerance_;
            CollDetectInfo info_;
            CollisionSystem sys_;

            internal SkinTester()
            {
            }

            internal void Set(CollisionSystem sys, CollisionFunctor collisionFunctor, CollisionSkinPredicate2 collisionPredicate, float collTolerance)
            {
                sys_ = sys;
                collisionFunctor_ = collisionFunctor;
                collisionPredicate_ = collisionPredicate;
                if (collisionPredicate_ == null)
                    collisionPredicate_ = this;
                collTolerance_ = collTolerance;
            }

            private static bool CheckCollidables(CollisionSkin skin0,CollisionSkin skin1)
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

            internal void TestSkin(CollisionSkin b, CollisionSkin s)
            {
                System.Diagnostics.Debug.Assert(b.Owner != null);
                System.Diagnostics.Debug.Assert(b.Owner.IsActive);
                if (!collisionPredicate_.ConsiderSkinPair(b, s))
                    return;
                info_.Skin0 = b;
                info_.Skin1 = s;
                int nSkin0 = info_.Skin0.NumPrimitives;
                int nSkin1 = info_.Skin1.NumPrimitives;
                for (info_.IndexPrim0 = 0; info_.IndexPrim0 != nSkin0; ++info_.IndexPrim0)
                {
                    for (info_.IndexPrim1 = 0; info_.IndexPrim1 != nSkin1; ++info_.IndexPrim1)
                    {
                        if (CheckCollidables(info_.Skin0, info_.Skin1))
                        {
                            DetectFunctor f =
                              sys_.GetCollDetectFunctor(info_.Skin0.GetPrimitiveNewWorld(info_.IndexPrim0).Type,
                                info_.Skin1.GetPrimitiveNewWorld(info_.IndexPrim1).Type);
                            if (f != null)
                                f.CollDetect(info_, collTolerance_, collisionFunctor_);
                        }
                    }
                }
            }

            public override bool ConsiderSkinPair(CollisionSkin skin0, CollisionSkin skin1)
            {
                return true;
            }
        }

        void AddToActive(CollisionSkin cs, SkinTester st)
        {
            int n = active_.Count;
            float xMin = cs.WorldBoundingBox.Min.X;
            bool active = (cs.Owner != null) && cs.Owner.IsActive;
            for (int i = 0; i != n; )
            {
                CollisionSkin asi = active_[i];
                if (asi.WorldBoundingBox.Max.X < xMin)
                {
                    //  prune no longer interesting boxes from potential overlaps
                    --n;
                    active_.RemoveAt(i);
                }
                else
                {
                    bool active2 = active || (active_[i].Owner != null && asi.Owner.IsActive);
                    if (active2 && BoundingBoxHelper.OverlapTest(ref cs.WorldBoundingBox, ref asi.WorldBoundingBox))
                        if (active)
                            st.TestSkin(cs, asi);
                        else
                            st.TestSkin(asi, cs);
                    ++i;
                }
            }
            active_.Add(cs);
        }

        public override bool SegmentIntersect(out float fracOut, out CollisionSkin skinOut, out Microsoft.Xna.Framework.Vector3 posOut, out Microsoft.Xna.Framework.Vector3 normalOut, JigLibX.Geometry.Segment seg, CollisionSkinPredicate1 collisionPredicate)
        {
            fracOut = float.MaxValue;
            skinOut = null;
            posOut = normalOut = Vector3.Zero;

            Vector3 min = seg.GetPoint(0);
            Vector3 tmp = seg.GetEnd();
            Vector3 max;
            Vector3.Max(ref min, ref tmp, out max);
            Vector3.Min(ref min, ref tmp, out min);

            BoundingBox box = new BoundingBox(min, max);
            float frac;
            Vector3 pos;
            Vector3 normal;

            active_.Clear();
            Extract(min, max, active_);

            int nActive = active_.Count;
            for (int i = 0; i != nActive; ++i)
            {
                CollisionSkin skin = active_[i];
                if (collisionPredicate == null || collisionPredicate.ConsiderSkin(skin))
                    if (BoundingBoxHelper.OverlapTest(ref box, ref skin.WorldBoundingBox))
                        if (skin.SegmentIntersect(out frac, out pos, out normal, seg))
                            if (frac >= 0 && frac < fracOut)
                            {
                                fracOut = frac;
                                skinOut = skin;
                                posOut = pos;
                                normalOut = normal;
                            }
            }
            return (fracOut <= 1);
        }

        void MaybeSort()
        {
            if (dirty_)
            {
                skins_.Sort(this);
                dirty_ = false;
            }
        }

        public int Compare(CollisionSkin x, CollisionSkin y)
        {
            float f = x.WorldBoundingBox.Min.X - y.WorldBoundingBox.Min.X;
            return (f < 0) ? -1 : (f > 0) ? 1 : 0;
        }
    }
}