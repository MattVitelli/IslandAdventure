#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
#endregion

namespace JigLibX.Geometry
{

    #region public struct Line
    /// <summary>
    /// A line goes through pos, and extends infinitely far in both
    /// directions along dir.
    /// </summary>
    public struct Line
    {
        public Vector3 Origin;
        public Vector3 Dir;

        public Line(Vector3 origin, Vector3 dir)
        {
            this.Origin = origin;
            this.Dir = dir;
        }

        public Vector3 GetOrigin(float t)
        {
            return new Vector3(
                Origin.X + t * Dir.X,
                Origin.Y + t * Dir.Y,
                Origin.Z + t * Dir.Z);
            //return this.Origin + t * this.Dir;
        }
    }
    #endregion

    #region public struct Ray
    /// <summary>
    /// A Ray is just a line that extends in the +ve direction
    /// </summary>
    public struct Ray
    {
        public Vector3 Origin;
        public Vector3 Dir;

        public Ray(Vector3 origin,Vector3 dir)
        {
            this.Origin = origin;
            this.Dir = dir;
        }

        public Vector3 GetOrigin(float t)
        {
            return new Vector3(
                Origin.X + t * Dir.X,
                Origin.Y + t * Dir.Y,
                Origin.Z + t * Dir.Z);

            //return this.Origin + t * this.Dir;
        }
    }
    #endregion

    #region public struct Segment
    /// <summary>
    /// A Segment is a line that starts at origin and goes only as far as
    /// (origin + delta).
    /// </summary>
    public struct Segment
    {
        public Vector3 Origin;
        public Vector3 Delta;

        public Segment(Vector3 origin, Vector3 delta)
        {
            this.Origin = origin;
            this.Delta = delta;
        }

        public void GetPoint(float t, out Vector3 point)
        {
            point = new Vector3(
                t * Delta.X,
                t * Delta.Y,
                t * Delta.Z);

            point.X += Origin.X;
            point.Y += Origin.Y;
            point.Z += Origin.Z;
        }

        public Vector3 GetPoint(float t)
        {
            Vector3 result = new Vector3(
                t * Delta.X,
                t * Delta.Y,
                t * Delta.Z);

            result.X += Origin.X;
            result.Y += Origin.Y;
            result.Z += Origin.Z;
            
            return result;
        }

        public Vector3 GetEnd()
        {
            return new Vector3(
                Delta.X + Origin.X,
                Delta.Y + Origin.Y,
                Delta.Z + Origin.Z);
            //return Origin + Delta;
        }

    }
    #endregion

}
