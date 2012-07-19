using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Rendering;
namespace Gaia.Core
{
    public static class MathUtils
    {
        public static void ComputeTangent(ref VertexPNTTB srcVert, VertexPNTTB vert1, VertexPNTTB vert2)
        {
            Vector4 d0 = vert1.Position - srcVert.Position;
            Vector4 d1 = vert2.Position - srcVert.Position;

            Vector2 s = vert1.Texcoord - srcVert.Texcoord;
            Vector2 t = vert2.Texcoord - srcVert.Texcoord;

            float r = 1.0F / (t.X * s.Y - t.Y * s.X);
            Vector3 tangent = new Vector3(s.Y * d1.X - t.Y * d0.X, s.Y * d1.Y - t.Y * d0.Y, s.Y * d1.Z - t.Y * d0.Z) * r;
            srcVert.Tangent = Vector3.Normalize(tangent - srcVert.Normal * Vector3.Dot(srcVert.Normal, tangent));
        }

        public static BoundingBox TransformBounds(BoundingBox srcBounds, Matrix transform)
        {
            srcBounds.Min = Vector3.Transform(srcBounds.Min, transform);
            srcBounds.Max = Vector3.Transform(srcBounds.Max, transform);
            return srcBounds;
        }

        public static Matrix Invert3x3(Matrix matrix)
        {
            Matrix destMat = Matrix.Identity;

            destMat.M11 = (matrix.M22 * matrix.M33 - matrix.M32 * matrix.M23);
            destMat.M21 = (matrix.M31 * matrix.M23 - matrix.M21 * matrix.M33);
            destMat.M31 = (matrix.M21 * matrix.M32 - matrix.M31 * matrix.M22);
            destMat.M12 = (matrix.M32 * matrix.M13 - matrix.M12 * matrix.M33);
            destMat.M22 = (matrix.M11 * matrix.M33 - matrix.M31 * matrix.M13);
            destMat.M32 = (matrix.M31 * matrix.M12 - matrix.M11 * matrix.M32);
            destMat.M13 = (matrix.M12 * matrix.M23 - matrix.M22 * matrix.M13);
            destMat.M23 = (matrix.M13 * matrix.M21 - matrix.M11 * matrix.M23);
            destMat.M33 = (matrix.M11 * matrix.M22 - matrix.M21 * matrix.M12);
            double invDet = 1.0 / (matrix.M11 * destMat.M11 + matrix.M21 * destMat.M12 + matrix.M31 * destMat.M13);

            destMat.M11 = (float)(destMat.M11 * invDet);
            destMat.M12 = (float)(destMat.M12 * invDet);
            destMat.M13 = (float)(destMat.M13 * invDet);
            destMat.M21 = (float)(destMat.M21 * invDet);
            destMat.M22 = (float)(destMat.M22 * invDet);
            destMat.M23 = (float)(destMat.M23 * invDet);
            destMat.M31 = (float)(destMat.M31 * invDet);
            destMat.M32 = (float)(destMat.M32 * invDet);
            destMat.M33 = (float)(destMat.M33 * invDet);

            return destMat;
        }
    }
}
