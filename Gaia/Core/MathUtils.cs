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
    }
}
