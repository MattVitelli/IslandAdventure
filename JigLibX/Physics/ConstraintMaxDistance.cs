#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
#endregion

namespace JigLibX.Physics
{
    /// <summary>
    /// Constraints a point on one body to be fixed to a point on another body
    /// </summary>
    public class ConstraintMaxDistance : Constraint
    {
        // maximum relative velocity induced at the constraint points - proportional
        // to the error
        private const float maxVelMag = 20.0f;
        private const float minVelForProcessing = 0.01f;

        // configuration
        private Body body0;
        private Body body1;
        private Vector3 body0Pos;
        private Vector3 body1Pos;
        private float mMaxDistance;

        // stuff that gets updated
        private Vector3 R0;
        private Vector3 R1;
        private Vector3 worldPos;
        private Vector3 currentRelPos0;

        public ConstraintMaxDistance()
        { }

        public ConstraintMaxDistance(Body body0, Vector3 body0Pos, Body body1, Vector3 body1Pos, float maxDistance)
        {
            Initialise(body0, body0Pos, body1, body1Pos, maxDistance);
        }

        public void Initialise(Body body0, Vector3 body0Pos, Body body1, Vector3 body1Pos, float maxDistance)
        {
            this.body0Pos = body0Pos;
            this.body1Pos = body1Pos;
            this.body0 = body0;
            this.body1 = body1;
            this.mMaxDistance = maxDistance;

            if (body0 != null) this.body0.AddConstraint(this);
            if (body1 != null) this.body1.AddConstraint(this);
        }

        public override void PreApply(float dt)
        {
            this.Satisfied = false;

            #region REFERENCE: R0 = Vector3.Transform(body0Pos, body0.Orientation);
            Vector3.Transform(ref body0Pos, ref body0.transform.Orientation,out R0);
            #endregion

            #region REFERENCE: R1 = Vector3.Transform(body1Pos, body1.Orientation);
            Vector3.Transform(ref body1Pos, ref body1.transform.Orientation,out R1);
            #endregion 

            #region REFERENCE: Vector3 worldPos0 = body0.Position + R0;
            Vector3 worldPos0;
            Vector3.Add(ref body0.transform.Position, ref R0, out worldPos0);
            #endregion

            #region REFERENCE: Vector3 worldPos1 = body1.Position + R1;
            Vector3 worldPos1;
            Vector3.Add(ref body1.transform.Position, ref R1, out worldPos1);
            #endregion

            #region REFERENCE: worldPos = 0.5f * (worldPos0 + worldPos1);
            Vector3.Add(ref worldPos0, ref worldPos1, out worldPos);
            Vector3.Multiply(ref worldPos, 0.5f, out worldPos);
            #endregion

            #region REFERENCE: currentRelPos0 = worldPos0 - worldPos1;
            Vector3.Subtract(ref worldPos0, ref worldPos1, out currentRelPos0);
            #endregion
        }

