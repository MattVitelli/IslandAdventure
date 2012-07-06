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
    public class ConstraintPoint : Constraint
    {
        private const float mMaxVelMag = 20.0f;
        private const float minVelForProcessing = 0.01f;

        private Vector3 body0Pos;
        private Body body0;
        private Vector3 body1Pos;
        private Body body1;
        private float allowedDistance;
        private float timescale;

        // some values that we calculate once in pre_apply
        private Vector3 worldPos; ///< average of the two joint positions
        private Vector3 R0; ///< position relative to body 0 (in world space)
        private Vector3 R1;
        private Vector3 vrExtra; ///< extra vel for restoring deviation

        public ConstraintPoint()
        {
        }

        public ConstraintPoint(Body body0, Vector3 body0Pos, Body body1, Vector3 body1Pos, float allowedDistance, float timescale)
        {
            Initialise(body0, body0Pos, body1, body1Pos, allowedDistance, timescale);
        }

        public void Initialise(Body body0, Vector3 body0Pos, Body body1, Vector3 body1Pos, float allowedDistance, float timescale)
        {
            this.body0Pos = body0Pos;
            this.body1Pos = body1Pos;
            this.body0 = body0;
            this.body1 = body1;

            this.allowedDistance = allowedDistance;
            this.timescale = timescale;

            if (timescale < JiggleMath.Epsilon)
                timescale = JiggleMath.Epsilon;

            if (body0 != null) body0.AddConstraint(this);
            if (body1 != null) body1.AddConstraint(this);
        }

        public override void PreApply(float dt)
        {
            this.Satisfied = false;

            #region REFERENCE: R0 = Vector3.Transform(body0Pos, body0.Orientation);
            Vector3.Transform(ref body0Pos, ref body0.transform.Orientation, out R0);
            #endregion

            #region REFERENCE: R1 = Vector3.Transform(body1Pos, body1.Orientation);
            Vector3.Transform(ref body1Pos, ref body1.transform.Orientation, out R1);
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

            // add a "correction" based on the deviation of point 0
            #region REFERENCE: Vector3 deviation = worldPos0 - worldPos1;
            Vector3 deviation;
            Vector3.Subtract(ref worldPos0, ref worldPos1, out deviation);
            #endregion

            float deviationAmount = deviation.Length();

            if (deviationAmount > allowedDistance)
            {
                #region REFERENCE: vrExtra = ((deviationAmount - allowedDistance) / (deviationAmount * System.Math.Max(timescale, dt))) * deviation;
                Vector3.Multiply(ref deviation, (deviationAmount - allowedDistance) / (deviationAmount * System.Math.Max(timescale, dt)), out vrExtra);
                #endregion
            }
            else
            {
                vrExtra = Vector3.Zero;
            }
        }

        public override bool Apply(float dt)
        {
            this.Satisfied = true;

            bool body0FrozenPre = !body0.IsActive;
            bool body1FrozenPre = !body1.IsActive;

            //  if (body0FrozenPre && body1FrozenPre)
            //    return false;

            #region REFERENCE: Vector3 currentVel0 = (body0.Velocity + Vector3.Cross(body0.AngVel, R0));
            Vector3 currentVel0;
            Vector3.Cross(ref body0.transformRate.AngularVelocity, ref R0, out currentVel0);
            Vector3.Add(ref currentVel0, ref body0.transformRate.Velocity, out currentVel0);
            #endregion

            #region REFERENCE: Vector3 currentVel1 = (body1.Velocity + Vector3.Cross(body1.AngVel, R1));
            Vector3 currentVel1;
            Vector3.Cross(ref body1.transformRate.AngularVelocity, ref R1, out currentVel1);
            Vector3.Add(ref currentVel1, ref body1.transformRate.Velocity, out currentVel1);
            #endregion

            // add a "correction" based on the deviation of point 0
            #region REFERENCE: Vector3 Vr = (vrExtra + currentVel0 - currentVel1);
            Vector3 Vr;
            Vector3.Add(ref vrExtra, ref currentVel0, out Vr);
            Vector3.Subtract(ref Vr, ref currentVel1, out Vr);
            #endregion

            float normalVel = Vr.Length();

            if (normalVel < minVelForProcessing)
                return false;

            // limit things
            if (normalVel > mMaxVelMag)
            {
                #region REFERENCE: Vr *= mMaxVelMag / normalVel;
                Vector3.Multiply(ref Vr, mMaxVelMag / normalVel, out Vr);
                #endregion
                normalVel = mMaxVelMag;
            }

            #region REFERENCE: Vector3 N = Vr / normalVel;
            Vector3 N;
            Vector3.Divide(ref Vr, normalVel, out N);
            #endregion

            float numerator = -normalVel;

            #region REFERENCE: float denominator = body0.InvMass + body1.InvMass + Vector3.Dot(N, Vector3.Cross(Vector3.Transform(Vector3.Cross(R0, N), body0.WorldInvInertia), R0)) + Vector3.Dot(N, Vector3.Cross(Vector3.Transform(Vector3.Cross(R1, N), body1.WorldInvInertia), R1));
            Vector3 v1; float f1, f2;
            Vector3.Cross(ref R0, ref N, out v1);
            Vector3.Transform(ref v1, ref body0.worldInvInertia, out v1);
            Vector3.Cross(ref v1, ref R0, out v1);
            Vector3.Dot(ref N, ref v1, out f1);
            Vector3.Cross(ref R1, ref N, out v1);
            Vector3.Transform(ref v1, ref body1.worldInvInertia, out v1);
            Vector3.Cross(ref v1, ref R1, out v1);
            Vector3.Dot(ref N, ref v1, out f2);

            float denominator = body0.InverseMass + body1.InverseMass + f1 + f2;
            #endregion

            if (denominator < JiggleMath.Epsilon)
                return false;

            #region REFERENCE: Vector3 normalImpulse = (numerator / denominator) * N;
            Vector3 normalImpulse;
            Vector3.Multiply(ref N, numerator / denominator, out normalImpulse);
            #endregion

            if (!body0.Immovable)
                body0.ApplyWorldImpulse(normalImpulse, worldPos);

            if (!body1.Immovable)
                body1.ApplyWorldImpulse(-normalImpulse, worldPos);

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
