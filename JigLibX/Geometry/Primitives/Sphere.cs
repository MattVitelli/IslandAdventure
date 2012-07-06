#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
#endregion


namespace JigLibX.Geometry
{
    public class Sphere : Primitive
    {
        private float radius;
        
        private static Sphere hugeSphere = new Sphere(Vector3.Zero, float.MaxValue);

        public Sphere(Vector3 pos, float radius) : base((int)PrimitiveType.Sphere)
        {
            this.transform.Position = pos;
            this.radius = radius;
        }

        public override Primitive Clone()
        {
            return new Sphere(this.transform.Position, this.radius);
        }

        public override bool SegmentIntersect(out float frac, out Vector3 pos, out Vector3 normal, Segment seg)
        {
            bool result;
            result = Intersection.SegmentSphereIntersection(out frac, seg, this);

            if (result)
            {
                pos = seg.GetPoint(frac);
                normal = pos - this.transform.Position;

                JiggleMath.NormalizeSafe(ref normal);
            }
            else
            {
                pos = Vector3.Zero;
                normal = Vector3.Zero;
            }

            return result;
        }

        public override void GetMassProperties(PrimitiveProperties primitiveProperties, out float mass, out Vector3 centerOfMass, out Matrix inertiaTensor)
        {
            if (primitiveProperties.MassType == PrimitiveProperties.MassTypeEnum.Mass)
            {
                mass = primitiveProperties.MassOrDensity;
            }
            else
            {
                if (primitiveProperties.MassDistribution == PrimitiveProperties.MassDistributionEnum.Solid)
                    mass = GetVolume() * primitiveProperties.MassOrDensity;
                else
                    mass = GetSurfaceArea() * primitiveProperties.MassOrDensity;
            }

            centerOfMass = this.transform.Position;
            float Ixx;
            if (primitiveProperties.MassDistribution == PrimitiveProperties.MassDistributionEnum.Solid)
                Ixx = 0.4f * mass * radius;
            else
                Ixx = (2.0f / 3.0f) * mass * radius;


            inertiaTensor = Matrix.Identity;
            inertiaTensor.M11 = inertiaTensor.M22 = inertiaTensor.M33 = Ixx;

            // Transfer of axe theorem
            inertiaTensor.M11 = inertiaTensor.M11 + mass * (centerOfMass.Y * centerOfMass.Y + centerOfMass.Z * centerOfMass.Z);
            inertiaTensor.M22 = inertiaTensor.M22 + mass * (centerOfMass.Z * centerOfMass.Z + centerOfMass.X * centerOfMass.X);
            inertiaTensor.M33 = inertiaTensor.M33 + mass * (centerOfMass.X * centerOfMass.X + centerOfMass.Y * centerOfMass.Y);

            inertiaTensor.M12 = inertiaTensor.M21 = inertiaTensor.M12 - mass * centerOfMass.X * centerOfMass.Y;
            inertiaTensor.M23 = inertiaTensor.M32 = inertiaTensor.M23 - mass * centerOfMass.Y * centerOfMass.Z;
            inertiaTensor.M31 = inertiaTensor.M13 = inertiaTensor.M31 - mass * centerOfMass.Z * centerOfMass.X;
        }

        public override Transform Transform
        {
            get{return this.transform;}
            set{this.transform = value;}
        }

        public override float GetVolume()
        {
            return (4.0f / 3.0f) * MathHelper.Pi * radius * radius * radius;
        }

        public override float GetSurfaceArea()
        {
            return 4.0f * MathHelper.Pi * radius * radius;
        }

        public Vector3 Position
        {
            get { return this.transform.Position; }
            set { this.transform.Position = value; }
        }

        public float Radius
        {
            get { return this.radius; }
            set { this.radius = value; }
        }

        public static Sphere HugeSphere
        {
            get { return hugeSphere; }
        }


    }
}
