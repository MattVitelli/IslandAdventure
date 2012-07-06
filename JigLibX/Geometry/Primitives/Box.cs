#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
using JigLibX.Utils;
#endregion

namespace JigLibX.Geometry
{

    public class Box : Primitive
    {

        #region public struct Edge
        /// <summary>
        /// Edge just contains indexes into the points returned by GetCornerPoints.
        /// </summary>
        public struct Edge
        {
            public BoxPointIndex Ind0;
            public BoxPointIndex Ind1;

            public Edge(BoxPointIndex ind0, BoxPointIndex ind1)
            {
                this.Ind0 = ind0;
                this.Ind1 = ind1;
            }
        }
        #endregion

        #region public enum BoxPointIndex
        /// <summary>
        /// Indices into the points returned by GetCornerPoints
        /// </summary>
        public enum BoxPointIndex
        {
            BRD, BRU, BLD, BLU,
            FRD, FRU, FLD, FLU
        }
        #endregion

        internal Vector3 sideLengths;

        // must match with GetCornerPoints!
        private static Edge[] edges = new Edge[12]
            { 
                new Edge(BoxPointIndex.BRD,BoxPointIndex.BRU), // origin-up
                new Edge(BoxPointIndex.BRD,BoxPointIndex.BLD), // origin-left
                new Edge(BoxPointIndex.BRD,BoxPointIndex.FRD), // origin-fwd
                new Edge(BoxPointIndex.BLD,BoxPointIndex.BLU), // leftorigin-up
                new Edge(BoxPointIndex.BLD,BoxPointIndex.FLD), // leftorigin-fwd
                new Edge(BoxPointIndex.FRD,BoxPointIndex.FRU), // fwdorigin-up
                new Edge(BoxPointIndex.FRD,BoxPointIndex.FLD), // fwdorigin-left
                new Edge(BoxPointIndex.BRU,BoxPointIndex.BLU), // uporigin-left
                new Edge(BoxPointIndex.BRU,BoxPointIndex.FRU), // uporigin-fwd
                new Edge(BoxPointIndex.BLU,BoxPointIndex.FLU), // upleftorigin-fwd
                new Edge(BoxPointIndex.FRU,BoxPointIndex.FLU), // upfwdorigin-left
                new Edge(BoxPointIndex.FLD,BoxPointIndex.FLU), // fwdleftorigin-up
            };
        private Vector3[] outPoints = new Vector3[8];


        /// <summary>
        /// Position/orientation are based on one corner the box. Sides are
        /// the full side lengths
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="orient"></param>
        /// <param name="sideLengths"></param>
        public Box(Vector3 pos, Matrix orient, Vector3 sideLengths) : base((int)PrimitiveType.Box)
        {
            this.transform = new Transform(pos, orient);
            this.sideLengths = sideLengths;
        }

        public override Primitive Clone()
        {
            return new Box(transform.Position, transform.Orientation, sideLengths);
        }

        /// <summary>
        /// Get/set the box corner/origin position
        /// </summary>
        public Vector3 Position
        {
            get { return transform.Position; }
            set { transform.Position = value; }
        }

        /// <summary>
        /// Get the box centre position
        /// </summary>
        /// <returns></returns>
        public Vector3 GetCentre()
        {
            Vector3 result = new Vector3(
                    sideLengths.X * 0.5f,
                    sideLengths.Y * 0.5f,
                    sideLengths.Z * 0.5f);

            Vector3.Transform(ref result, ref transform.Orientation, out result);
            Vector3.Add(ref result, ref transform.Position, out result);

            return result;
        }

        public void GetCentre(out Vector3 centre)
        {
            centre = new Vector3(
                sideLengths.X * 0.5f,
                sideLengths.Y * 0.5f,
                sideLengths.Z * 0.5f);

            Vector3.Transform(ref centre, ref transform.Orientation, out centre);
            Vector3.Add(ref centre, ref transform.Position, out centre);
        }

