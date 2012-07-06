#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Physics;
using JigLibX.Geometry;
using JigLibX.Collision;
using JigLibX.Utils;
using System.Collections.ObjectModel;
#endregion

namespace JigLibX.Collision
{

    /// <summary>
    /// The user of CollisionSystem creates an object derived from
    /// CollisionFunctor and passes it in to
    /// DetectCollisions. For every collision detected
    /// the functor gets called so that the user can decide if they want
    /// to keep the collision.
    /// </summary>
    public abstract class CollisionFunctor
    {
        /// <summary>
        /// Skins are passed back because there maybe more than one skin
        /// per body, and the user can always get the body from the skin.
        /// </summary>
        /// <param name="collDetectInfo"></param>
        /// <param name="dirToBody0"></param>
        /// <param name="pointInfos"></param>
        public unsafe abstract void CollisionNotify(ref CollDetectInfo collDetectInfo,
            ref Vector3 dirToBody0, SmallCollPointInfo* pointInfos, int numCollPts);
    }

    /// The user can create an object derived from this and pass it in
    /// to CollisionSystem.DetectCollisions to indicate whether a pair
    /// of skins should be considered. 
    public abstract class CollisionSkinPredicate2
    {
        /// <summary>
        /// Decides whether a pair of skins should be considered for collision
        /// or not.
        /// </summary>
        /// <param name="skin0"></param>
        /// <param name="skin1"></param>
        /// <returns>True if the pair should be considered otherwise false.</returns>
        public abstract bool ConsiderSkinPair(CollisionSkin skin0, CollisionSkin skin1);
    }

    /// <summary>
    /// The user can create an object derived from this and pass it in
    /// to the ray/segment intersection functions to indicate whether certain
    /// skins should be considered. 
    /// </summary>
    public abstract class CollisionSkinPredicate1
    {
        /// <summary>
        /// Decides whether a CollisionSkin should be considered while
        /// doing SegmentIntersecting tests or not.
        /// </summary>
        /// <param name="skin0">Skin to be considered.</param>
        /// <returns>True if the skin should be considered otherwise false.</returns>
        public abstract bool ConsiderSkin(CollisionSkin skin0);
    }

    /// <summary>
    /// a skin can ask to get a callback when a collision is detected this will be called
    /// if it return false, the contact points will not be generated
    /// </summary>
    /// <param name="skin0">the skin that had the callback on</param>
    /// <param name="skin1">the other skin that we have collided with (maybe null tho would be odd...)</param>
    /// <returns>false to inhibit contact point generation</returns>
    public delegate bool CollisionCallbackFn( CollisionSkin skin0, CollisionSkin skin1);

    /// <summary>
    /// Interface to a class that will contain a list of all the
    /// collision objects in the world, and it will provide ways of
    /// detecting collisions between other objects and these collision
    /// objects.
    /// </summary>
    public abstract class CollisionSystem
    {

        private Dictionary<int,DetectFunctor> detectionFunctors = new Dictionary<int,DetectFunctor>();
        private bool useSweepTests = false;
        private MaterialTable materialTable = new MaterialTable();

        private static CollDetectBoxBox boxBoxCollDetector = new CollDetectBoxBox();
        private static CollDetectBoxStaticMesh boxStaticMeshCollDetector = new CollDetectBoxStaticMesh();
        private static CollDetectCapsuleBox capsuleBoxCollDetector = new CollDetectCapsuleBox();
        private static CollDetectCapsuleCapsule capsuleCapsuleCollDetector = new CollDetectCapsuleCapsule();
        private static CollDetectSphereCapsule sphereCapsuleCollDetector = new CollDetectSphereCapsule();
        private static CollDetectSphereBox sphereBoxCollDetector = new CollDetectSphereBox();
        private static CollDetectSphereSphere sphereSphereCollDetector = new CollDetectSphereSphere();
        private static CollDetectBoxHeightmap boxHeightmapCollDetector = new CollDetectBoxHeightmap();
        private static CollDetectSphereHeightmap sphereHeightmapCollDetector = new CollDetectSphereHeightmap();
        private static CollDetectCapsuleHeightmap capsuleHeightmapCollDetector = new CollDetectCapsuleHeightmap();
        private static CollDetectSphereStaticMesh sphereStaticMeshCollDetector = new CollDetectSphereStaticMesh();
        private static CollDetectCapsuleStaticMesh capsuleStaticMeshCollDetector = new CollDetectCapsuleStaticMesh();
        private static CollDetectBoxPlane boxPlaneCollDetector = new CollDetectBoxPlane();
        private static CollDetectSpherePlane spherePlaneCollDetector = new CollDetectSpherePlane();
        private static CollDetectCapsulePlane capsulePlaneCollDetector = new CollDetectCapsulePlane();

