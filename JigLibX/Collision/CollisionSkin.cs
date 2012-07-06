#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using JigLibX.Physics;
using JigLibX.Geometry;
using JigLibX.Math;
using Microsoft.Xna.Framework;
#endregion

namespace JigLibX.Collision
{

    /// <summary>
    /// Some skins may be owned by a physical body too.
    /// </summary>
    public class CollisionSkin
    {
        private CollisionSystem collSystem;
        private Body owner;

        // Bounding box in world reference frame - includes all children too
        public BoundingBox WorldBoundingBox = new BoundingBox();

        private List<CollisionInfo> collisions = new List<CollisionInfo>(16);
        private List<CollisionSkin> nonCollidables = new List<CollisionSkin>();

        private List<Primitive> primitivesOldWorld = new List<Primitive>();                     // old value of primitives in world space
        private List<Primitive> primitivesNewWorld = new List<Primitive>();                     // our primitives in world space
        private List<Primitive> primitivesLocal = new List<Primitive>();                        // Our primitives in local space
        private List<int> materialIDs = new List<int>();                                        // material for each primitive
        private List<MaterialProperties> materialProperties 
            = new List<MaterialProperties>();                                        // values used when mat ID is USER_DEFINED

        private Transform transformOld = Transform.Identity;
        private Transform transformNew = Transform.Identity;

        private static int idCounter;

        internal object ExternalData;
        internal int ID;
        public event CollisionCallbackFn callbackFn;

        public CollisionSkin()
        {
            this.ID = idCounter++;
            this.owner = null;

            collSystem = null;
        }

        public CollisionSkin(Body owner)
        {
            this.ID = idCounter++;
            this.owner = owner;

            collSystem = null;
        }

