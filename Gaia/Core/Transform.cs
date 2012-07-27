using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using JigLibX.Physics;

namespace Gaia.Core
{
    public class Transform
    {
        protected Vector3 position;
        protected Vector3 rotation;
        protected Vector3 scale;

        protected bool dirtyMatrix = true;
        protected Matrix worldMatrix;
        protected Matrix objectMatrix;
        protected BoundingBox bounds;
        Body body;

        public Transform()
        {
            position = Vector3.Zero;
            rotation = Vector3.Zero;
            scale = Vector3.One;
            worldMatrix = Matrix.Identity;
            objectMatrix = Matrix.Identity;
            dirtyMatrix = true;
        }

        public Transform(Body body)
        {
            this.body = body;
            dirtyMatrix = true;
        }

        public void SetPosition(Vector3 position)
        {
            this.position = position;
            if(body != null)
                body.MoveTo(position, Matrix.Identity);
            dirtyMatrix = true;
        }

        public Vector3 GetPosition()
        {
            return (body != null)?body.Position:position;
        }

        public void SetRotation(Vector3 rotation)
        {
            this.rotation = rotation;
            dirtyMatrix = true;
        }

        public Vector3 GetRotation()
        {
            return rotation;
        }

        public void SetScale(Vector3 scale)
        {
            this.scale = scale;
            dirtyMatrix = true;
        }

        public Vector3 GetScale()
        {
            return scale;
        }

        public Matrix GetTransform()
        {
            if (dirtyMatrix)
                UpdateMatrix();
            return worldMatrix;
        }

        public Matrix GetObjectSpace()
        {
            if (dirtyMatrix)
                UpdateMatrix();
            return objectMatrix;
        }

        public BoundingBox GetBounds()
        {
            return bounds;
        }

        public virtual BoundingBox TransformBounds(BoundingBox inBounds)
        {
            Matrix transform = GetTransform();
            Vector3 min = Vector3.Transform(inBounds.Min, transform);
            Vector3 max = Vector3.Transform(inBounds.Max, transform);
            return new BoundingBox(Vector3.Min(min, max), Vector3.Max(min, max));
        }

        protected virtual void UpdateMatrix()
        {
            if (body == null)
            {
                worldMatrix = Matrix.CreateScale(scale) * Matrix.CreateRotationX(rotation.X) * Matrix.CreateRotationY(rotation.Y) * Matrix.CreateRotationZ(rotation.Z);
                //worldMatrix = MathUtils.CreateWorld(scale, rotation, position);
                //worldMatrix = Matrix.CreateScale(scale) * Matrix.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
                worldMatrix.Translation = position;
                dirtyMatrix = false;
            }
            else
            {
                worldMatrix = JigLibXConverter.ToXNA(body, this);
                dirtyMatrix = body.IsActive;
                position = body.Position;
            }
            
            objectMatrix = Matrix.Invert(worldMatrix);
            bounds.Min = Vector3.Transform(-Vector3.One, worldMatrix);
            bounds.Max = Vector3.Transform(Vector3.One, worldMatrix);
        }
    }
}
