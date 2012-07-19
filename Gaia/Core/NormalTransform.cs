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
        float theta = MathHelper.PiOver4;
        bool useForwardVector = false;
        Vector3 forwardVec;
 
        public NormalTransform()
        {
            normalConform = false;
        }

        public void SetForwardVector(Vector3 forward)
        {
            useForwardVector = true;
            forwardVec = forward;
        }

        public void ConformToNormal(Vector3 normal)
        {
            this.normal = Vector3.Normalize(normal);
            dirtyMatrix = true;
            normalConform = true;
        }

        public void SetAngle(float angle)
        {
            theta = angle;
            dirtyMatrix = true;
        }

        public float GetAngle()
        {
            return theta;
        }

        protected override void UpdateMatrix()
        {
            base.UpdateMatrix();

            if (normalConform)
            {
                worldMatrix = Matrix.Identity;
                worldMatrix.Up = normal;

                Vector3 fwd = (useForwardVector) ? forwardVec : new Vector3(normal.Z, normal.X, normal.Y);
                fwd = Vector3.Normalize(fwd - Vector3.Dot(fwd, normal) * normal);
                Vector3 right = Vector3.Cross(fwd, normal);

                worldMatrix.Right = right;// Vector3.Normalize(new Vector3(normal.Z, normal.X, normal.Y));
                worldMatrix.Forward = fwd;// Vector3.Normalize(Vector3.Cross(worldMatrix.Up, worldMatrix.Right));
                worldMatrix = Matrix.CreateScale(scale) * Matrix.CreateRotationY(theta) * worldMatrix;
                worldMatrix.Translation = position;
            }
            
            objectMatrix = Matrix.Invert(worldMatrix);
            bounds.Min = Vector3.Transform(-Vector3.One, worldMatrix);
            bounds.Max = Vector3.Transform(Vector3.One, worldMatrix);
        }
    }
}
