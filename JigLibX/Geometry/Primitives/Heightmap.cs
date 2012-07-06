#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
using JigLibX.Utils;
using JigLibX.Geometry;
#endregion

namespace JigLibX.Geometry
{
    /// <summary>
    /// Defines a heightmap that has up in the "y" direction 
    /// </summary>
    /// <remarks>
    /// indexs go from "bottom right" - i.e. (0, 0) -> (xmin, ymin)
    /// heights/normals are obtained by interpolation over triangles,
    /// with each quad being divided up in the same way - the diagonal
    /// going from (i, j) to (i+1, j+1)    
    /// </remarks>
    public class Heightmap : Primitive
    {
        private Array2D mHeights;

        private float x0, z0;
        private float dx, dz;
        private float xMin, zMin;
        private float xMax, zMax;
        private float yMax, yMin;

        public Vector3 Min { get { return new Vector3(xMin, yMin, zMin); } }
        public Vector3 Max { get { return new Vector3(xMax, yMax, zMax); } }

        /// <summary>
        /// pass in an array of heights, and the axis that represents up
        /// Also the centre of the heightmap (assuming z is up), and the grid size
        /// </summary>
        /// <param name="heights"></param>
        /// <param name="x0"></param>
        /// <param name="y0"></param>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        public Heightmap(Array2D heights, float x0, float z0, float dx, float dz)
            : base((int)PrimitiveType.Heightmap)
        {
            mHeights = heights;
            this.x0 = x0;
            this.z0 = z0;
            this.dx = dx;
            this.dz = dz;

            this.xMin = x0 - (mHeights.Nx - 1) * 0.5f * dx;
            this.zMin = z0 - (mHeights.Nz - 1) * 0.5f * dz;
            this.xMax = x0 + (mHeights.Nx - 1) * 0.5f * dx;
            this.zMax = z0 + (mHeights.Nz - 1) * 0.5f * dz;

            // Save this, so we don't need to recalc every time
            this.yMin = mHeights.Min;
            this.yMax = mHeights.Max;
        }

        /// <summary>
        /// Call this after changing heights. So the bounding
        /// box for collision detection gets recalculated.
        /// </summary>
        public void RecalculateBoundingBox()
        {
            this.xMin = x0 - (mHeights.Nx - 1) * 0.5f * dx;
            this.zMin = z0 - (mHeights.Nz - 1) * 0.5f * dz;
            this.xMax = x0 + (mHeights.Nx - 1) * 0.5f * dx;
            this.zMax = z0 + (mHeights.Nz - 1) * 0.5f * dz;
            this.yMin = mHeights.Min;
            this.yMax = mHeights.Max;
        }

        public override void GetBoundingBox(out AABox box)
        {
            box = new AABox(this.Min, this.Max);
        }

        /// <summary>
        /// Get the height at a particular index, indices are clamped
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public float GetHeight(int i, int j)
        {
            i = (int)MathHelper.Clamp(i, 0, mHeights.Nx - 1);
            j = (int)MathHelper.Clamp(j, 0, mHeights.Nz - 1);

            return mHeights[i, j];
        }

        /// <summary>
        /// Get the normal
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public Vector3 GetNormal(int i, int j)
        {
            int i0 = i - 1;
            int i1 = i + 1;
            int j0 = j - 1;
            int j1 = j + 1;
            i0 = (int)MathHelper.Clamp(i0, 0, (int)mHeights.Nx - 1);
            j0 = (int)MathHelper.Clamp(j0, 0, (int)mHeights.Nz - 1);
            i1 = (int)MathHelper.Clamp(i1, 0, (int)mHeights.Nx - 1);
            j1 = (int)MathHelper.Clamp(j1, 0, (int)mHeights.Nz - 1);

            float dx = (i1 - i0) * this.dx;
            float dz = (j1 - j0) * this.dz;

            if (i0 == i1) dx = 1.0f;
            if (j0 == j1) dz = 1.0f;

            if (i0 == i1 && j0 == j1) return Vector3.Up;

            float hFwd = mHeights[i1, j];
            float hBack = mHeights[i0, j];
            float hLeft = mHeights[i, j1];
            float hRight = mHeights[i, j0];

            Vector3 v1 = new Vector3(dx, hFwd - hBack,0.0f);
            Vector3 v2 = new Vector3(0.0f, hLeft - hRight,dz);

            #region REFERENCE: Vector3 normal = Vector3.Cross(v1,v2);
            Vector3 normal;
            Vector3.Cross(ref v1, ref v2, out normal);
            #endregion
            normal.Normalize();

            return normal;
        }

        /// <summary>
        /// get height and normal (quicker than calling both)
        /// </summary>
        /// <param name="h"></param>
        /// <param name="normal"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        public void GetHeightAndNormal(out float h, out Vector3 normal, int i, int j)
        {
            h = GetHeight(i, j);
            normal = GetNormal(i, j);
        }

        public void GetSurfacePos(out Vector3 pos, int i, int j)
        {
            float h = GetHeight(i, j);
            pos = new Vector3(xMin + i * dx, h, zMin + j * dz);
        }