        /// <summary>
        /// Get bounding radius around the centre
        /// </summary>
        /// <returns></returns>
        public float GetBoundingRadiusAroundCentre()
        {
            return 0.5f * sideLengths.Length();
        }

        /// <summary>
        /// Get/Set the box orientation
        /// </summary>
        public Matrix Orientation
        {
            get { return transform.Orientation; }
            set { transform.Orientation = value; }
        }

        /// <summary>
        /// Get the three side lengths of the box
        /// </summary>
        /// <param name="sideLengths"></param>
        /// <returns></returns>
        public Vector3 SideLengths
        {
            get { return this.sideLengths; }
            set { this.sideLengths = value; }
        }

        /// <summary>
        /// Expands box by amount on each side (in both +ve and -ve directions)
        /// </summary>
        /// <param name="amount"></param>
        public void Expand(Vector3 amount)
        {
            transform.Position -= Vector3.Transform(amount, transform.Orientation);
            sideLengths += sideLengths + 2.0f * amount;
        }

        /// <summary>
        /// Returns the half-side lengths.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetHalfSideLengths()
        {
            Vector3 result = new Vector3(
                sideLengths.X * 0.5f,
                sideLengths.Y * 0.5f,
                sideLengths.Z * 0.5f);

            return result;
        }

        /// <summary>
        /// Returns the vector representing the edge direction 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Vector3 GetSide(int i)
        {
            return JiggleUnsafe.Get(transform.Orientation, i) * 
                JiggleUnsafe.Get(ref sideLengths, i);
        }

        /// <summary>
        /// Returns the squared distance 
        /// todo remove this/put it in distance fns
        /// </summary>
        /// <param name="closestBoxPoint"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public float GetSqDistanceToPoint(out Vector3 closestBoxPoint, Vector3 point)
        {
            closestBoxPoint = Vector3.Transform(point - transform.Position, Matrix.Transpose(transform.Orientation));

            float sqDistance = 0.0f;
            float delta;

            if (closestBoxPoint.X < 0.0f)
            {
                sqDistance += closestBoxPoint.X * closestBoxPoint.X;
                closestBoxPoint.X = 0.0f;
            }
            else if (closestBoxPoint.X > sideLengths.X)
            {
                delta = closestBoxPoint.X - sideLengths.X;
                sqDistance += delta * delta;
                closestBoxPoint.X = sideLengths.X;
            }

            if (closestBoxPoint.Y < 0.0f)
            {
                sqDistance += closestBoxPoint.Y * closestBoxPoint.Y;
                closestBoxPoint.Y = 0.0f;
            }
            else if (closestBoxPoint.Y > sideLengths.Y)
            {
                delta = closestBoxPoint.Y - sideLengths.Y;
                sqDistance += delta * delta;
                closestBoxPoint.Y = sideLengths.Y;
            }

            if (closestBoxPoint.Z < 0.0f)
            {
                sqDistance += closestBoxPoint.Z * closestBoxPoint.Z;
                closestBoxPoint.Z = 0.0f;
            }
            else if (closestBoxPoint.Z > sideLengths.Z)
            {
                delta = closestBoxPoint.Z - sideLengths.Z;
                sqDistance += delta * delta;
                closestBoxPoint.Z = sideLengths.Z;
            }

            Vector3.Transform(ref closestBoxPoint, ref transform.Orientation, out closestBoxPoint);
            Vector3.Add(ref transform.Position, ref closestBoxPoint, out closestBoxPoint);

            return sqDistance;
        }

        /// <summary>
        /// Returns the distance from the point to the box, (-ve if the
        /// point is inside the box), and optionally the closest point on
        /// the box.
        /// TODO make this actually return -ve if inside
        /// todo remove this/put it in distance fns
        /// </summary>
        /// <param name="closestBoxPoint"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public float GetDistanceToPoint(out Vector3 closestBoxPoint,
             Vector3 point)
        {
            return (float)System.Math.Sqrt(GetSqDistanceToPoint(out closestBoxPoint, point));
        }
        
