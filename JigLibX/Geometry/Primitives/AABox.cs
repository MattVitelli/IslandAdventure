#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
using JigLibX.Geometry;
#endregion

namespace JigLibX.Geometry
{

    /// <summary>
    /// An axis aligned Box.
    /// </summary>
    public class AABox : Primitive
    {

        private Vector3 minPos = new Vector3(float.MaxValue);
        private Vector3 maxPos = new Vector3(float.MinValue);

        private static AABox hugeBox = new AABox(
            new Vector3(float.MinValue),new Vector3(float.MaxValue));

        /// <summary>
        /// Position based on one corner. sideLengths are the full side
        /// lengths (each element must be >= 0)
        /// </summary>
        /// <param name="minPos"></param>
        /// <param name="maxPos"></param>
        public AABox(Vector3 minPos, Vector3 maxPos) :
            base((int)PrimitiveType.AABox)
        {
            this.minPos = minPos;
            this.maxPos = maxPos;
        }

        public AABox() : 
            base((int)PrimitiveType.AABox)
        {
           this.Clear();
        }

        private Vector3 offset = Vector3.Zero;
        public override Transform Transform
        {
            get
            {
                return new Transform(offset, Matrix.Identity);
            }
            set
            {
                maxPos = maxPos - offset + value.Position;
                minPos = minPos - offset + value.Position;
                offset = value.Position;
            }
        }

        // adding points etc.
        public void Clear()
        {
            minPos.X = minPos.Y = minPos.Z = float.MaxValue;
            maxPos.X = maxPos.Y = maxPos.Z = float.MinValue;
        }

        /// <summary>
        /// Adds a point to the AABB.
        /// </summary>
        /// <remarks>
        /// This function is heavily used to calculate the axis
        /// aligned bounding boxes arround any object. Calling 
        /// by reference is unusal but makes it faster.
        /// </remarks> 
        /// <param name="pos"></param>
        public void AddPoint(ref Vector3 pos)
        {
            if (pos.X < minPos.X) minPos.X = pos.X - JiggleMath.Epsilon;
            if (pos.X > maxPos.X) maxPos.X = pos.X + JiggleMath.Epsilon;

            if (pos.Y < minPos.Y) minPos.Y = pos.Y - JiggleMath.Epsilon;
            if (pos.Y > maxPos.Y) maxPos.Y = pos.Y + JiggleMath.Epsilon;

            if (pos.Z < minPos.Z) minPos.Z = pos.Z - JiggleMath.Epsilon;
            if (pos.Z > maxPos.Z) maxPos.Z = pos.Z + JiggleMath.Epsilon;
        }

        public void AddPoint(Vector3 pos)
        {
            if (pos.X < minPos.X) minPos.X = pos.X - JiggleMath.Epsilon;
            if (pos.X > maxPos.X) maxPos.X = pos.X + JiggleMath.Epsilon;

            if (pos.Y < minPos.Y) minPos.Y = pos.Y - JiggleMath.Epsilon;
            if (pos.Y > maxPos.Y) maxPos.Y = pos.Y + JiggleMath.Epsilon;

            if (pos.Z < minPos.Z) minPos.Z = pos.Z - JiggleMath.Epsilon;
            if (pos.Z > maxPos.Z) maxPos.Z = pos.Z + JiggleMath.Epsilon;
        }
/*
        public void AddBox(Box box)
        {
            Vector3[] pts = new Vector3[8];
            box.GetCornerPoints(out pts);

            AddPoint(ref pts[0]);
            AddPoint(ref pts[1]);
            AddPoint(ref pts[2]);
            AddPoint(ref pts[3]);
            AddPoint(ref pts[4]);
            AddPoint(ref pts[5]);
            AddPoint(ref pts[6]);
            AddPoint(ref pts[7]);
        }

        public void AddSegment(Segment seg)
        {
            AddPoint(seg.Origin);
            AddPoint(seg.GetEnd());
        }

        public void AddAABox(AABox aabox)
        {
            AddPoint(aabox.MaxPos);
            AddPoint(aabox.MinPos);
        }

        public void AddSphere(Sphere sphere)
        {
            if ((sphere.Position.X - sphere.Radius) < minPos.X)
                minPos.X = (sphere.Position.X - sphere.Radius) - JiggleMath.Epsilon;
            if ((sphere.Position.X + sphere.Radius) > maxPos.X)
                maxPos.X = (sphere.Position.X + sphere.Radius) + JiggleMath.Epsilon;

            if ((sphere.Position.Y - sphere.Radius) < minPos.Y)
                minPos.Y = (sphere.Position.Y - sphere.Radius) - JiggleMath.Epsilon;
            if ((sphere.Position.Y + sphere.Radius) > maxPos.Y)
                maxPos.Y = (sphere.Position.Y + sphere.Radius) + JiggleMath.Epsilon;

            if ((sphere.Position.Z - sphere.Radius) < minPos.Z)
                minPos.Z = (sphere.Position.Z - sphere.Radius) - JiggleMath.Epsilon;
            if ((sphere.Position.Z + sphere.Radius) > maxPos.Z)
                maxPos.Z = (sphere.Position.Z + sphere.Radius) + JiggleMath.Epsilon;
        }

        public void AddCapsule(Capsule capsule)
        {
            AddSphere(new Sphere(capsule.Position, capsule.Radius));
            AddSphere(new Sphere(capsule.Position + capsule.Length * capsule.Orientation.Backward, capsule.Radius));
        }

        public void AddPrimitive(Primitive prim)
        {
            switch ((PrimitiveType)prim.Type)
            {
                case PrimitiveType.Box:
                    AddBox((Box)prim);
                    break;
                case PrimitiveType.Sphere:
                    AddSphere((Sphere)prim);
                    break;
                case PrimitiveType.Capsule:
                    AddCapsule((Capsule)prim);
                    break;
                default:
                    AddAABox(prim.GetBoundingBox());
                    break;
            }
        }
*/
        public void Move(Vector3 delta)
        {
            minPos += delta;
            maxPos += delta;
        }

