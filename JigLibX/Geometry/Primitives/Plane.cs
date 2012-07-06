#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
#endregion

namespace JigLibX.Geometry
{
    public class Plane : Primitive
    {

        internal Vector3 normal = Vector3.Zero;
        private float d = 0.0f;

        public Plane()
            : base((int)PrimitiveType.Plane)
        {
        }

        public Plane(Vector3 n, float d)
            : base((int)PrimitiveType.Plane)
        {
            JiggleMath.NormalizeSafe(ref n);
            this.normal = n;
            this.d = d;
        }

        public Plane(Vector3 n, Vector3 pos)
            : base((int)PrimitiveType.Plane)
        {
            JiggleMath.NormalizeSafe(ref n);
            this.normal = n;
            this.d = -Vector3.Dot(n, pos);
        }

        public Plane(Vector3 pos0,Vector3 pos1,Vector3 pos2)
            : base((int)PrimitiveType.Plane)
        {
            Vector3 dr1 = pos1 - pos0;
            Vector3 dr2 = pos2 - pos0;

            this.normal = Vector3.Cross(dr1, dr2);
            float mNLen = normal.Length();
            if (mNLen < JiggleMath.Epsilon)
            {
                this.normal = Vector3.Up;
                this.d = 0.0f;
            }
            else
            {
                this.normal /= mNLen;
                this.d = -Vector3.Dot(this.normal, pos0);
            }
        }

        public Vector3 Normal
        {
            get { return this.normal; }
            set { this.normal = value; }
        }

        public float D
        {
            get { return this.d; }
            set { this.d = value; }
        }

        public override Primitive Clone()
        {
            Plane newPlane = new Plane(this.Normal, this.D);
            newPlane.Transform = Transform;
            return newPlane;
        }
        private Matrix transformMatrix;
        private Matrix invTransform;
        public override Transform Transform
        {
            get
            {
                return base.Transform;
            }
            set
            {
                base.Transform = value;
                transformMatrix = transform.Orientation;
                transformMatrix.Translation = transform.Position;
                invTransform = Matrix.Invert(transformMatrix);
            }
        }

        // use a cached version 
        public override Matrix TransformMatrix
        {
            get
            {
                return transformMatrix;
            }
        }
        // use a cached version 
        public override Matrix InverseTransformMatrix
        {
            get
            {
                return invTransform;
            }
        }
        public override bool SegmentIntersect(out float frac, out Vector3 pos, out Vector3 normal, Segment seg)
        {
            bool result;
            if (result = Intersection.SegmentPlaneIntersection(out frac, seg, this))
            {
                pos = seg.GetPoint(frac);
                normal = this.Normal;
            }
            else
            {
                pos = Vector3.Zero;
                normal = Vector3.Zero;
            }

            return result;
        }

        public override float GetVolume()
        {
            return 0.0f;
        }

        public override float GetSurfaceArea()
        {
            return 0.0f;
        }

        public override void GetMassProperties(PrimitiveProperties primitiveProperties, out float mass, out Vector3 centerOfMass, out Matrix inertiaTensor)
        {
            mass = 0.0f;
            centerOfMass = Vector3.Zero;
            inertiaTensor = Matrix.Identity;
        }

        public void Invert()
        {
            Vector3.Negate(ref normal, out normal);
        }

        public Plane GetInverse()
        {
            Plane plane = new Plane(this.normal, this.d);
            plane.Invert();
            return plane;
        }

    }
}