        /// <summary>
        /// Gets the minimum and maximum extents of the box along the
        /// axis, relative to the centre of the box.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="axis"></param>
        public void GetSpan(out float min, out float max, Vector3 axis)
        {
            float s, u, d;
            Vector3 right = transform.Orientation.Right;
            Vector3 up = transform.Orientation.Up;
            Vector3 back = transform.Orientation.Backward;

            Vector3.Dot(ref axis, ref right, out s);
            Vector3.Dot(ref axis, ref up, out u);
            Vector3.Dot(ref axis, ref back, out d);

            s = System.Math.Abs(s * 0.5f * sideLengths.X);
            u = System.Math.Abs(u * 0.5f * sideLengths.Y);
            d = System.Math.Abs(d * 0.5f * sideLengths.Z);

            float r = s + u + d;
            GetCentre(out right);
            float p;
            Vector3.Dot(ref right,ref axis,out p);
            min = p - r;
            max = p + r;
        }
        
        /// <summary>
        /// Gets the corner points, populating pts
        /// </summary>
        /// <param name="pts"></param>
        public void GetCornerPoints(out Vector3[] pts)
        {
            pts = outPoints;
            pts[(int)BoxPointIndex.BRD] = transform.Position;
            pts[(int)BoxPointIndex.FRD] = transform.Position + sideLengths.X * transform.Orientation.Right;
            pts[(int)BoxPointIndex.BLD] = transform.Position + sideLengths.Y * transform.Orientation.Up;
            pts[(int)BoxPointIndex.BRU] = transform.Position + sideLengths.Z * transform.Orientation.Backward;
            pts[(int)BoxPointIndex.FLD] = pts[(int)BoxPointIndex.BLD] + sideLengths.X * transform.Orientation.Right;
            pts[(int)BoxPointIndex.BLU] = pts[(int)BoxPointIndex.BRU] + sideLengths.Y * transform.Orientation.Up;
            pts[(int)BoxPointIndex.FRU] = pts[(int)BoxPointIndex.FRD] + sideLengths.Z * transform.Orientation.Backward;
            pts[(int)BoxPointIndex.FLU] = pts[(int)BoxPointIndex.FLD] + sideLengths.Z * transform.Orientation.Backward;
        }

        /// <summary>
        /// Returns a (const) list of 12 edges - at the moment in this order:
        /// {BRD, BRU}, // origin-up
        /// {BRD, BLD}, // origin-left
        /// {BRD, FRD}, // origin-fwd
        /// {BLD, BLU}, // leftorigin-up
        /// {BLD, FLD}, // leftorigin-fwd
        /// {FRD, FRU}, // fwdorigin-up
        /// {FRD, FLD}, // fwdorigin-left
        /// {BRU, BLU}, // uporigin-left
        /// {BRU, FRU}, // uporigin-fwd
        /// {BLU, FLU}, // upleftorigin-fwd
        /// {FRU, FLU}, // upfwdorigin-left
        /// {FLD, FLU}, // fwdleftorigin-up
        /// </summary>
        /// <returns></returns>
        public void GetEdges(out Edge[] edg)
        {
            edg = edges;
        }

        /// <summary>
        /// EdgeIndices will contain indexes into the result of GetAllEdges
        /// </summary>
        /// <param name="edgeIndices"></param>
        /// <param name="pt"></param>
        public void GetEdgesAroundPoint(out int[] edgeIndices, BoxPointIndex pt)
        {
            edgeIndices = new int[3];
            int ind = 0;
            
            for (int i = 0; i < edges.Length; ++i)
            {
                if ((edges[i].Ind0 == pt) || (edges[i].Ind1 == pt))
                    edgeIndices[ind++] = i;
                if (ind == 3) return;
            }
        }