        /// <summary>
        /// Constructor initialized the default CollisionFunctors. Other CollisionFunctors can be added with 
        /// RegisterCollDetectFunctor.
        /// </summary>
        public CollisionSystem()
        {
            RegisterCollDetectFunctor(boxBoxCollDetector);
            RegisterCollDetectFunctor(boxStaticMeshCollDetector);
            RegisterCollDetectFunctor(capsuleBoxCollDetector);
            RegisterCollDetectFunctor(capsuleCapsuleCollDetector);
            RegisterCollDetectFunctor(sphereBoxCollDetector);
            RegisterCollDetectFunctor(sphereSphereCollDetector);
            RegisterCollDetectFunctor(sphereCapsuleCollDetector);
            RegisterCollDetectFunctor(boxHeightmapCollDetector);
            RegisterCollDetectFunctor(sphereHeightmapCollDetector);
            RegisterCollDetectFunctor(capsuleHeightmapCollDetector);
            RegisterCollDetectFunctor(sphereStaticMeshCollDetector);
            RegisterCollDetectFunctor(capsuleStaticMeshCollDetector);
            RegisterCollDetectFunctor(boxPlaneCollDetector);
            RegisterCollDetectFunctor(spherePlaneCollDetector);
            RegisterCollDetectFunctor(capsulePlaneCollDetector);
        }

        /// <summary>
        /// Don't add skins whilst doing detection!
        /// </summary>
        /// <param name="collisionSkin"></param>
        public abstract void AddCollisionSkin(CollisionSkin collisionSkin);

        /// <summary>
        /// Don't remove skins whilst doing detection!
        /// </summary>
        /// <param name="collisionSkin"></param>
        public abstract bool RemoveCollisionSkin(CollisionSkin collisionSkin);

        public abstract ReadOnlyCollection<CollisionSkin> CollisionSkins { get; }

        /// <summary>
        /// Whenever a skin changes position it will call this to let us
        /// update our internal state.
        /// </summary>
        /// <param name="skin"></param>
        public abstract void CollisionSkinMoved(CollisionSkin skin);

        /// <summary>
        /// Detects all collisions between the body and all the registered
        /// collision skins (which should have already had their
        /// positions/bounding volumes etc updated).  For each potential
        /// pair of skins then the predicate (if it exists) will be called
        /// to see whether or not to continue. If the skins are closer
        /// than collTolerance (+ve value means report objects that aren't
        /// quite colliding) then the functor will get called.
        /// You can't just loop over all your bodies calling this, because 
        /// that will double-detect collisions. Use DetectAllCollisions for 
        /// that.
        /// </summary>
        public abstract void DetectCollisions(Body body, CollisionFunctor collisionFunctor,
            CollisionSkinPredicate2 collisionPredicate, float collTolerance);


        /// <summary>
        /// As DetectCollisions but detects for all bodies, testing each pair 
        /// only once
        /// </summary>
        /// <param name="bodies"></param>
        /// <param name="collisionFunctor"></param>
        /// <param name="collisionPredicate"></param>
        /// <param name="collTolerance"></param>
        public abstract void DetectAllCollisions(List<Body> bodies, CollisionFunctor collisionFunctor,
            CollisionSkinPredicate2 collisionPredicate, float collTolerance);

        /// <summary>
        /// type0/1 could be from tCollisionSkinType or they could be
        /// larger values. The collision detection table will get extended
        /// as necessary. You only need to register the function once
        /// (i.e. not for type0, type1 then type1, type 0).
        /// </summary>
        /// <param name="f"></param>
        public void RegisterCollDetectFunctor(DetectFunctor f)
        {
            int key01 = f.Type0 << 16 | f.Type1;
            int key10 = f.Type1 << 16 | f.Type0;

            if (!detectionFunctors.ContainsKey(key01))
                detectionFunctors.Add(key01,f);

            if (!detectionFunctors.ContainsKey(key10))
                detectionFunctors.Add(key10,f);
        }

        /// <summary>
        /// Get the previously registered function for the pair type. May
        /// return 0.
        /// </summary>
        /// <param name="type0"></param>
        /// <param name="type1"></param>
        /// <returns></returns>
        public DetectFunctor GetCollDetectFunctor(int type0, int type1)
        {
            DetectFunctor functor;
            int key01 = type0 << 16 | type1;
            if (detectionFunctors.TryGetValue(key01, out functor))
                return functor;
            else
                return null;
        }

        /// <summary>
        /// Intersect a segment with the world. If non-zero the predicate
        /// allows certain skins to be excluded
        /// </summary>
        /// <param name="seg"></param>
        /// <param name="collisionPredicate"></param>
        /// <returns></returns>
        public abstract bool SegmentIntersect(out float fracOut, out CollisionSkin skinOut, out Vector3 posOut, out Vector3 normalOut,
            Segment seg, CollisionSkinPredicate1 collisionPredicate);

        /// <summary>
        /// Sets whether collision tests should use sweep or overlap
        /// </summary>
        public bool UseSweepTests
        {
            get { return useSweepTests; }
            set { useSweepTests = value; }
        }

        /// <summary>
        /// Get the current MaterialTable of the CollisionSystem.
        /// </summary>
        public MaterialTable MaterialTable
        {
            get { return materialTable; }
        }

    }
}
