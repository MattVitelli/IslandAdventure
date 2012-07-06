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
    /// Implements a simple hinge between two rigid bodies. The bodies
    /// should be in a suitable configuration when this joint is created.
    /// </summary>
    public class HingeJoint : Joint
    {
        private Vector3 hingeAxis;
        private Vector3 hingePosRel0;
        private Body body0;
        private Body body1;
        private bool usingLimit;
        private bool hingeEnabled;
        private bool broken;
        private float damping;
        private float extraTorque; // allow extra torque applied per update

        private ConstraintPoint mMidPointConstraint;
        private ConstraintMaxDistance[] mSidePointConstraints;
        private ConstraintMaxDistance mMaxDistanceConstraint;

        /// <summary>
        /// default constructor so you can initialise this joint later
        /// </summary>
        public HingeJoint()
        {
        }

        public void Initialise(Body body0, Body body1, Vector3 hingeAxis, Vector3 hingePosRel0,
                float hingeHalfWidth, float hingeFwdAngle, float hingeBckAngle, float sidewaysSlack, float damping)
        {
            this.body0 = body0;
            this.body1 = body1;
            this.hingeAxis = hingeAxis;
            this.hingePosRel0 = hingePosRel0;
            this.usingLimit = false;
            this.damping = damping;

            //  tScalar allowedDistance = 0.005f;
            this.hingeAxis.Normalize();

            Vector3 hingePosRel1 = body0.Position + hingePosRel0 - body1.Position;

            // generate the two positions relative to each body
            Vector3 relPos0a = hingePosRel0 + hingeHalfWidth * hingeAxis;
            Vector3 relPos0b = hingePosRel0 - hingeHalfWidth * hingeAxis;

            Vector3 relPos1a = hingePosRel1 + hingeHalfWidth * hingeAxis;
            Vector3 relPos1b = hingePosRel1 - hingeHalfWidth * hingeAxis;

            float timescale = 1.0f / 20.0f;
            float allowedDistanceMid = 0.005f;
            float allowedDistanceSide = sidewaysSlack * hingeHalfWidth;

            mSidePointConstraints = new ConstraintMaxDistance[2];

            mSidePointConstraints[0] = new ConstraintMaxDistance();
            mSidePointConstraints[1] = new ConstraintMaxDistance();

            mSidePointConstraints[0].Initialise(body0, relPos0a, body1, relPos1a, allowedDistanceSide);
            mSidePointConstraints[1].Initialise(body0, relPos0b, body1, relPos1b, allowedDistanceSide);

            mMidPointConstraint = new ConstraintPoint();
            mMidPointConstraint.Initialise(body0, hingePosRel0, body1, hingePosRel1, allowedDistanceMid, timescale);

            if (hingeFwdAngle <= 150) // MAX_HINGE_ANGLE_LIMIT
            {
                // choose a direction that is perpendicular to the hinge
                Vector3 perpDir = Vector3.Up;

                if (Vector3.Dot(perpDir, hingeAxis) > 0.1f)
                    perpDir = Vector3.Right;

                // now make it perpendicular to the hinge
                Vector3 sideAxis = Vector3.Cross(hingeAxis, perpDir);
                perpDir = Vector3.Cross(sideAxis, hingeAxis);
                perpDir.Normalize();

                // the length of the "arm" TODO take this as a parameter? what's
                // the effect of changing it?
                float len = 10.0f * hingeHalfWidth;

                // Choose a position using that dir. this will be the anchor point
                // for body 0. relative to hinge
                Vector3 hingeRelAnchorPos0 = perpDir * len;

                // anchor point for body 2 is chosen to be in the middle of the
                // angle range.  relative to hinge
                float angleToMiddle = 0.5f * (hingeFwdAngle - hingeBckAngle);
                Vector3 hingeRelAnchorPos1 = Vector3.Transform(hingeRelAnchorPos0, Matrix.CreateFromAxisAngle(hingeAxis,MathHelper.ToRadians(-angleToMiddle)));

                // work out the "string" length
                float hingeHalfAngle = 0.5f * (hingeFwdAngle + hingeBckAngle);
                float allowedDistance = len * 2.0f * (float)System.Math.Sin(MathHelper.ToRadians(hingeHalfAngle * 0.5f));

                Vector3 hingePos = body1.Position + hingePosRel0;
                Vector3 relPos0c = hingePos + hingeRelAnchorPos0 - body0.Position;
                Vector3 relPos1c = hingePos + hingeRelAnchorPos1 - body1.Position;

                mMaxDistanceConstraint = new ConstraintMaxDistance();

                mMaxDistanceConstraint.Initialise(body0, relPos0c, body1, relPos1c, allowedDistance);

                usingLimit = true;
            }
            if (damping <= 0.0f)
                damping = -1.0f; // just make sure that a value of 0.0 doesn't mess up...
            else
                damping = MathHelper.Clamp(damping, 0, 1);
        }

        /// <summary>
        /// Register the constraints
        /// </summary>
        public void EnableHinge()
        {
            if (hingeEnabled)
                return;

            if (body0 != null)
            {
                mMidPointConstraint.EnableConstraint();
                mSidePointConstraints[0].EnableConstraint();
                mSidePointConstraints[1].EnableConstraint();

                if (usingLimit && !broken)
                    mMaxDistanceConstraint.EnableConstraint();

                EnableController();
            }
            hingeEnabled = true;
        }

        /// <summary>
        /// deregister the constraints
        /// </summary>
        public void DisableHinge()
        {
            if (!hingeEnabled)
                return;

            if (body0 != null)
            {
                mMidPointConstraint.DisableConstraint();
                mSidePointConstraints[0].DisableConstraint();
                mSidePointConstraints[1].DisableConstraint();

                if (usingLimit && !broken)
                    mMaxDistanceConstraint.DisableConstraint();

                DisableController();
            }
            hingeEnabled = false;
        }

        /// <summary>
        /// Just remove the limit constraint
        /// </summary>
        public void Break()
        {
            if (broken)
                return;

            if (usingLimit)
                mMaxDistanceConstraint.DisableConstraint();

            broken = true;

        }

        /// <summary>
        /// Just enable the limit constraint
        /// </summary>
        public void Mend()
        {
            if (!broken)
                return;

            if (usingLimit)
                mMaxDistanceConstraint.EnableConstraint();

            broken = false;

        }

        public override void UpdateController(float dt)
        {
            if (body0 == null || body1 == null)
                return;

            //Assert(0 != mBody0);
            //Assert(0 != mBody1);

            if (damping > 0.0f)
            {
                // Some hinges can bend in wonky ways. Derive the effective hinge axis
                // using the relative rotation of the bodies.
                Vector3 hingeAxis = body1.AngularVelocity - body0.AngularVelocity;

                JiggleMath.NormalizeSafe(ref hingeAxis);

                float angRot1;//
                Vector3.Dot(ref body0.transformRate.AngularVelocity,ref hingeAxis,out angRot1);
                float angRot2;
                Vector3.Dot(ref body1.transformRate.AngularVelocity,ref hingeAxis,out angRot2);

                float avAngRot = 0.5f * (angRot1 + angRot2);

                float frac = 1.0f - damping;
                float newAngRot1 = avAngRot + (angRot1 - avAngRot) * frac;
                float newAngRot2 = avAngRot + (angRot2 - avAngRot) * frac;

                Vector3 newAngVel1;// = body0.AngVel + (newAngRot1 - angRot1) * hingeAxis;
                Vector3.Multiply(ref hingeAxis, newAngRot1 - angRot1, out newAngVel1);
                Vector3.Add(ref newAngVel1,ref body0.transformRate.AngularVelocity, out newAngVel1);

                Vector3 newAngVel2;// = body1.AngVel + (newAngRot2 - angRot2) * hingeAxis;
                Vector3.Multiply(ref hingeAxis, newAngRot2 - angRot2, out newAngVel2);
                Vector3.Add(ref newAngVel2, ref body1.transformRate.AngularVelocity, out newAngVel2);

                body0.AngularVelocity = newAngVel1;
                body1.AngularVelocity = newAngVel2;
            }

            // the extra torque
            if (extraTorque != 0.0f)
            {
                Vector3 torque1;// = extraTorque * Vector3.Transform(hingeAxis, body0.Orientation);
                Vector3.Transform(ref hingeAxis, ref body0.transform.Orientation, out torque1);
                Vector3.Multiply(ref torque1, extraTorque, out torque1);

                body0.AddWorldTorque(torque1);
                body1.AddWorldTorque(-torque1);
            }
        }

        public bool HingeEnabled
        {
            get { return hingeEnabled; }
        }

        /// <summary>
        /// Are we broken
        /// </summary>
        public bool IsBroken
        {
            get { return broken; }
        }

    }
}
