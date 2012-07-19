using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using JigLibX.Physics;

namespace Gaia.Core
{
    public static class JigLibXConverter
    {
        public static Matrix ToXNA(Body body)
        {
            Matrix mat = body.Orientation;
            mat.Translation = body.Position;
            return mat;
        }

        public static Matrix ToXNA(Body body, Transform transform)
        {
            Matrix mat = body.Orientation * Matrix.CreateRotationY(transform.GetRotation().Y);
            mat.Translation = body.Position;
            return mat;
        }
    }
}
