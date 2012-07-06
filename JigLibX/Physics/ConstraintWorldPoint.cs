#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Collision;
using JigLibX.Math;
#endregion

namespace JigLibX.Physics
{
    /// <summary>
    /// Constrains a point within a rigid body to remain at a fixed world point    
    /// </summary>
    public class ConstraintWorldPoint : Constraint
    {
        const float minVelForProcessing = 0.001f;

        private Body body;
        private Vector3 pointOnBody;
        private Vector3 worldPosition;

        public ConstraintWorldPoint()
        {
            Initialise(null, Vector3.Zero, Vector3.Zero);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="pointOnBody">pointOnBody is in body coords</param>
        /// <param name="worldPosition"></param>
        public ConstraintWorldPoint(Body body, Vector3 pointOnBody, Vector3 worldPosition)
        {
            Initialise(body, pointOnBody, worldPosition);
        }

        public void Initialise(Body body, Vector3 pointOnBody, Vector3 worldPosition)
        {
            this.body = body;
            this.pointOnBody = pointOnBody;
            this.worldPosition = worldPosition;
         
            if (body!=null) body.AddConstraint(this);
        }

        public override void PreApply(float dt)
        {
            Satisfied = false;
        }

        public override bool Apply(float dt)
        {
            Satisfied = true;

            // transform PointOnBody to the world space

            #region REFERENCE: Vector3 worldPos = body.Position + Vector3.Transform(pointOnBody, body.Orientation);
            Vector3 worldPos;
            Vector3.Transform(ref pointOnBody, ref body.transform.Orientation, out worldPos);
            Vector3.Add(ref worldPos, ref body.transform.Position, out worldPos);
            #endregion

            #region REFERENCE: Vector3 R = worldPos - body.Position;
            Vector3 R;
            Vector3.Subtract(ref worldPos, ref body.transform.Position, out R);
            #endregion

            #region REFERENCE: Vector3 currentVel = body.Velocity + Vector3.Cross(body.AngVel, R);
            Vector3 currentVel;
            Vector3.Cross(ref body.transformRate.AngularVelocity, ref R, out currentVel);
            Vector3.Add(ref currentVel, ref body.transformRate.Velocity, out currentVel);
            #endregion

            // add an extra term to get us back to the original position
            Vector3 desiredVel;

            float allowedDeviation = 0.01f;
            float timescale = 4.0f * dt;

            #region REFERENCE: Vector3 deviation = worldPos - worldPosition;
            Vector3 deviation;
            Vector3.Subtract(ref worldPos,ref worldPosition,out deviation);
            #endregion

            float deviationDistance = deviation.Length();

            if (deviationDistance > allowedDeviation)
            {
                #region REFERENCE: Vector3 deviationDir = deviation / deviationDistance;
                Vector3 deviationDir;
                Vector3.Divide(ref deviation, deviationDistance, out deviationDir);
                #endregion

                #region REFERENCE: desiredVel = ((allowedDeviation - deviationDistance) / timescale) * deviationDir;
                Vector3.Multiply(ref deviationDir, (allowedDeviation - deviationDistance) / timescale, out desiredVel);
                #endregion

            }
            else
            {
                desiredVel = Vector3.Zero;
            }

            // stop velocities pushing us through geometry
            if (body.CollisionSkin != null)
            {
                List<CollisionInfo> collisions = body.CollisionSkin.Collisions;

                int num = collisions.Count;

                for (int i = 0; i < num; i++)
                {
                    CollisionInfo collInfo = collisions[i];

                    if (collInfo.SkinInfo.Skin1.Owner == null)
                    {
                        Vector3 dir = collInfo.DirToBody0;

                        #region float dot = Vector3.Dot(desiredVel, dir);
                        float dot;
                        Vector3.Dot(ref desiredVel,ref dir,out dot);
                        #endregion

                        if (dot < 0.0f)
                            desiredVel -= dot * dir;
                    }
                }
            }

            // need an impulse to take us from the current vel to the desired vel
            #region REFERENCE: Vector3 N = currentVel - desiredVel;
            Vector3 N;
            Vector3.Subtract(ref currentVel, ref desiredVel, out N);
            #endregion

            float normalVel = N.Length();

            if (normalVel < minVelForProcessing)
                return false;

            #region REFERENCE: N /= normalVel;
            Vector3.Divide(ref N, normalVel, out N);
            #endregion

            #region REFERENCE: float denominator = body.InvMass + Vector3.Dot(N, Vector3.Cross(Vector3.Transform(Vector3.Cross(R, N), body.WorldInvInertia), R));
            Vector3 v1; float f1;
            Vector3.Cross(ref R, ref N, out v1);
            Vector3.Transform(ref v1, ref body.worldInvInertia, out v1);
            Vector3.Cross(ref v1, ref R, out v1);
            Vector3.Dot(ref N, ref v1, out f1);

            float denominator = body.InverseMass + f1;
            #endregion

            if (denominator < JiggleMath.Epsilon)
                return false;

            float normalImpulse = -normalVel / denominator;

            body.ApplyWorldImpulse(normalImpulse * N, worldPos);

            body.SetConstraintsAndCollisionsUnsatisfied();
            Satisfied = true;

            return true;
        }

        public override void Destroy()
        {
            // this is moved here from ConstraintWorldPoint destructor..
            if (body != null) body.RemoveConstraint(this);

            body = null;
            DisableConstraint();

        }

        public Vector3 WorldPosition
        {   
            set { worldPosition = value;}
            get { return worldPosition; }
        }

        public Body Body
        {
            get { return body; }
        }

    }
}
