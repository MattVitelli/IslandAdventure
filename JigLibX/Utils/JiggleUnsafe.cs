#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
#endregion

namespace JigLibX.Utils
{
    public sealed class JiggleUnsafe
    {

        public static unsafe float Get(ref Vector3 vec, int index)
        {
            fixed (Vector3* adr = &vec)
            {
                return ((float*)adr)[index];
            }
        }

        public static unsafe float Get(Vector3 vec, int index)
        {
            Vector3* adr = &vec;
            return ((float*)adr)[index];
        }

        public static unsafe Vector3 Get(Matrix mat, int index)
        {
            float* adr = &mat.M11;
            adr += index;
            return ((Vector3*)adr)[index];
        }

        public static unsafe Vector3 Get(ref Matrix mat, int index)
        {
            fixed (float* adr = &mat.M11)
            {
                return ((Vector3*)(adr+index))[index];
            }
        }

        public static unsafe void Get(ref Matrix mat, int index, out Vector3 vec)
        {
            fixed (float* adr = &mat.M11)
            {
                vec = ((Vector3*)(adr + index))[index];
            }
        }
    }
}
