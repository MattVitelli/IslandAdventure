#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using JigLibX.Math;
using Microsoft.Xna.Framework;
using System.Collections;
#endregion

namespace JigLibX.Utils
{
    /// <summary>
    /// Defines a 2D Array
    /// </summary>
    public class Array2D
    {
        public delegate float Function(int x, int z, Array2D arrInstance);

        private int nx, nz;
        private bool wrap;

        public float[] Array;

        public Array2D(Array2D arr)
        {
            if (arr == null || arr.Array == null)
                return;

            this.Array = new float[arr.Array.Length];
            this.nx = arr.Nx;
            this.nz = arr.Nz;

            Buffer.BlockCopy(arr.Array, 0, this.Array, 0, this.Array.Length*4);
        }

        public Array2D(int nx, int nz)
        {
            Array = new float[nx * nz];
            this.nx = nx;
            this.nz = nz;
        }

        public Array2D(int nx, int nz, float val)
        {
            Array = new float[nx * nz];
            this.nx = nx;
            this.nz = nz;

            for (int i = 0; i < Array.Length; i++)
                Array[i] = val;

        }

        /// <summary>
        /// Creates 2D array from mathematical function
        /// </summary>
        /// <param name="nx"></param>
        /// <param name="ny"></param>
        /// <param name="yScale"></param>
        /// <returns></returns>
        public static Array2D CreateArray(int nx, int nz, Function func)
        {
            Array2D arr = new Array2D(nx, nz);

            if(func == null)
                return arr;

            for (int xx = 0; xx < nx; xx++)
            {
                for (int zz = 0; zz < nz; zz++)
                {
                    arr.Array[xx + zz * nx] = func(xx, zz, arr);
                }

            }

            return arr;

        }

        /// <summary>
        /// allows resizing. Data will be lost if resizing occurred
        /// </summary>
        /// <param name="nx"></param>
        /// <param name="ny"></param>
        public void Resize(int nx, int nz)
        {
            if (nx == this.nx && nz == this.nz) 
                return;

            Array = new float[nx * nz]; 
            
            this.nx = nx; 
            this.nz = nz;
        }

        /// <summary>
        /// raise array elements to a power
        /// </summary>
        /// <param name="rhs"></param>
        /// <returns></returns>
        public Array2D Pow(float rhs)
        {
            for (int i = 0; i < this.nx * this.nz; ++i)
            {
                Array[i] = (float)System.Math.Pow(Array[i], rhs);
            }

            return this;
        }

        /// <summary>
        /// sets each value to its absolute value by comparison with float(0)
        /// </summary>
        public void Abs()
        {
            for (int i = 0; i < Array.Length; ++i)
            {
                if (Array[i] < 0)
                    Array[i] = -Array[i];
            }
        }

        /// <summary>
        /// Apply a Gaussian filter with length scale r, extending over a
        /// square of half-width n (so n=1 uses a square of 9 points, n = 2
        /// uses 25 etc). Suggest using n at least 2*r.
        /// </summary>
        /// <param name="r">length scale</param>
        /// <param name="n">half-width</param>
        public void GaussianFilter(float r, int n)
        {
            int i, j, ii, jj, iii, jjj;

            int size = (n * 2 + 1);
            float[] filter = new float[size * size];

            for (i = 0; i < size; ++i)
            {
                for (j = 0; j < size; ++j)
                    filter[i + j * size] = (float)System.Math.Exp(-((i - n) * (i - n) + (j - n) * (j - n)) / (r * r));
            }

            for (i = 0; i < (int)this.nx; ++i)
            {
                for (j = 0; j < (int)this.nz; ++j)
                {
                    float total = 0;
                    float weight_total = 0;

                    for (ii = -n; ii < (int)n; ++ii)
                    {
                        if ((((iii = i + ii) >= 0) && (iii < this.nx)) || (wrap))
                        {
                            for (jj = -n; jj < (int)n; ++jj)
                            {
                                if ((((jjj = j + jj) >= 0) && (jjj < this.nz)) || (wrap))
                                {
                                    // in a valid location
                                    int index = (n + ii) + (n + jj) * size;

                                    weight_total += filter[index];
                                    total += filter[index] * GetAt(iii, jjj);
                                }
                            }
                        }
                    }

                    SetAt(i, j, total / weight_total);
                }
            }
        }

