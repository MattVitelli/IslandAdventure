#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
#endregion

namespace JigLibX.Collision
{

    #region public struct CollDetectInfo

    /// <summary>
    /// Details about which parts of the skins are colliding.
    /// </summary>
    public struct CollDetectInfo
    {

        public int IndexPrim0; // index into skin0 primitive
        public int IndexPrim1; // index into skin1 primitive

        public CollisionSkin Skin0;
        public CollisionSkin Skin1;

        public CollDetectInfo(CollisionSkin skin0, CollisionSkin skin1, int indexPrim0, int indexPrim1)
        {
            this.IndexPrim0 = indexPrim0;
            this.IndexPrim1 = indexPrim1;
            this.Skin0 = skin0;
            this.Skin1 = skin1;
        }

        public static CollDetectInfo Empty
        {
            get { return new CollDetectInfo(null, null, 0, 0); }
        }

    }
    #endregion

    public struct SmallCollPointInfo
    {
        /// <summary>
        /// Estimated Penetration before the objects collide (can be -ve)
        /// </summary>
        public float InitialPenetration;

        /// <summary>
        /// Positions relative to body 0 (in world space)
        /// </summary>
        public Vector3 R0;

        /// <summary>
        /// positions relative to body 1 (if there is a body1)
        /// </summary>
        public Vector3 R1;

        public SmallCollPointInfo(ref Vector3 R0, ref Vector3 R1, float initialPenetration)
        {
            this.R0 = R0;
            this.R1 = R1;
            this.InitialPenetration = initialPenetration;
        }

        public SmallCollPointInfo(Vector3 R0, Vector3 R1, float initialPenetration)
        {
            this.R0 = R0;
            this.R1 = R1;
            this.InitialPenetration = initialPenetration;
        }
    }

    #region public class CollPointInfo
    public class CollPointInfo
    {
        public SmallCollPointInfo info;

        /// <summary>
        /// Estimated Penetration before the objects collide (can be -ve)
        /// </summary>
        public float InitialPenetration
        {
            get
            {
                return info.InitialPenetration;
            }
        }

        /// <summary>
        /// Positions relative to body 0 (in world space)
        /// </summary>
        public Vector3 R0
        {
            get
            {
                return info.R0;
            }
        }

        /// <summary>
        /// positions relative to body 1 (if there is a body1)
        /// </summary>
        public Vector3 R1
        {
            get
            {
                return info.R1;
            }
        }

        /// <summary>
        /// Used by physics to cache desired minimum separation velocity
        /// in the normal direction
        /// </summary>
        public float MinSeparationVel;

        /// <summary>
        /// Used by physics to cache value used in calculating impulse
        /// </summary>
        public float Denominator;

        /// <summary>
        /// Used by physics to accumulated the normal impulse
        /// </summary>
        public float AccumulatedNormalImpulse;

        /// <summary>
        /// Used by physics to accumulated the normal impulse
        /// </summary>
        public Vector3 AccumulatedFrictionImpulse;

        /// <summary>
        /// Used by physics to accumulated the normal impulse
        /// </summary>
        public float AccumulatedNormalImpulseAux;

        /// <summary>
        /// Used by physics to cache the world position (not really
        /// needed? pretty useful in debugging!)
        /// </summary>
        public Vector3 Position;

        public void Init(ref SmallCollPointInfo info)
        {
            this.info = info;
            this.Denominator = 0.0f;
            this.AccumulatedNormalImpulse = 0.0f;
            this.AccumulatedNormalImpulseAux = 0.0f;
            this.AccumulatedFrictionImpulse = Vector3.Zero;
            this.Position = Vector3.Zero;
            this.MinSeparationVel = 0.0f;
        }
    }
    #endregion

    /// <summary>
    /// Contains all the details about a collision between two skins,
    /// each of which may be associated with a tBody.  Each collision
    /// can have a number of points associated with it
    /// </summary>
    public class CollisionInfo
    {
        public const int MaxCollisionPoints = 10;

        public MaterialPairProperties MatPairProperties;

        public CollDetectInfo SkinInfo;
        internal Vector3 dirToBody0; // hack
        private bool satisfied;


        public const int InitialCollisionInfoStack = 64;
        public const int InitialCollisionPointInfoStack = 4096;
        private static Stack<CollisionInfo> freeInfos = new Stack<CollisionInfo>(InitialCollisionInfoStack);
        private static Stack<CollPointInfo> freePtInfos = new Stack<CollPointInfo>(InitialCollisionPointInfoStack);
        private CollisionInfo() { }

