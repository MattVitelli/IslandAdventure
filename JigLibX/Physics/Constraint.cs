#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
#endregion

namespace JigLibX.Physics
{
    public abstract class Constraint
    {

        private bool constraintEnabled = false;
        private bool satisfied = false;

        /// <summary>
        /// Register with the physics system.
        /// </summary>
        public void EnableConstraint()
        {
            if (PhysicsSystem.CurrentPhysicsSystem == null) return;
            if (constraintEnabled) return;

            constraintEnabled = true;
            PhysicsSystem.CurrentPhysicsSystem.AddConstraint(this);
        }

        /// <summary>
        /// deregister from the physics system
        /// </summary>
        public void DisableConstraint()
        {
            if (PhysicsSystem.CurrentPhysicsSystem == null) return;
            if (!constraintEnabled) return;

            constraintEnabled = false;
            PhysicsSystem.CurrentPhysicsSystem.RemoveConstraint(this);
        }

        /// <summary>
        /// Are we registered with the physics system?
        /// </summary>
        public bool IsConstraintEnabled
        {
            get { return constraintEnabled; }
        }

        /// <summary>
        /// prepare for applying constraints - the subsequent calls to
        /// apply will all occur with a constant position i.e. precalculate
        /// everything possible
        /// </summary>
        /// <param name="dt"></param>
        public abstract void PreApply(float dt);

        /// <summary>
        /// apply the constraint by adding impulses. Return value
        /// indicates if any impulses were applied. If impulses were applied
        /// the derived class should call SetConstraintsUnsatisfied() on each
        /// body that is involved.
        /// </summary>
        /// <param name="dt"></param>
        public abstract bool Apply(float dt);

        /// <summary>
        /// implementation should remove all references to bodies etc - they've 
        /// been destroyed.
        /// </summary>
        public abstract void Destroy();

        /// <summary>
        /// Derived class should call this when Apply has been called on 
        /// this constraint.
        /// </summary>
        public bool Satisfied
        {
            get { return this.satisfied; }
            set { this.satisfied = value; }
        }

        /// <summary>
        /// SmoothCD for ease-in / ease-out smoothing 
        /// Based on Game Programming Gems 4 Chapter 1.10
        /// </summary>
        /// <param name="val">in/out: value to be smoothed</param>
        /// <param name="valRate">in/out: rate of change of the value</param>
        /// <param name="timeDelta">in: time interval</param>
        /// <param name="to">in: the target value</param>
        /// <param name="smoothTime">in: timescale for smoothing</param>
        public static void SmoothCD(ref Vector3 val, ref Vector3 valRate, float timeDelta, Vector3 to, float smoothTime)
        {
            if (smoothTime > 0.0f)
            {
                float omega = 2.0f / smoothTime;
                float x = omega * timeDelta;
                float exp = 1.0f / (1.0f + x + 0.48f * x * x + 0.235f * x * x * x);

                #region REFERENCE: Vector3 change = val - to;
                Vector3 change;// = val - to;
                Vector3.Subtract(ref val, ref to, out change);
                #endregion

                #region REFERENCE: Vector3 temp = (valRate + omega * change) * timeDelta;
                Vector3 temp;
                Vector3.Multiply(ref change, omega, out temp);
                Vector3.Add(ref valRate, ref temp, out temp);
                Vector3.Multiply(ref temp, timeDelta, out temp);
                #endregion

                #region REFERENCE: valRate = (valRate - omega * temp) * exp;
                Vector3 v1;
                Vector3.Multiply(ref temp, omega, out v1);
                Vector3.Subtract(ref valRate, ref v1, out v1);
                Vector3.Multiply(ref v1, exp, out valRate);
                #endregion

                #region REFERENCE: val = to + (change + temp) * exp;
                Vector3.Add(ref change, ref temp, out val);
                Vector3.Multiply(ref val, exp, out val);
                Vector3.Add(ref val, ref to, out val);
                #endregion
            }
            else if (timeDelta > 0.0f)
            {
                #region REFERENCE: valRate = (to - val) / timeDelta;
                Vector3.Subtract(ref to, ref val, out valRate);
                Vector3.Divide(ref valRate, timeDelta, out valRate);
                #endregion

                val = to;
            }
            else
            {
                val = to;
                valRate.X = valRate.Y = valRate.Z = 0.0f;  // zero it...
            }
        }

    }
}