        public bool IsPointInside(Vector3 pos)
        {
            return ((pos.X >= minPos.X) &&
                (pos.X <= maxPos.X) &&
                (pos.Y >= minPos.Y) &&
                (pos.Y <= maxPos.Y) &&
                (pos.Z >= minPos.Z) &&
                (pos.Z <= maxPos.Z));
        }

        public static bool OverlapTest(AABox box0, AABox box1)
        {
            return ((box0.minPos.Z >= box1.maxPos.Z) ||
                (box0.maxPos.Z <= box1.minPos.Z) ||
                (box0.minPos.Y >= box1.maxPos.Y) ||
                (box0.maxPos.Y <= box1.minPos.Y) ||
                (box0.minPos.X >= box1.maxPos.X) ||
                (box0.maxPos.X <= box1.minPos.X)) ? false : true;
        }

        public static bool OverlapTest(AABox box0, AABox box1, float tol)
        {
            return ((box0.minPos.Z >= box1.maxPos.Z + tol) ||
                (box0.maxPos.Z <= box1.minPos.Z - tol) ||
                (box0.minPos.Y >= box1.maxPos.Y + tol) ||
                (box0.maxPos.Y <= box1.minPos.Y - tol) ||
                (box0.minPos.X >= box1.maxPos.X + tol) ||
                (box0.maxPos.X <= box1.minPos.X - tol)) ? false : true;
        }

        public Vector3 GetCentre()
        {
            return 0.5f * (minPos + maxPos);
        }

        public Vector3 MinPos
        {
            get { return this.minPos; }
            set { this.minPos = value; }
        }

        public Vector3 MaxPos
        {
            get { return this.maxPos; }
            set { this.maxPos = value; }
        }

        public Vector3 GetSideLengths()
        {
            return maxPos - minPos;
        }

        public float GetRadiusAboutCentre()
        {
            return 0.5f * (maxPos - minPos).Length();
        }

        public float GetRadiusSqAboutCentre()
        {
            float result = this.GetRadiusAboutCentre();
            return result * result;
        }

        public static AABox HugeBox
        {
            get { return hugeBox; }
        }

        public override Primitive Clone()
        {
            return new AABox(this.minPos, this.maxPos);
        }

        public override bool SegmentIntersect(out float frac, out Vector3 pos, out Vector3 normal, Segment seg)
        {
            // todo implement
            // throw new JigLibXException("Not implemented!");
            throw new NotImplementedException();
        }

        public override float GetVolume()
        {
            return (this.maxPos - this.minPos).LengthSquared();
        }

        public override float GetSurfaceArea()
        {
            Vector3 sl = this.maxPos - this.minPos;
            return 2.0f * (sl.X * sl.Y + sl.X * sl.Z + sl.Y * sl.Z);
        }

        public override void GetMassProperties(PrimitiveProperties primitiveProperties, out float mass, out Vector3 centerOfMass, out Matrix inertiaTensor)
        {
            mass = 0.0f;
            centerOfMass = Vector3.Zero;
            inertiaTensor = Matrix.Identity;
        }
    }

