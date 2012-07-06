#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
#endregion

namespace JigLibX.Math
{

    #region public struct Transform

    /// <summary>
    /// Transform is unneeded and should be removed soon. The XNA matrix4x4 can store
    /// the orientation and position.
    /// </summary>
    public struct Transform
    {

        public Vector3 Position;
        public Matrix Orientation;

        public static Transform Identity
        {
            get { return new Transform(Vector3.Zero, Matrix.Identity); }
        }

        public Transform(Vector3 position, Matrix orientation)
        {
            this.Position = position;
            this.Orientation = orientation;
        }

        public void ApplyTransformRate(ref TransformRate rate, float dt)
        {
            //Position += dt * rate.Velocity;
            Vector3 pos;
            Vector3.Multiply(ref rate.Velocity, dt, out pos);
            Vector3.Add(ref Position, ref pos, out Position);

            Vector3 dir = rate.AngularVelocity;
            float ang = dir.Length();

            if (ang > 0.0f)
            {
                Vector3.Divide(ref dir, ang, out dir);  // dir /= ang;
                ang *= dt;
                Matrix rot;
                Matrix.CreateFromAxisAngle(ref dir, ang, out rot);
                Matrix.Multiply(ref Orientation, ref rot, out Orientation);
            }

            //JiggleMath.Orthonormalise(ref this.Orientation);
        }

        public void ApplyTransformRate(TransformRate rate, float dt)
        {
            //Position += dt * rate.Velocity;
            Vector3 pos;
            Vector3.Multiply(ref rate.Velocity, dt, out pos);
            Vector3.Add(ref Position, ref pos, out Position);

            Vector3 dir = rate.AngularVelocity;
            float ang = dir.Length();

            if (ang > 0.0f)
            {
                Vector3.Divide(ref dir, ang, out dir);  // dir /= ang;
                ang *= dt;
                Matrix rot;
                Matrix.CreateFromAxisAngle(ref dir, ang, out rot);
                Matrix.Multiply(ref Orientation, ref rot, out Orientation);
            }

          //  JiggleMath.Orthonormalise(ref this.Orientation);
        }


        public static Transform operator *(Transform lhs, Transform rhs)
        {
            Transform result;
            Transform.Multiply(ref lhs, ref rhs, out result);
            return result;
        }

        public static Transform Multiply(Transform lhs, Transform rhs)
        {
            Transform result = new Transform();
            Matrix.Multiply(ref rhs.Orientation, ref lhs.Orientation, out result.Orientation);
            //result.Orientation = rhs.Orientation * lhs.Orientation;
            Vector3.Transform(ref rhs.Position, ref lhs.Orientation, out result.Position);
            Vector3.Add(ref lhs.Position, ref result.Position, out result.Position);
            //result.Position = lhs.Position + Vector3.Transform(rhs.Position, lhs.Orientation);

            return result;
        }

        public static void Multiply(ref Transform lhs, ref Transform rhs, out Transform result)
        {
            result = new Transform();

            Matrix.Multiply(ref rhs.Orientation, ref lhs.Orientation, out result.Orientation);
            //result.Orientation = rhs.Orientation * lhs.Orientation;
            Vector3.Transform(ref rhs.Position, ref lhs.Orientation, out result.Position);
            Vector3.Add(ref lhs.Position, ref result.Position, out result.Position);
            //result.Position = lhs.Position + Vector3.Transform(rhs.Position, lhs.Orientation);
        }

    }

    #endregion

    #region public struct TransformRate

    public struct TransformRate
    {

        public Vector3 Velocity;
        public Vector3 AngularVelocity;

        public TransformRate(Vector3 velocity, Vector3 angularVelocity)
        {
            this.Velocity = velocity;
            this.AngularVelocity = angularVelocity;
        }

        public static TransformRate Zero { get { return new TransformRate(); } }

        public static TransformRate Add(TransformRate rate1, TransformRate rate2)
        {
            TransformRate result = new TransformRate();
            Vector3.Add(ref rate1.Velocity, ref rate2.Velocity, out result.Velocity);
            Vector3.Add(ref rate1.AngularVelocity, ref rate2.AngularVelocity, out result.AngularVelocity);
            return result;
        }

        public static void Add(ref TransformRate rate1, ref TransformRate rate2 ,out TransformRate result)
        {
            Vector3.Add(ref rate1.Velocity, ref rate2.Velocity, out result.Velocity);
            Vector3.Add(ref rate1.AngularVelocity, ref rate2.AngularVelocity, out result.AngularVelocity);
        }

        //public static TransformRate operator +(TransformRate rate1, TransformRate rate2)
        //{
        //    return new TransformRate(rate1.Velocity + rate2.Velocity, rate1.AngularVelocity + rate2.AngularVelocity);
        //}


    }
    #endregion

}
