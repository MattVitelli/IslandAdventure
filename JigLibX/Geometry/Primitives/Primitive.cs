#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
#endregion

namespace JigLibX.Geometry
{

    #region PrimitiveProperties
    public struct PrimitiveProperties
    {
        public enum MassDistributionEnum
        {
            Solid,Shell
        }

        // density is mass per volume of SOLID, otherwise mass per surface area
        public enum MassTypeEnum
        {
            Mass,Density
        }

        public MassTypeEnum MassType;
        public MassDistributionEnum MassDistribution;
        public float MassOrDensity;

        public PrimitiveProperties(MassDistributionEnum massDistribution,
            MassTypeEnum massType, float massOrDensity)
        {
            this.MassDistribution = massDistribution;
            this.MassOrDensity = massOrDensity;
            this.MassType = massType;
        }

    }
    #endregion

    #region PrimitiveType

    /// <summary>
    /// The JigLibX default Primitives.
    /// </summary>
    public enum PrimitiveType
    {
        AABox,
        Box,
        Capsule,
        Heightmap,
        Plane,
        Sphere,
        TriangleMesh,
        Cylinder,
        NumTypes // can add more user-defined types
    }

    #endregion

    /// All geometry primitives should derive from this so that it's possible to 
    /// cast them into the correct type without the overhead/hassle of RTTI or 
    /// virtual fns specific to just one class of primitive. Just do a static cast
    /// based on the type, or use the Get functions
    ///
    /// However, destruction requires virtual functions really, as does supporting other
    /// user-defined primitives
    public abstract class Primitive
    {

        private int type;

        internal Transform transform = Transform.Identity;

        public Primitive(int type)
        {
            this.type = type;
        }

        /// <summary>
        /// Returns a copy
        /// </summary>
        /// <returns></returns>
        public abstract Primitive Clone();

        public virtual Transform Transform 
        { 
            get 
            { 
                return transform; 
            } 
            set 
            { 
                transform = value; 
            } 
        }

        public virtual Matrix TransformMatrix
        {
            get
            {
                Matrix trans = transform.Orientation;
                trans.Translation = transform.Position;
                return trans;
            }
        }

        public virtual Matrix InverseTransformMatrix
        {
            get
            {
                Matrix trans = transform.Orientation;
                trans.Translation = transform.Position;
                return Matrix.Invert(trans);
            }
        }

        /// <summary>
        /// Must support intersection with a segment (ray cast)
        /// </summary>
        /// <param name="frac"></param>
        /// <param name="normal"></param>
        /// <param name="seg"></param>
        /// <returns></returns>
        public abstract bool SegmentIntersect(out float frac,out Vector3 pos,
            out Vector3 normal,Segment seg);

        /// <summary>
        /// Calculate and return the volume
        /// </summary>
        /// <returns></returns>
        public abstract float GetVolume();

        /// <summary>
        /// Calculate and return the surface area
        /// </summary>
        /// <returns></returns>
        public abstract float GetSurfaceArea();

        /// <summary>
        /// Returns the mass, center of mass, and intertia tensor around the origin
        /// </summary>
        /// <param name="primitiveProperties"></param>
        /// <param name="mass"></param>
        /// <param name="centerOfMass"></param>
        /// <param name="inertiaTensor"></param>
        public abstract void GetMassProperties(PrimitiveProperties primitiveProperties,
            out float mass, out Vector3 centerOfMass, out Matrix inertiaTensor);

        /// <summary>
        /// Returns a bounding box that covers this primitive. Default returns a huge box, so
        /// implement this in the derived class for efficiency
        /// </summary>
        /// <returns></returns>
        public virtual void GetBoundingBox(out AABox box)
        {
            box = AABox.HugeBox;
        }

        /// <summary>
        /// Returns a bounding box that covers this primitive. Default returns a huge box, so
        /// implement this in the derived class for efficiency
        /// </summary>
        /// <returns></returns>
        public AABox GetBoundingBox()
        {
            AABox result;
            GetBoundingBox(out result);
            return result;
        }

        public int Type
        {
            get { return this.type; }
        }

    }
}