        static CollisionInfo()
        {
            // prep our collision info free list
            for (int i = 0; i < InitialCollisionInfoStack; ++i)
            {
                freeInfos.Push(new CollisionInfo());
            }
            for (int i = 0; i < InitialCollisionPointInfoStack; ++i)
            {
                freePtInfos.Push(new CollPointInfo());
            }
        }
        #region Properties

        public bool Satisfied
        {
            get { return satisfied; }
            set { satisfied = value; }
        }

        public Vector3 DirToBody0
        {
            get { return dirToBody0; }
            set { dirToBody0 = value; }
        }

        public CollPointInfo[] PointInfo = new CollPointInfo[MaxCollisionPoints];
        public int NumCollPts = 0;

        #endregion

        private unsafe void Init(CollDetectInfo info, Vector3 dirToBody0, SmallCollPointInfo* pointInfos, int numPointInfos)
        {
            this.SkinInfo = info;
            this.dirToBody0 = dirToBody0;

            int ID0 = info.Skin0.GetMaterialID(info.IndexPrim0);
            int ID1 = info.Skin1.GetMaterialID(info.IndexPrim1);

            MaterialTable matTable = info.Skin0.CollisionSystem.MaterialTable;

            if (ID0 == (int)MaterialTable.MaterialID.UserDefined || (int)ID1 == (int)MaterialTable.MaterialID.UserDefined)
            {
                MaterialProperties prop0, prop1;

                if (ID0 == (int)MaterialTable.MaterialID.UserDefined)
                    prop0 = info.Skin0.GetMaterialProperties(info.IndexPrim0);
                else
                    prop0 = matTable.GetMaterialProperties(ID0);

                if (ID1 == (int)MaterialTable.MaterialID.UserDefined)
                    prop1 = info.Skin1.GetMaterialProperties(info.IndexPrim1);
                else
                    prop1 = matTable.GetMaterialProperties(ID1);

                MatPairProperties.Restitution = prop0.Elasticity * prop1.Elasticity;
                MatPairProperties.StaticFriction = prop0.StaticRoughness * prop1.StaticRoughness;
                MatPairProperties.DynamicFriction = prop0.DynamicRoughness * prop1.DynamicRoughness;
            }
            else
            {
                MatPairProperties = matTable.GetPairProperties(ID0, ID1);
            }

            numPointInfos = (numPointInfos > MaxCollisionPoints) ? MaxCollisionPoints : numPointInfos;

            NumCollPts = 0;
            for (int i = 0; i < numPointInfos; ++i)
            {
                if (freePtInfos.Count == 0)
                {
                    freePtInfos.Push(new CollPointInfo());
                }
                this.PointInfo[NumCollPts] = freePtInfos.Pop();
                this.PointInfo[NumCollPts++].Init( ref pointInfos[i] );
            }
        }

        // public List<CollPointInfo>

        private void Destroy()
        {
            for (int i = 0; i < NumCollPts; ++i)
            {
                freePtInfos.Push( this.PointInfo[i] );
            }
            SkinInfo.Skin0 = null;
            SkinInfo.Skin1 = null;
        }

        /// <summary>
        /// CollisionInfos will be given out from a pool.  If more than
        /// MaxCollisionPoints are passed in, the input positions will
        /// be silently truncated!
        /// </summary>
        /// <param name="info"></param>
        /// <param name="dirToBody0"></param>
        /// <param name="pointInfos"></param>
        /// <param name="numPointInfos"></param>
        /// <returns></returns>
        public static unsafe CollisionInfo GetCollisionInfo(CollDetectInfo info,
            Vector3 dirToBody0, SmallCollPointInfo* pointInfos, int numCollPts)
        {
            if (freeInfos.Count == 0)
                freeInfos.Push(new CollisionInfo());

            CollisionInfo collInfo = freeInfos.Pop();//[freeInfos.Count - 1];
            collInfo.Init(info, dirToBody0, pointInfos, numCollPts);
            //freeInfos.RemoveAt(freeInfos.Count - 1);
            return collInfo;
        }

        /// <summary>
        /// Return this info to the pool.
        /// </summary>
        /// <param name="info"></param>
        public static void FreeCollisionInfo(CollisionInfo info)
        {
            info.Destroy();
            freeInfos.Push(info);
        }

    }
}
