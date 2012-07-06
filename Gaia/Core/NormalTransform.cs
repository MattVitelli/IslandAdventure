using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using JigLibX.Physics;

namespace Gaia.Core
{
    public class NormalTransform : Transform
    {
        Vector3 normal;
        bool normalConform;
 
        public NormalTransform()
        {
            normalConform = false;
        }

        public void ConformToNormal(Vector3 normal)
        {
            this.normal = Vector3.Normalize(normal);
            dirtyMatrix = true;
            normalConform = true;
        }

        protected override void UpdateMatrix()
        {
            base.UpdateMatrix();

            if (normalConform)
            {
                worldMatrix = Matrix.Identity;
                worldMatrix.Up = normal;
                worldMatrix.Right = new Vector3(normal.Y, normal.Z, normal.X);
                worldMatrix.Forward = new Vector3(normal.Z, normal.X, normal.Y);
                worldMatrix = Matrix.CreateScale(new Vector3(scale.X, 1, scale.Y)) * worldMatrix;
                worldMatrix.Translation = position;
            }
            
            objectMatrix = Matrix.Invert(worldMatrix);
            bounds.Min = Vector3.Transform(-Vector3.One, worldMatrix);
            bounds.Max = Vector3.Transform(Vector3.One, worldMatrix);
        }
    }
}
