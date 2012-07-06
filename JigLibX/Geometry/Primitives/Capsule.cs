#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
#endregion


namespace JigLibX.Geometry
{

    /// <summary>
    /// defines a capsule that is orientated along its body x direction, with
    /// its start at its position.
    /// </summary>
    public class Capsule : Primitive 
    {

        private float length;
        private float radius;

        public Capsule(Vector3 pos,Matrix orient,float radius, float length)
            : base((int)PrimitiveType.Capsule)
        {
            this.transform = new Transform(pos, orient);
            this.length = length;
            this.radius = radius;
        }

        public override bool SegmentIntersect(out float frac, out Vector3 pos, out Vector3 normal, Segment seg)
        {
            bool result = Intersection.SegmentCapsuleIntersection(out frac, seg, this);

            if (result)
            {
                pos = seg.GetPoint(frac);
                normal = pos - transform.Position;
                normal -= Vector3.Dot(normal, transform.Orientation.Backward) * transform.Orientation.Backward;
                JiggleMath.NormalizeSafe(ref normal);
            }
            else
            {
                pos = normal = Vector3.Zero;
            }

            return result;
        }

        public override Primitive Clone()
        {
            return new Capsule(this.transform.Position, this.transform.Orientation,
                this.radius, this.length);
        }

        public override Transform Transform
        {
            get{return this.transform;}
            set{this.transform = value;}
        }

        public Vector3 Position
        {
            get { return transform.Position; }
            set { transform.Position = value; }
        }

        public Vector3 GetEnd()
        {
            return transform.Position + length * transform.Orientation.Backward;
        }

        public Matrix Orientation
        {
            get { return transform.Orientation; }
            set { transform.Orientation = value; }
        }

        public float Length
        {
            get { return this.length; }
            set { this.length = value; }
        }

        public float Radius
        {
            get { return this.radius; }
            set { this.radius = value; }
        }

        public override float GetVolume()
        {
            return (4.0f / 3.0f) * MathHelper.Pi * radius * radius * radius + length * MathHelper.Pi * radius * radius;
        }

        public override float GetSurfaceArea()
        {
            return 4.0f * MathHelper.Pi * radius * radius + length * 2.0f * MathHelper.Pi * radius;
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

            centerOfMass = transform.Position + 0.5f * length * transform.Orientation.Backward;

            /// todo check solid/shell
            // first cylinder
            float cylinderMass = mass * MathHelper.Pi * radius * radius * length / GetVolume();
            float Ixx = 0.5f * cylinderMass * radius * radius;
            float Iyy = 0.25f * cylinderMass * radius * radius + (1.0f / 12.0f) * cylinderMass * length * length;
            float Izz = Iyy;
            // add ends
            float endMass = mass - cylinderMass;
            Ixx += 0.2f * endMass * radius * radius;
            Iyy += 0.4f * endMass * radius * radius + endMass * (0.5f * length) * (0.5f * length);
            Izz += 0.4f * endMass * radius * radius + endMass * (0.5f * length) * (0.5f * length);

            inertiaTensor = Matrix.Identity;
            inertiaTensor.M11 = Ixx;
            inertiaTensor.M22 = Iyy;
            inertiaTensor.M33 = Izz;

            // transform - e.g. see p664 of Physics-Based Animation
            // todo is the order correct here? Does it matter?

            // Calculate the tensor in a frame at the CoM, but aligned with the world axes
            inertiaTensor = transform.Orientation * inertiaTensor * Matrix.Transpose(transform.Orientation);

            // Transfer of axe theorem
            inertiaTensor.M11 = inertiaTensor.M11 + mass * (centerOfMass.Y * centerOfMass.Y + centerOfMass.Z * centerOfMass.Z);
            inertiaTensor.M22 = inertiaTensor.M22 + mass * (centerOfMass.Z * centerOfMass.Z + centerOfMass.X * centerOfMass.X);
            inertiaTensor.M33 = inertiaTensor.M33 + mass * (centerOfMass.X * centerOfMass.X + centerOfMass.Y * centerOfMass.Y);

            inertiaTensor.M12 = inertiaTensor.M21 = inertiaTensor.M12 - mass * centerOfMass.X * centerOfMass.Y;
            inertiaTensor.M23 = inertiaTensor.M32 = inertiaTensor.M23 - mass * centerOfMass.Y * centerOfMass.Z;
            inertiaTensor.M31 = inertiaTensor.M13 = inertiaTensor.M31 - mass * centerOfMass.Z * centerOfMass.X;
        }
    }
}