        public void GetSurfacePosAndNormal(out Vector3 pos, out Vector3 normal, int i, int j)
        {
            float h = GetHeight(i, j);
            pos = new Vector3(xMin + i * dx, h, zMin + j * dz);
            normal = GetNormal(i, j);
        }

        public float GetHeight(Vector3 point)
        {
            // todo - optimise
            float h;
            Vector3 normal;
            GetHeightAndNormal(out h, out normal,point);
            return h;
        }

        public Vector3 GetNormal(Vector3 point)
        {
            // todo - optimise
            float h;
            Vector3 normal;
            GetHeightAndNormal(out h, out normal,point);
            return normal;
        }

        public void GetHeightAndNormal(out float h, out Vector3 normal,Vector3 point)
        {
            float x = point.X;
            float z = point.Z;

            x = MathHelper.Clamp(x, xMin, xMax);
            z = MathHelper.Clamp(z, zMin, zMax);

            int i0 = (int)((x - xMin) / dx);
            int j0 = (int)((point.Z - zMin) / dz);

            i0 = (int)MathHelper.Clamp((int)i0, 0, mHeights.Nx - 1);
            j0 = (int)MathHelper.Clamp((int)j0, 0, mHeights.Nz - 1);

            int i1 = i0 + 1;
            int j1 = j0 + 1;

            if (i1 >= (int)mHeights.Nx) i1 = mHeights.Nx - 1;
            if (j1 >= (int)mHeights.Nz) j1 = mHeights.Nz - 1;

            float iFrac = (x - (i0 * dx + xMin)) / dx;
            float jFrac = (z - (j0 * dz + zMin)) / dz;

            iFrac = MathHelper.Clamp(iFrac, 0.0f, 1.0f);
            jFrac = MathHelper.Clamp(jFrac, 0.0f, 1.0f);

            float h00 = mHeights[i0, j0];
            float h01 = mHeights[i0, j1];
            float h10 = mHeights[i1, j0];
            float h11 = mHeights[i1, j1];

            // All the triangles are orientated the same way.
            // work out the normal, then z is in the plane of this normal
            if ((i0 == i1) && (j0 == j1))
            {
                normal = Vector3.Up;
            }
            else if (i0 == i1)
            {
                Vector3 right = Vector3.Right;
                normal = Vector3.Cross(new Vector3(0.0f, h01 - h00, dz),right);
                normal.Normalize();
            }

            if (j0 == j1)
            {
                Vector3 backw = Vector3.Backward;
                normal = Vector3.Cross(backw, new Vector3(dx, h10 - h00, 0.0f));
                normal.Normalize();
            }
            else if (iFrac > jFrac)
            {
                normal = Vector3.Cross(new Vector3(dx, h11 - h00, dz), new Vector3(dx, h10 - h00, 0.0f));
                normal.Normalize();
            }
            else
            {
                normal = Vector3.Cross(new Vector3(0.0f, h01 - h00, dz), new Vector3(dx, h11 - h00, dz));
                normal.Normalize();
            }

             // get the plane equation
             // h00 is in all the triangles
             JiggleMath.NormalizeSafe(ref normal);
             Plane plane = new Plane(normal, new Vector3((i0 * dx + xMin), h00, (j0 * dz + zMin)));
            
             h = Distance.PointPlaneDistance(point,plane);
        }

        public void GetSurfacePos(out Vector3 pos, Vector3 point)
        {
            // todo - optimise
            float h = GetHeight(point);
            pos = new Vector3(point.X,h, point.Z);
        }

        public void GetSurfacePosAndNormal(out Vector3 pos, out Vector3 normal, Vector3 point)
        {
            float h;
            GetHeightAndNormal(out h, out normal,point);
            pos = new Vector3(point.X,h, point.Z);
        }

        public override Primitive Clone()
        {
            return new Heightmap(new Array2D(mHeights), x0, z0, dx, dz);
        }

        public override Transform Transform
        {
            get {return Transform.Identity;}
            set {}
        }

        public override bool SegmentIntersect(out float frac, out Vector3 pos, out Vector3 normal,Segment seg)
        {
            frac = 0;
            pos = Vector3.Zero;
            normal = Vector3.Up;

            //if (seg.Delta.Y > -JiggleMath.Epsilon )
            //    return false;

            Vector3 normalStart;
            float heightStart;

            GetHeightAndNormal(out heightStart, out normalStart,seg.Origin);

            if (heightStart < 0.0f)
                return false;

            Vector3 normalEnd;
            float heightEnd;
            Vector3 end = seg.GetEnd();
            GetHeightAndNormal(out heightEnd, out normalEnd,end);

            if (heightEnd > 0.0f)
                return false;

            // start is above, end is below...
            float depthEnd = -heightEnd;

            // normal is the weighted mean of these...
            float weightStart = 1.0f / (JiggleMath.Epsilon + heightStart);
            float weightEnd = 1.0f / (JiggleMath.Epsilon + depthEnd);

            normal = (normalStart * weightStart + normalEnd * weightEnd) /
              (weightStart + weightEnd);

            frac = heightStart / (heightStart + depthEnd + JiggleMath.Epsilon);

            pos = seg.GetPoint(frac);

            return true;
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

        public Array2D Heights
        {
            get
            {
                return mHeights;
            }
        }

    }
}
