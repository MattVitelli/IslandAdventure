using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Gaia.Voxels
{
    public class TriangleGraph
    {
        public Vector3 Normal;
        
        public ulong ID;

        public Vector3 Centroid;

        float D;
        int DominantAxis;
        float abCrossDiff;
        float acCrossDiff;
        Vector2 abDiff;
        Vector2 acDiff;
        float invC;
        float invB;
        Vector3 p0, p1, p2;

        public Vector3 GetVertex0()
        {
            return p0;
        }

        public Vector3 GetVertex1()
        {
            return p1;
        }

        public Vector3 GetVertex2()
        {
            return p2;
        }

        void ComputeParameters()
        {
            Normal = Vector3.Normalize(Vector3.Cross(p2 - p0, p1 - p0));
            Centroid = (p0 + p1 + p2) / 3.0f;
            D = -Vector3.Dot(Normal, Centroid);

            Vector3 absNormal = new Vector3(Math.Abs(Normal.X), Math.Abs(Normal.Y), Math.Abs(Normal.Z));
            //if (absNormal.X > absNormal.Y && absNormal.X > absNormal.Z)
            DominantAxis = 0;
            
            if (absNormal.Y > absNormal.X && absNormal.Y > absNormal.Z)
                DominantAxis = 1;
            if (absNormal.Z > absNormal.X && absNormal.Z > absNormal.Y)
                DominantAxis = 2;

            switch (DominantAxis)
            {
                case 0:
                    p0.X = p0.Y;
                    p0.Y = p0.Z;
                    p1.X = p1.Y;
                    p1.Y = p1.Z;
                    p2.X = p2.Y;
                    p2.Y = p2.Z;
                    break;
                case 1:
                    p0.Y = p0.X;
                    p0.X = p0.Z;
                    p1.Y = p1.X;
                    p1.X = p1.Z;
                    p2.Y = p2.X;
                    p2.X = p2.Z;
                    break;
            }

            abCrossDiff = (p0.X * p1.Y) - (p0.Y * p1.X);
            acCrossDiff = (p0.X * p2.Y) - (p0.Y * p2.X);
            abDiff = new Vector2(p1.X - p0.X, p1.Y - p0.Y);
            acDiff = new Vector2(p2.X - p0.X, p2.Y - p0.Y);
            invC = 1.0f / ((abDiff.Y * p2.X) + (abDiff.X * p2.Y) + abCrossDiff);
            invB = 1.0f / ((acDiff.Y * p1.X) + (acDiff.X * p1.Y) + acCrossDiff);
        }

        public TriangleGraph(ushort index0, ushort index1, ushort index2, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            ID = (ulong)index0;
            ID += ((ulong)index1 << sizeof(ushort));
            ID += ((ulong)index2 << sizeof(ushort) * 2);
            this.p0 = p0;
            this.p1 = p1;
            this.p2 = p2;
            ComputeParameters();
        }

        public Vector3 GeneratePointInTriangle(Random randGen)
        {
            return Centroid;

            Vector3 weights = new Vector3((float)randGen.NextDouble(), (float)randGen.NextDouble(), (float)randGen.NextDouble());
            weights /= Vector3.Dot(weights, Vector3.One);
            
            return weights.X * p0 + weights.Y * p1 + weights.Z * p2;
        }

        public Vector3 GeneratePointInTriangle(Random randGen, Matrix transform)
        {
            Vector3 v0New = Vector3.Transform(p0, transform);
            Vector3 v1New = Vector3.Transform(p1, transform);
            Vector3 v2New = Vector3.Transform(p2, transform);

            v2New -= v0New;
            v1New -= v0New;
            Vector3 weights = new Vector3((float)randGen.NextDouble(), (float)randGen.NextDouble(), (float)randGen.NextDouble());
            weights /= Vector3.Dot(weights, Vector3.One);

            return weights.X * v0New + weights.Y * v1New + weights.Z * v2New;
        }

        public bool PointInTriangle(Vector3 pointInPlane)
        {
            /*
            const double eps = 0.1f;
            Vector3 vert0 = Vector3.Normalize(pointInPlane - p0);
            Vector3 vert1 = Vector3.Normalize(pointInPlane - p1);
            Vector3 vert2 = Vector3.Normalize(pointInPlane - p2);
            double angle = Math.Acos(Vector3.Dot(vert0, vert1)) +
                Math.Acos(Vector3.Dot(vert0, vert2)) + Math.Acos(Vector3.Dot(vert1, vert2));
            return (Math.Abs(angle - MathHelper.TwoPi) < eps);
            */

            switch (DominantAxis)
            {
                case 0:
                    pointInPlane.X = pointInPlane.Y;
                    pointInPlane.Y = pointInPlane.Z;
                    break;
                case 1:
                    pointInPlane.Y = pointInPlane.X;
                    pointInPlane.X = pointInPlane.Z;
                    break;
            }

            Vector3 barycenter = Vector3.Zero;
            barycenter.X = ((abDiff.Y * pointInPlane.X) + (abDiff.X * pointInPlane.Y) + abCrossDiff) * invC;
            if (barycenter.X > 1.0f || barycenter.X < 0.0f)
                return false;
            barycenter.Y = ((acDiff.Y * pointInPlane.X) + (acDiff.X * pointInPlane.Y) + acCrossDiff) * invB;
            if (barycenter.Y < 0.0f || barycenter.Y > 1.0f)
                return false;
            barycenter.Z = 1.0f - barycenter.X - barycenter.Y;
            return (barycenter.Z > 0.0f);
        }

        public bool Intersection(Vector3 pos, Vector3 dir, out float t)
        {
            t = 0;

            float denom = Vector3.Dot(Normal, dir);
            if (Math.Abs(denom) < 0.00001f)
                return false;
            t = -(Vector3.Dot(pos, Normal) + D) / denom;

            Vector3 pointInPlane = pos + dir * t;

            return PointInTriangle(pointInPlane);
        }
    }
}