        public override float GetSurfaceArea()
        {
            return 2.0f * (sideLengths.X * sideLengths.Y + sideLengths.X * sideLengths.Z + sideLengths.Y * sideLengths.Z);
        }

        public override float GetVolume()
        {
            return sideLengths.X * sideLengths.Y * sideLengths.Z;
        }

        public override Transform Transform
        {
            get{return this.transform;}
            set{this.transform = value;}
        }

        public override bool SegmentIntersect(out float fracOut, out Vector3 posOut, out Vector3 normalOut, Segment seg)
        {
            fracOut = float.MaxValue;
            posOut = normalOut = Vector3.Zero;
            
            // algo taken from p674 of realting rendering
            // needs debugging
            float min = float.MinValue;
            float max = float.MaxValue;

            Vector3 p = GetCentre() - seg.Origin;
            Vector3 h;
            h.X = sideLengths.X * 0.5f;
            h.Y = sideLengths.Y * 0.5f;
            h.Z = sideLengths.Z * 0.5f;

            int dirMax = 0;
            int dirMin = 0;
            int dir = 0;

            Vector3[] matrixVec = new Vector3[3];
            matrixVec[0] = transform.Orientation.Right;
            matrixVec[1] = transform.Orientation.Up;
            matrixVec[2] = transform.Orientation.Backward;

            float[] vectorFloat = new float[3];
            vectorFloat[0] = h.X;
            vectorFloat[1] = h.Y;
            vectorFloat[2] = h.Z;
  
            for (dir = 0; dir < 3; dir++)
            {
                float e = Vector3.Dot(matrixVec[dir], p);
                float f = Vector3.Dot(matrixVec[dir], seg.Delta);

                if (System.Math.Abs(f) > JiggleMath.Epsilon)
                {
                    float t1 = (e + vectorFloat[dir]) / f;
                    float t2 = (e - vectorFloat[dir]) / f;

                    if (t1 > t2){float tmp = t1;t1 = t2; t2 = tmp;}

                    if (t1 > min)
                    {
                        min = t1;
                        dirMin = dir;
                    }
                    if (t2 < max)
                    {
                        max = t2;
                        dirMax = dir;
                    }

                    if (min > max)
                        return false;

                    if (max < 0.0f)
                        return false;
                }
                else if((-e-vectorFloat[dir] > 0.0f) ||
                    (-e + vectorFloat[dir] < 0.0f))
                {
                    return false;
                }
            }

            if (min > 0.0f)
            {
                dir = dirMin;
                fracOut = min;
            }
            else
            {
                dir = dirMax;
                fracOut = max;
            }

            fracOut = MathHelper.Clamp(fracOut, 0.0f, 1.0f);
            posOut = seg.GetPoint(fracOut);
            if (Vector3.Dot(matrixVec[dir], seg.Delta) > 0.0f)
                normalOut = -matrixVec[dir];
            else
                normalOut = matrixVec[dir];

            return true;
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

            centerOfMass = GetCentre();

            // First calculate inertia in local frame, then shift to origin
            Vector3 sides = sideLengths;
            // todo check solid/shell
            float Ixx = (1.0f / 12.0f) * mass * (sides.Y * sides.Y + sides.Z * sides.Z);
            float Iyy = (1.0f / 12.0f) * mass * (sides.X * sides.X + sides.Z * sides.Z);
            float Izz = (1.0f / 12.0f) * mass * (sides.X * sides.X + sides.Y * sides.Y);

            inertiaTensor = Matrix.Identity;
            inertiaTensor.M11 = Ixx;
            inertiaTensor.M22 = Iyy;
            inertiaTensor.M33 = Izz;

            // transform - e.g. see p664 of Physics-Based Animation
            // todo is the order correct here? Does it matter?

            // Calculate the tensor in a frame at the CoM, but aligned with the world axes
            inertiaTensor = Matrix.Transpose(transform.Orientation) * inertiaTensor  * transform.Orientation;

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