        public bool OnCollisionEvent(CollisionSkin skin0, CollisionSkin skin1)
        {
            if (callbackFn != null)
            {
                return callbackFn(skin0, skin1);
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Get or Set the owner of a skin.
        /// </summary>
        public Body Owner
        {
            get { return this.owner; }
            set { this.owner = value; }
        }

        /// <summary>
        /// Adds a primitive to this collision skin - the primitive is
        /// copied (so you can pass in something on the stack, or delete
        /// the original) - perhaps using reference counting.  Returns the
        /// primitive index, or -1 if failure Also takes that material ID
        /// and the properties used when a collision ID is USER_DEFINED
        /// </summary>
        private int AddPrimitive(Primitive prim, int matID, MaterialProperties matProps)
        {
            Primitive newPrim = prim.Clone();

            if (newPrim == null) 
                throw new ArgumentException("Not able to clone primitive!");

            materialIDs.Add(matID);
            materialProperties.Add(matProps);

            primitivesOldWorld.Add(prim.Clone());
            primitivesNewWorld.Add(prim.Clone());
            primitivesLocal.Add(newPrim);

            UpdateWorldBoundingBox();

            return materialIDs.Count - 1;
        }

        public int AddPrimitive(Primitive prim, int matID)
        {
            if (matID == (int)MaterialTable.MaterialID.UserDefined)
                throw new ArgumentException("matID can't be set to 'UserDefined'");

            return AddPrimitive(prim, matID, MaterialProperties.Unset);
        }

        public int AddPrimitive(Primitive prim, MaterialProperties matProps)
        {
            return AddPrimitive(prim, (int)MaterialTable.MaterialID.UserDefined, matProps);
        }

        /// <summary>
        /// Removes and destroys all primitives
        /// </summary>
        public void RemoveAllPrimitives()
        {
            primitivesOldWorld.Clear();
            primitivesNewWorld.Clear();
            primitivesLocal.Clear();
            materialIDs.Clear();
            materialProperties.Clear();
        }

        /// <summary>
        /// Returns the number of registered primitives
        /// </summary>
        public int NumPrimitives
        {
            get { return primitivesLocal.Count; }
        }

        /// <summary>
        /// Gets the primitive in local space
        /// </summary>
        /// <param name="prim"></param>
        /// <returns></returns>
        public Primitive GetPrimitiveLocal(int prim)
        {
            return primitivesLocal[prim];
        }

        /// <summary>
        /// Gets the old value of primitive in world space 
        /// </summary>
        /// <param name="prim"></param>
        /// <returns></returns>
        public Primitive GetPrimitiveOldWorld(int prim)
        {
            return primitivesOldWorld[prim];
        }

        /// <summary>
        /// Gets the new value of primitive in world space
        /// </summary>
        /// <param name="prim"></param>
        /// <returns></returns>
        public Primitive GetPrimitiveNewWorld(int prim)
        {
            return primitivesNewWorld[prim];
        }

        /// <summary>
        /// Gets the material ID for a primitive 
        /// </summary>
        /// <param name="prim"></param>
        /// <returns></returns>
        public int GetMaterialID(int prim)
        {
            return materialIDs[prim];
        }

        /// <summary>
        /// Returns the material properties for a primitive
        /// </summary>
        /// <param name="prim"></param>
        /// <returns></returns>
        public MaterialProperties GetMaterialProperties(int prim)
        {
            return materialProperties[prim];
        }

        /// <summary>
        /// Sets the material properties for a primitive. In this case the
        /// material ID will be automatically set to USER_DEFINED
        /// </summary>
        /// <param name="prim"></param>
        /// <param name="matProperties"></param>
        public void SetMaterialProperties(int prim, MaterialProperties matProperties)
        {
            materialProperties[prim] = matProperties;
            materialIDs[prim] = (int)MaterialTable.MaterialID.UserDefined;
        }

        /// <summary>
        /// returns the total volume
        /// </summary>
        /// <returns></returns>
        public float GetVolume()
        {
            float result = 0.0f;
            for (int prim = primitivesLocal.Count; prim-- != 0; )
                result += primitivesLocal[prim].GetVolume();
            return result;
        }

        /// <summary>
        /// returns the total surface area
        /// </summary>
        /// <returns></returns>
        public float GetSurfaceArea()
        {
            float result = 0.0f;
            for (int prim = primitivesLocal.Count; prim-- != 0; )
                result += primitivesLocal[prim].GetSurfaceArea();
            return result;
        }

        /// <summary>
        /// these get called during the collision detection
        /// </summary>
        /// <param name="transform"></param>
        public void SetNewTransform(ref Transform transform)
        {
            transformNew = transform;
            Transform t;

            for(int prim = primitivesNewWorld.Count; prim-- != 0;)
            {
                t = primitivesLocal[prim].Transform;
                primitivesNewWorld[prim].Transform = transform * t;
            }

            UpdateWorldBoundingBox();

            if (collSystem != null)
                collSystem.CollisionSkinMoved(this);

        }

        public void SetOldTransform(ref Transform transform)
        {
            transformOld = transform;
            Transform t;

            for (int prim = primitivesNewWorld.Count; prim-- != 0; )
            {
                t = primitivesLocal[prim].Transform;
                primitivesOldWorld[prim].Transform = transform * t;
            }

            UpdateWorldBoundingBox();

            if (collSystem != null)
                collSystem.CollisionSkinMoved(this);

        }

        public void SetTransform(ref Transform transformOld, ref Transform transformNew)
        {
            this.transformOld = transformOld;
            this.transformNew = transformNew;

            for (int prim = primitivesNewWorld.Count; prim-- != 0; )
            {
                primitivesOldWorld[prim].Transform = transformOld * primitivesLocal[prim].Transform;
                primitivesNewWorld[prim].Transform = transformNew * primitivesLocal[prim].Transform;
            }

            UpdateWorldBoundingBox();

            if (collSystem != null)
                collSystem.CollisionSkinMoved(this);
        }

        /// <summary>
        /// Applies a transform to the local primitives (e.g. to shift
        /// everything after calculating CoM etc)
        /// </summary>
        /// <param name="transform"></param>
        public void ApplyLocalTransform(Transform transform)
        {
            Transform t;
            for (int prim = primitivesNewWorld.Count; prim-- != 0; )
            {
                t = primitivesLocal[prim].Transform;
                primitivesLocal[prim].Transform = transform * t;
            }

            SetTransform(ref transformOld, ref transformNew);
        }

        public Vector3 OldPosition
        {
            get { return transformOld.Position; }
        }

        public Vector3 NewPosition
        {
            get { return transformNew.Position; }
        }

        public Matrix OldOrient
        {
            get { return transformOld.Orientation; }
        }

        public Matrix NewOrient
        {
            get { return transformNew.Orientation; }
        }

        public Transform OldTransform
        {
            get { return transformOld; }
        }

        public Transform NewTransform
        {
            get { return transformNew; }
        }

        /// <summary>
        /// Updates bounding volume of this skin 
        /// </summary>
        public void UpdateWorldBoundingBox()
        {
            BoundingBox temp = BoundingBoxHelper.InitialBox;

            for (int iold = primitivesOldWorld.Count; iold-- != 0; )
            {
                BoundingBoxHelper.AddPrimitive(primitivesOldWorld[iold], ref temp);
            }

            if (collSystem != null && collSystem.UseSweepTests)
            {
                for (int inew = primitivesNewWorld.Count; inew-- != 0; )
                {
                    BoundingBoxHelper.AddPrimitive(primitivesNewWorld[inew], ref temp);
                }
            }
            WorldBoundingBox = BoundingBoxHelper.InitialBox;
            BoundingBoxHelper.AddPoint(ref temp.Min, ref WorldBoundingBox);
            BoundingBoxHelper.AddPoint(ref temp.Max, ref WorldBoundingBox);
        }

        /// <summary>
        /// Intended for internal use by Physics - we get told about the
        /// collisions we're involved with. Used to resolve penetrations.
        /// </summary>
        public List<CollisionInfo> Collisions
        {
            get { return this.collisions; }
        }

        /// <summary>
        /// Each skin can contain a list of other skins it shouldn't
        /// collide with. You only need to add skins from another "family"
        /// - i.e.  don't explicitly add children/parents
        /// </summary>
        public List<CollisionSkin> NonCollidables
        {
            get { return nonCollidables; }
        }

        public CollisionSystem CollisionSystem
        {
            set { this.collSystem = value; }
            get { return this.collSystem; }
        }

        /// <summary>
        /// Every skin must support a ray/segment intersection test -
        /// operates on the new value of the primitives
        /// </summary>
        /// <param name="frac"></param>
        /// <param name="pos"></param>
        /// <param name="normal"></param>
        /// <param name="seg"></param>
        /// <returns></returns>
        public bool SegmentIntersect(out float frac, out Vector3 pos, out Vector3 normal, Segment seg)
        {
            Vector3 segEnd = seg.GetEnd();
            frac = float.MaxValue;

            float thisSegLenRelToOrig = 1.0f;
            Segment segCopy = seg;

            pos = normal = Vector3.Zero;

            for (int prim = primitivesNewWorld.Count; prim-- != 0; )
            {
                float thisFrac;
                Vector3 newPosition = pos;

                if (primitivesNewWorld[prim].SegmentIntersect(out thisFrac, out newPosition, out normal, segCopy))
                {
                    pos = newPosition;
                    frac = thisFrac * thisSegLenRelToOrig;
                    segCopy.Delta *= thisFrac;
                    thisSegLenRelToOrig *= frac;
                }
            }

            //System.Diagnostics.Debug.WriteLineIf(frac <= 1.0f, pos);

            return (frac <= 1.0f);
        }

        /// <summary>
        /// Helper to calculate the combined mass, centre of mass, and
        /// inertia tensor about the origin and the CoM (for the local
        /// primitives) primitiveProperties indicates the properties used
        /// for all primitives - so the mass is the total mass
        /// </summary>
        /// <param name="primitiveProperties"></param>
        /// <param name="mass"></param>
        /// <param name="centerOfMass"></param>
        /// <param name="inertiaTensor"></param>
        /// <param name="inertiaTensorCoM"></param>
        public void GetMassProperties(PrimitiveProperties primitiveProperties,
            out float mass, out Vector3 centerOfMass, out Matrix inertiaTensor, out Matrix inertiaTensorCoM)
        {
            mass = 0.0f;
            centerOfMass = Vector3.Zero;
            inertiaTensor = new Matrix();

            float totalWeighting = 0.0f;

            if (primitiveProperties.MassType == PrimitiveProperties.MassTypeEnum.Mass)
            {
                for (int prim = primitivesLocal.Count; prim-- != 0; )
                {
                    if (primitiveProperties.MassDistribution == PrimitiveProperties.MassDistributionEnum.Solid)
                        totalWeighting += primitivesLocal[prim].GetVolume();
                    else
                        totalWeighting += primitivesLocal[prim].GetSurfaceArea();
                }
            }

            for (int prim = primitivesLocal.Count; prim-- != 0; )
            {
                float m; Vector3 com; Matrix it;

                PrimitiveProperties primProperties = primitiveProperties;

                if (primitiveProperties.MassType == PrimitiveProperties.MassTypeEnum.Mass)
                {
                    float weighting = 0.0f;
                    if (primitiveProperties.MassDistribution == PrimitiveProperties.MassDistributionEnum.Solid)
                        weighting = primitivesLocal[prim].GetVolume();
                    else
                        weighting = primitivesLocal[prim].GetSurfaceArea();
                    primProperties.MassOrDensity *= weighting / totalWeighting;
                }

                primitivesLocal[prim].GetMassProperties(primProperties, out m, out com, out it);

                mass += m;
                centerOfMass += m * com;
                inertiaTensor += it;
            }

            inertiaTensorCoM = Matrix.Identity;

            if (mass > 0.0f)
            {
                centerOfMass /= mass;

                // Transfer of axe theorem
                inertiaTensorCoM.M11 = inertiaTensor.M11 - mass * (centerOfMass.Y * centerOfMass.Y + centerOfMass.Z * centerOfMass.Z);
                inertiaTensorCoM.M22 = inertiaTensor.M22 - mass * (centerOfMass.Z * centerOfMass.Z + centerOfMass.X * centerOfMass.X);
                inertiaTensorCoM.M33 = inertiaTensor.M33 - mass * (centerOfMass.X * centerOfMass.X + centerOfMass.Y * centerOfMass.Y);

                // CHECK THIS. seems strange for me
                inertiaTensorCoM.M12 = inertiaTensorCoM.M21 = inertiaTensor.M12 + mass * centerOfMass.X * centerOfMass.Y;
                inertiaTensorCoM.M23 = inertiaTensorCoM.M32 = inertiaTensor.M23 + mass * centerOfMass.Y * centerOfMass.Z;
                inertiaTensorCoM.M31 = inertiaTensorCoM.M13 = inertiaTensor.M31 + mass * centerOfMass.Z * centerOfMass.X;
            }

            if (primitiveProperties.MassType == PrimitiveProperties.MassTypeEnum.Mass)
                mass = primitiveProperties.MassOrDensity;


        }

        /// <summary>
        /// Helper to calculate the combined mass, centre of mass, and
        /// inertia tensor about the origin and the CoM (for the local
        /// primitives) primitiveProperties is an array of properties -
        /// must be the same number as there are primitives
        /// </summary>
        /// <param name="primitiveProperties"></param>
        /// <param name="mass"></param>
        /// <param name="centerOfMass"></param>
        /// <param name="inertiaTensor"></param>
        /// <param name="inertiaTensorCoM"></param>
        public void GetMassProperties(PrimitiveProperties[] primitiveProperties,
            out float mass, out Vector3 centerOfMass, out Matrix inertiaTensor, out Matrix inertiaTensorCoM)
        {
            mass = 0.0f;
            centerOfMass = Vector3.Zero;
            inertiaTensor = Matrix.Identity;
            inertiaTensorCoM = Matrix.Identity;

            for (int prim = primitivesLocal.Count; prim-- != 0; )
            {
                float m;
                Vector3 com;
                Matrix it;
                primitivesLocal[prim].GetMassProperties(primitiveProperties[prim], out m, out com, out it);

                mass += m;
                centerOfMass += m * com;
                inertiaTensor += it;
            }

            if (mass > 0.0f)
            {
                centerOfMass /= mass;

                // Transfer of axe theorem
                inertiaTensorCoM.M11 = inertiaTensor.M11 - mass * (centerOfMass.Y * centerOfMass.Y + centerOfMass.Z * centerOfMass.Z);
                inertiaTensorCoM.M22 = inertiaTensor.M22 - mass * (centerOfMass.Z * centerOfMass.Z + centerOfMass.X * centerOfMass.X);
                inertiaTensorCoM.M33 = inertiaTensor.M33 - mass * (centerOfMass.X * centerOfMass.X + centerOfMass.Y * centerOfMass.Y);

                // CHECK THIS. seems strange for me
                inertiaTensorCoM.M12 = inertiaTensorCoM.M21 = inertiaTensor.M12 + mass * centerOfMass.X * centerOfMass.Y;
                inertiaTensorCoM.M23 = inertiaTensorCoM.M32 = inertiaTensor.M23 + mass * centerOfMass.Y * centerOfMass.Z;
                inertiaTensorCoM.M31 = inertiaTensorCoM.M13 = inertiaTensor.M31 + mass * centerOfMass.Z * centerOfMass.X;

            }
        }

    }
}