        public override bool Apply(float dt)
        {
            this.Satisfied = true;

            bool body0FrozenPre = !body0.IsActive;
            bool body1FrozenPre = !body1.IsActive;

            if (body0FrozenPre && body1FrozenPre)
                return false;

            #region REFERENCE: Vector3 currentVel0 = body0.Velocity + Vector3.Cross(body0.AngVel, R0);
            Vector3 currentVel0 = body0.AngularVelocity;
            Vector3.Cross(ref currentVel0, ref R0, out currentVel0);
            Vector3.Add(ref body0.transformRate.Velocity, ref currentVel0, out currentVel0);
            #endregion

            #region REFERENCE: Vector3 currentVel1 = body1.Velocity + Vector3.Cross(body1.AngVel, R1);
            Vector3 currentVel1 = body1.AngularVelocity;
            Vector3.Cross(ref currentVel1, ref R1, out currentVel1);
            Vector3.Add(ref body1.transformRate.Velocity, ref currentVel1, out currentVel1);
            #endregion

            // predict a new location
            #region REFERENCE: Vector3 predRelPos0 = (currentRelPos0 + (currentVel0 - currentVel1) * dt);
            Vector3 predRelPos0;
            Vector3.Subtract(ref currentVel0, ref currentVel1, out predRelPos0);
            Vector3.Multiply(ref predRelPos0, dt, out predRelPos0);
            Vector3.Add(ref predRelPos0, ref currentRelPos0, out predRelPos0);
            #endregion

            // if the new position is out of range then clamp it
            Vector3 clampedRelPos0 = predRelPos0;

            float clampedRelPos0Mag = clampedRelPos0.Length();

            if (clampedRelPos0Mag <= JiggleMath.Epsilon)
                return false;

            if (clampedRelPos0Mag > mMaxDistance)
                #region REFERENCE: clampedRelPos0 *= mMaxDistance / clampedRelPos0Mag;
                Vector3.Multiply(ref clampedRelPos0, mMaxDistance / clampedRelPos0Mag, out clampedRelPos0);
                #endregion

            // now claculate desired vel based on the current pos, new/clamped
            // pos and dt
            #region REFERENCE: Vector3 desiredRelVel0 = ((clampedRelPos0 - currentRelPos0) / System.Math.Max(dt, JiggleMath.Epsilon));
            Vector3 desiredRelVel0;
            Vector3.Subtract(ref clampedRelPos0, ref currentRelPos0, out desiredRelVel0);
            Vector3.Divide(ref desiredRelVel0, MathHelper.Max(dt, JiggleMath.Epsilon), out desiredRelVel0);
            #endregion

            // Vr is -ve the total velocity change
            #region REFERENCE: Vector3 Vr = (currentVel0 - currentVel1) - desiredRelVel0;
            Vector3 Vr;
            Vector3.Subtract(ref currentVel0, ref currentVel1, out Vr);
            Vector3.Subtract(ref Vr, ref desiredRelVel0, out Vr);
            #endregion

            float normalVel = Vr.Length();

            // limit it
            if (normalVel > maxVelMag)
            {
                #region REFERENCE: Vr *= (maxVelMag / normalVel);
                Vector3.Multiply(ref Vr, maxVelMag / normalVel, out Vr);
                #endregion
                normalVel = maxVelMag;
            }
            else if (normalVel < minVelForProcessing)
            {
                return false;
            }

            #region REFERENCE: Vector3 N = Vr / normalVel;
            Vector3 N;
            Vector3.Divide(ref Vr, normalVel, out N);
            #endregion

            #region REFERENCE: float denominator = body0.InvMass + body1.InvMass + Vector3.Dot(N, Vector3.Cross(Vector3.Transform(Vector3.Cross(R0, N), body0.WorldInvInertia), R0)) + Vector3.Dot(N, Vector3.Cross(Vector3.Transform(Vector3.Cross(R1, N), body1.WorldInvInertia), R1));
            Vector3 v1; float f1, f2;
            Vector3.Cross(ref R0, ref N, out v1);
            Vector3.Transform(ref v1, ref body0.worldInvInertia, out v1);
            Vector3.Cross(ref v1, ref R0, out v1);
            Vector3.Dot(ref N,ref v1,out f1);
            Vector3.Cross(ref R1, ref N, out v1);
            Vector3.Transform(ref v1, ref body1.worldInvInertia, out v1);
            Vector3.Cross(ref v1, ref R1, out v1);
            Vector3.Dot(ref N, ref v1, out f2);

            float denominator = body0.InverseMass + body1.InverseMass + f1 + f2;
            #endregion

            if (denominator < JiggleMath.Epsilon)
                return false;

            float normalImpulse = -normalVel / denominator;

            #region REFERENCE: if (!body0.Immovable) body0.ApplyWorldImpulse(normalImpulse * N, worldPos);
            Vector3 imp;
            Vector3.Multiply(ref N, normalImpulse, out imp);

            if (!body0.Immovable)
                body0.ApplyWorldImpulse(ref imp, ref worldPos);
            #endregion

            #region REFERENCE: if (!body1.Immovable) body1.ApplyWorldImpulse(-normalImpulse * N, worldPos);
            Vector3.Multiply(ref N, -normalImpulse, out imp);

            if (!body1.Immovable)
                body1.ApplyWorldImpulse(ref imp,ref worldPos);
            #endregion

            body0.SetConstraintsAndCollisionsUnsatisfied();
            body1.SetConstraintsAndCollisionsUnsatisfied();

            this.Satisfied = true;

            return true;
        }

        public override void Destroy()
        {
            body0 = null;
            body1 = null;

            DisableConstraint();
        }
    }
}
