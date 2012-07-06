#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Collision;
#endregion

namespace JigLibX.Physics
{
    /// <summary>
    /// constraints a velocity to be a certain value - either in world 
    /// or body (by transforming the velocity direction) coordinates
    /// </summary>
    public class ConstraintVelocity : Constraint
    {
        public enum ReferenceFrame
        {
            World,
            Body
        }

        private Body body;
        private ReferenceFrame frame;
        private Vector3 vel;
        private Vector3 angVel;
        private Vector3 velRate;
        private Vector3 angVelRate;
        private Vector3 targetVel;
        private Vector3 targetAngVel;
        private bool doVel;
        private bool doAngVel;

        public ConstraintVelocity(Body body, ReferenceFrame frame, Vector3? vel, Vector3? angVel)
        {
            Initialise(body, frame, vel, angVel);
        }

        public ConstraintVelocity()
        {
            Initialise(null, ReferenceFrame.World, Vector3.Zero, Vector3.Zero);
        }

        public void Initialise(Body body, ReferenceFrame frame, Vector3? vel, Vector3? angVel)
        {
            this.body = body;
            this.frame = frame;

            this.doVel = (vel != null);
            this.doAngVel = (angVel != null);

            if (doVel) targetVel = (Vector3)vel;
            if (doAngVel) targetAngVel = (Vector3)angVel;

            if (body != null)
            {
                vel = body.Velocity;
                angVel = body.AngularVelocity;
            }
            else
            {
                vel = Vector3.Zero;
                angVel = Vector3.Zero;
            }

            velRate = Vector3.Zero;
            angVelRate = Vector3.Zero;

            if (body != null)
                body.AddConstraint(this);
        }

        /// apply impulses to satisfy the constraint.
        public override void PreApply(float dt)
        {
            Satisfied = false;
            //base.PreApply(dt);

            float smoothTime = 0.2f;

            if (doVel)
                Constraint.SmoothCD(ref vel, ref velRate, dt, targetVel, smoothTime);

            if (doAngVel)
                Constraint.SmoothCD(ref angVel, ref angVelRate, dt, targetAngVel, smoothTime);

            // Try to prevent constraining the velocity into pushing through static geometry
            if (doVel && body.CollisionSkin != null && body.CollisionSkin.Collisions.Count != 0)
            {
                List<CollisionInfo> collisions = body.CollisionSkin.Collisions;
                int num = collisions.Count;

                for (int i = 0; i < num; i++)
                {
                    CollisionInfo collInfo = collisions[i];

                    if (collInfo.SkinInfo.Skin1.Owner == null)
                    {

                        Vector3 dir = collInfo.DirToBody0;
                        //Vector3 dir = collInfo.DirToBody0.GetNormalisedSafe();

                        #region REFERENCE: float dot = Vector3.Dot(vel, dir);
                        float dot;
                        Vector3.Dot(ref vel, ref dir, out dot);
                        #endregion

                        if (dot < 0.0f)
                        {
                            #region REFERENCE: vel -= dot * dir;
                            Vector3 v1;
                            Vector3.Multiply(ref dir, dot, out v1);
                            Vector3.Subtract(ref vel, ref v1, out vel);
                            #endregion
                        }

                    }
                }
            }
        }

        public override bool Apply(float dt)
        {
            Satisfied = true;

            if (body == null) return false;

            float frac = 0.001f;

            if (frame == ReferenceFrame.Body) // transfrom velocity to the body frame
            {
                if (doVel)
                {
                    #region REFERENCE: Vector3 velBodyFrame = Vector3.Transform(vel, body.Orientation);
                    Vector3 velBodyFrame;
                    Vector3.Transform(ref vel, ref body.transform.Orientation, out velBodyFrame);
                    #endregion

                    #region REFERENCE: body.Velocity = (frac * velBodyFrame + (1.0f - frac) * body.Velocity);
                    Vector3 v1;
                    Vector3.Multiply(ref body.transformRate.Velocity, 1.0f - frac, out v1);
                    Vector3.Multiply(ref velBodyFrame, frac, out body.transformRate.Velocity);
                    Vector3.Add(ref body.transformRate.Velocity, ref v1, out body.transformRate.Velocity);
                    #endregion
                }

                if (doAngVel)
                {
                    #region REFERENCE: Vector3 angVelBodyFrame = Vector3.Transform(angVel, body.Orientation);
                    Vector3 angVelBodyFrame;
                    Vector3.Transform(ref angVel, ref body.transform.Orientation, out angVelBodyFrame);
                    #endregion

                    #region REFERENCE: body.AngVel = (frac * angVelBodyFrame + (1.0f - frac) * body.AngVel);
                    Vector3 v1;
                    Vector3.Multiply(ref body.transformRate.AngularVelocity, 1.0f - frac, out v1);
                    Vector3.Multiply(ref angVelBodyFrame, frac, out body.transformRate.AngularVelocity);
                    Vector3.Add(ref body.transformRate.AngularVelocity, ref v1, out body.transformRate.AngularVelocity);
                    #endregion
                }
            }
            else // leave velocity in the world frame
            {
                if (doVel)
                {
                    #region REFERENCE: body.Velocity = (frac * vel + (1.0f - frac) * body.Velocity);
                    Vector3 v1;
                    Vector3.Multiply(ref body.transformRate.Velocity, 1.0f - frac, out body.transformRate.Velocity);
                    Vector3.Multiply(ref vel, frac, out v1);
                    Vector3.Add(ref body.transformRate.Velocity, ref v1, out body.transformRate.Velocity);
                    #endregion
                }

                if (doAngVel)
                {
                    #region REFERENCE: body.AngVel = (frac * angVel + (1.0f - frac) * body.AngVel);
                    Vector3 v1;
                    Vector3.Multiply(ref body.transformRate.AngularVelocity, 1.0f - frac, out body.transformRate.AngularVelocity);
                    Vector3.Multiply(ref angVel, frac, out v1);
                    Vector3.Add(ref body.transformRate.AngularVelocity, ref v1, out body.transformRate.AngularVelocity);
                    #endregion
                }
            }
            /// todo return false if we were already there...

            body.SetConstraintsAndCollisionsUnsatisfied();
            Satisfied = true;

            return true;
        }

        public override void Destroy()
        {
            // this is moved here from ConstraintVelocity destructor..
            if (body != null) body.RemoveConstraint(this);

            body = null;
            DisableConstraint();
        }

    }
}