        /// <summary>
        /// shifts all the elements...
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        public void Shift(int offsetX, int offsetZ)
        {
            Array2D orig = new Array2D(this);

            for (int i = 0; i < this.nx; ++i)
            {
                for (int j = 0; j < this.nz; ++j)
                {
                    int i0 = (i + offsetX) % this.nx;
                    int j0 = (j + offsetZ) % this.nz;

                    this.SetAt(i0, j0, orig.GetAt(i, j));
                }
            }

        }

        /// <summary>
        /// scale to fit within range...
        /// </summary>
        /// <param name="valMin"></param>
        /// <param name="valMax"></param>
        public void SetRange(float valMin, float valMax)
        {
            int i;
            float origMin = this.Min;
            float origMax = this.Max;

            // set min to 0 and scale...
            float scale = (valMax - valMin) / (origMax - origMin);
            float offset = valMin - scale * origMin;

            for (i = 0; i < Array.Length; ++i)
            {
                Array[i] = scale * Array[i] + offset;
            }

        }

        /// <summary>
        /// Set to a constant value
        /// </summary>
        /// <param name="val"></param>
        public void SetTo(float val)
        {
            for (int i = 0; i < Array.Length; ++i)
            {
                Array[i] = val;
            }
        }

        /// <summary>
        /// interpolate
        /// </summary>
        /// <param name="fi"></param>
        /// <param name="fj"></param>
        /// <returns></returns>
        public float Interpolate(float fi, float fj)
        {
            fi = MathHelper.Clamp(fi, 0.0f, (this.nx - 1.0f));
            fj = MathHelper.Clamp(fj, 0.0f, (this.nz - 1.0f));

            int i0 = (int)(fi);
            int j0 = (int)(fj);
            int i1 = i0 + 1;
            int j1 = j0 + 1;

            if (i1 >= this.nx) i1 = this.nx - 1;
            if (j1 >= this.nz) j1 = this.nz - 1;

            float iFrac = fi - i0;
            float jFrac = fj - j0;

            float result = jFrac * (iFrac * this[i1, j1] + (1.0f - iFrac) * this[i0, j1]) +
              (1.0f - jFrac) * (iFrac * this[i1, j0] + (1.0f - iFrac) * this[i0, j0]);

            return result;
        }

        /// <summary>
        /// checked access - unwraps if wrapping set
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public float GetAt(int i, int j)
        {
            UnwrapIndices(ref i, ref j);

            return Array[i + j * this.nx];
        }

        /// <summary>
        /// checked access - unwraps if wrapping set
        /// </summary>
        public void SetAt(int i, int j, float val)
        {
            UnwrapIndices(ref i, ref j);

            Array[i + j * this.nx] = val;
        }

        private void UnwrapIndices(ref int i, ref int j)
        {
            if (wrap == false)
                return;

            while (i < 0)
                i += this.nx;

            while (j < 0)
                j += this.nz;

            i = i % this.nx;
            j = j % this.nz;
        }

        /// <summary>
        /// ! Unchecked access - no wrapping
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public float this[int i, int j]
        {
            get
            {
                return Array[i + j * this.nx];
            }
        }


        /// <summary>
        /// enables/disables wrapping
        /// </summary>
        public bool Wrap
        {
            get { return wrap; }
            set { wrap = value; }
        }

        /// <summary>
        /// Gets the 'x' size of the array
        /// </summary>
        public int Nx
        {
            get { return this.nx; }
        }

        /// <summary>
        /// Gets the 'y' size of the array
        /// </summary>
        public int Nz
        {
            get { return this.nz; }
        }

        public float Min
        {
            get
            {
                float min = Array[0];

                for (int i = 0; i < Array.Length; ++i)
                {
                    if (Array[i] < min)
                        min = Array[i];
                }
                return min;
            }
        }

        public float Max
        {
            get
            {

                float max = Array[0];

                for (int i = 0; i < Array.Length; ++i)
                {
                    if (max < Array[i])
                        max = Array[i];
                }
                return max;
            }
        }


    }
}