    public class BoundingBoxHelper
    {
        static public BoundingBox InitialBox = new BoundingBox( new Vector3(float.PositiveInfinity),new Vector3(float.NegativeInfinity));
        static public void AddPoint(ref Vector3 pos, ref BoundingBox bb)
        {
            Vector3.Min(ref bb.Min, ref pos, out bb.Min);
            Vector3.Max(ref bb.Max, ref pos, out bb.Max);
        }
        static public void AddPoint(Vector3 pos, ref BoundingBox bb)
        {
            Vector3.Min(ref bb.Min, ref pos, out bb.Min);
            Vector3.Max(ref bb.Max, ref pos, out bb.Max);
        }

        static Vector3[] pts = new Vector3[8];

        static public void AddBox(Box box, ref BoundingBox bb)
        {
            // NOTE Not thread safe or rentrant as its uses pts
            box.GetCornerPoints(out pts);

            AddPoint(ref pts[0],ref bb);
            AddPoint(ref pts[1],ref bb);
            AddPoint(ref pts[2],ref bb);
            AddPoint(ref pts[3],ref bb);
            AddPoint(ref pts[4],ref bb);
            AddPoint(ref pts[5],ref bb);
            AddPoint(ref pts[6],ref bb);
            AddPoint(ref pts[7],ref bb);
        }
        static public void AddSegment(Segment seg, ref BoundingBox bb)
        {
            AddPoint(seg.Origin,ref bb);
            AddPoint(seg.GetEnd(), ref bb);
        }

        static public void AddAABox(AABox aabox, ref BoundingBox bb)
        {
            AddPoint(aabox.MaxPos, ref bb);
            AddPoint(aabox.MinPos, ref bb);
        }

        static public void AddSphere(Sphere sphere, ref BoundingBox bb)
        {
            Vector3 radius = new Vector3(sphere.Radius);
            Vector3 minSphere = sphere.Position;
            Vector3 maxSphere = sphere.Position;

            Vector3.Subtract(ref minSphere, ref radius, out minSphere);
            Vector3.Add(ref maxSphere, ref radius, out maxSphere);

            Vector3.Min(ref bb.Min, ref minSphere, out bb.Min);
            Vector3.Max(ref bb.Max, ref maxSphere, out bb.Max);
        }

        static public void AddSphere(Microsoft.Xna.Framework.BoundingSphere sphere, ref BoundingBox bb)
        {
            Vector3 radius = new Vector3(sphere.Radius);
            Vector3 minSphere = sphere.Center;
            Vector3 maxSphere = sphere.Center;

            Vector3.Subtract(ref minSphere, ref radius, out minSphere);
            Vector3.Add(ref maxSphere, ref radius, out maxSphere);

            Vector3.Min(ref bb.Min, ref minSphere, out bb.Min);
            Vector3.Max(ref bb.Max, ref maxSphere, out bb.Max);
        }

        static public void AddCapsule(Capsule capsule, ref BoundingBox bb)
        {
            AddSphere(new Microsoft.Xna.Framework.BoundingSphere(capsule.Position, capsule.Radius), ref bb);
            AddSphere(new Microsoft.Xna.Framework.BoundingSphere(capsule.Position + capsule.Length * capsule.Orientation.Backward, capsule.Radius), ref bb);
        }

        static public void AddPrimitive(Primitive prim, ref BoundingBox bb)
        {
            switch ((PrimitiveType)prim.Type)
            {
                case PrimitiveType.Box:
                    AddBox((Box)prim, ref bb);
                    break;
                case PrimitiveType.Sphere:
                    AddSphere((Sphere)prim, ref bb);
                    break;
                case PrimitiveType.Capsule:
                    AddCapsule((Capsule)prim, ref bb);
                    break;
                default:
                    AddAABox(prim.GetBoundingBox(), ref bb);
                    break;
            }
        }
        public static bool OverlapTest(ref BoundingBox box0, ref BoundingBox box1)
        {
            return ((box0.Min.Z >= box1.Max.Z) ||
                (box0.Max.Z <= box1.Min.Z) ||
                (box0.Min.Y >= box1.Max.Y) ||
                (box0.Max.Y <= box1.Min.Y) ||
                (box0.Min.X >= box1.Max.X) ||
                (box0.Max.X <= box1.Min.X)) ? false : true;
        }

        public static bool OverlapTest(ref BoundingBox box0, ref BoundingBox box1, float tol)
        {
            return ((box0.Min.Z >= box1.Max.Z + tol) ||
                (box0.Max.Z <= box1.Min.Z - tol) ||
                (box0.Min.Y >= box1.Max.Y + tol) ||
                (box0.Max.Y <= box1.Min.Y - tol) ||
                (box0.Min.X >= box1.Max.X + tol) ||
                (box0.Max.X <= box1.Min.X - tol)) ? false : true;
        }


    }
}
