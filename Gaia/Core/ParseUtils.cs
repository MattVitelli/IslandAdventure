using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Gaia.Core
{
    public static class ParseUtils
    {
        public static Vector2 ParseVector2(string text)
        {
            string[] splits = text.Split(' ');
            float x = 0, y = 0;
            float.TryParse(splits[0], out x);
            if (splits.Length > 1)
                float.TryParse(splits[1], out y);
            return new Vector2(x, y);
        }

        public static Vector3 ParseVector3(string text)
        {
            string[] splits = text.Split(' ');
            float x = 0, y = 0, z = 0;
            float.TryParse(splits[0], out x);
            if (splits.Length > 1)
                float.TryParse(splits[1], out y);
            if (splits.Length > 2)
                float.TryParse(splits[2], out z);
            return new Vector3(x, y, z);
        }

        public static Vector4 ParseVector4(string text)
        {
            string[] splits = text.Split(' ');
            float x = 0, y = 0, z = 0, w = 0;
            float.TryParse(splits[0], out x);
            if (splits.Length > 1)
                float.TryParse(splits[1], out y);
            if (splits.Length > 2)
                float.TryParse(splits[2], out z);
            if (splits.Length > 3)
                float.TryParse(splits[3], out w);
            return new Vector4(x, y, z, w);
        }

        public static string WriteVector2(Vector2 vector)
        {
            return vector.X + " " + vector.Y;
        }

        public static string WriteVector3(Vector3 vector)
        {
            return vector.X + " " + vector.Y + " " + vector.Z;
        }

        public static string WriteVector4(Vector4 vector)
        {
            return vector.X + " " + vector.Y + " " + vector.Z + " " + vector.W;
        }
    }
}
