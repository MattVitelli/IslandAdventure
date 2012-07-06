#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Math;
using JigLibX.Physics;
using JigLibX.Geometry;
using JigLibX.Collision;
#endregion

namespace JigLibX.Vehicles
{
    public class Wheel
    {
        #region private fields
        private Car car;

        /// local mount position
        private Vector3 pos;
        private Vector3 axisUp;
        private float spring;
        private float travel;
        private float inertia;
        private float radius;
        private float sideFriction;
        private float fwdFriction;
        private float damping;
        int numRays;

        // things that change 
        private float angVel;
        private float steerAngle;
        private float torque;
        private float driveTorque;
        private float axisAngle;
        private float displacement; // = mTravel when fully compressed
        private float upSpeed; // speed relative to the car
        private bool locked;
        // last frame stuff
        private float lastDisplacement;
        private bool lastOnFloor;

        /// used to estimate the friction
        private float angVelForGrip;
        #endregion

        public void Setup(Car car,
              Vector3 pos, ///< position relative to car, in car's space
              Vector3 axisUp, ///< in car's space
              float spring,  ///< force per suspension offset
              float travel,  ///< suspension travel upwards
              float inertia, ///< inertia about the axel
              float radius,
              float sideFriction,
              float fwdFriction,
              float damping,
              int numRays)
        {
            this.car = car;
            this.pos = pos;
            this.axisUp = axisUp;
            this.spring = spring;
            this.travel = travel;
            this.inertia = inertia;
            this.radius = radius;
            this.sideFriction = sideFriction;
            this.fwdFriction = fwdFriction;
            this.damping = damping;
            this.numRays = numRays;

            pred = new WheelPred(car.Chassis.Body.CollisionSkin);

            Reset();
        }

        /// <summary>
        /// sets everything that varies to a default
        /// </summary>
        public void Reset()
        {
            angVel = 0.0f;
            steerAngle = 0.0f;
            torque = 0.0f;
            driveTorque = 0.0f;
            axisAngle = 0.0f;
            displacement = 0.0f;
            upSpeed = 0.0f;
            locked = false;
            lastDisplacement = 0.0f;
            lastOnFloor = false;
            angVelForGrip = 0.0f;

        }

        // do a number of rays, and choose the deepest penetration
        const int maxNumRays = 32;
        float[] fracs = new float[maxNumRays];
        CollisionSkin[] otherSkins = new CollisionSkin[maxNumRays];
        Vector3[] groundPositions = new Vector3[maxNumRays];
        Vector3[] groundNormals = new Vector3[maxNumRays];
        Segment[] segments = new Segment[maxNumRays];
        WheelPred pred;

        /// <summary> // TODO teting testing testing ...
        /// Adds the forces die to this wheel to the parent. Return value indicates if it's
        /// on the ground.
        /// </summary>
        /// <param name="dt"></param>
        public bool AddForcesToCar(float dt)
        {
            
            Vector3 force = Vector3.Zero;
            lastDisplacement = displacement;
            displacement = 0.0f;

            Body carBody = car.Chassis.Body;

            Vector3 worldPos = carBody.Position + Vector3.Transform(pos, carBody.Orientation);// *mPos;
            Vector3 worldAxis = Vector3.Transform(axisUp, carBody.Orientation);// *mAxisUp;
            
            //Vector3 wheelFwd = RotationMatrix(mSteerAngle, worldAxis) * carBody.Orientation.GetCol(0);
            // OpenGl has differnet row/column order for matrixes than XNA has ..
            Vector3 wheelFwd = Vector3.Transform(carBody.Orientation.Right, JiggleMath.RotationMatrix(steerAngle, worldAxis));
            //Vector3 wheelFwd = RotationMatrix(mSteerAngle, worldAxis) * carBody.GetOrientation().GetCol(0);
            Vector3 wheelUp = worldAxis;
            Vector3 wheelLeft = Vector3.Cross(wheelUp, wheelFwd);
            wheelLeft.Normalize();

            wheelUp = Vector3.Cross(wheelFwd, wheelLeft);

            // start of ray
            float rayLen = 2.0f * radius + travel;
            Vector3 wheelRayEnd = worldPos - radius * worldAxis;
            Segment wheelRay = new Segment(wheelRayEnd + rayLen * worldAxis, -rayLen * worldAxis);

            //Assert(PhysicsSystem.CurrentPhysicsSystem);
            CollisionSystem collSystem = PhysicsSystem.CurrentPhysicsSystem.CollisionSystem;

            ///Assert(collSystem);
            int numRaysUse = System.Math.Min(numRays, maxNumRays);

            // adjust the start position of the ray - divide the wheel into numRays+2 
            // rays, but don't use the first/last.
            float deltaFwd = (2.0f * radius) / (numRaysUse + 1);
            float deltaFwdStart = deltaFwd;


            lastOnFloor = false;
            int bestIRay = 0;
            int iRay;

            for (iRay = 0; iRay < numRaysUse; ++iRay)
            {
                fracs[iRay] = float.MaxValue; //SCALAR_HUGE;
                // work out the offset relative to the middle ray
                float distFwd = (deltaFwdStart + iRay * deltaFwd) - radius;
                //float zOffset = mRadius * (1.0f - CosDeg(90.0f * (distFwd / mRadius)));
                float zOffset = radius * (1.0f - (float)System.Math.Cos( MathHelper.ToRadians( 90.0f * (distFwd / radius))));

                segments[iRay] = wheelRay;
                segments[iRay].Origin += distFwd * wheelFwd + zOffset * wheelUp;

                if (collSystem.SegmentIntersect(out fracs[iRay], out otherSkins[iRay],
                                                 out groundPositions[iRay], out groundNormals[iRay], segments[iRay], pred))
                {
                    lastOnFloor = true;

                    if (fracs[iRay] < fracs[bestIRay])
                        bestIRay = iRay;
                }
            }

            
            if (!lastOnFloor)
                return false;

            //Assert(bestIRay < numRays);

            // use the best one
            Vector3 groundPos = groundPositions[bestIRay];
            float frac = fracs[bestIRay];
            CollisionSkin otherSkin = otherSkins[bestIRay];

            //  const Vector3 groundNormal = (worldPos - segments[bestIRay].GetEnd()).NormaliseSafe();
            //  const Vector3 groundNormal = groundNormals[bestIRay];

            Vector3 groundNormal = worldAxis;

            if (numRaysUse > 1)
            {
                for (iRay = 0; iRay < numRaysUse; ++iRay)
                {
                    if (fracs[iRay] <= 1.0f)
                    {
                        groundNormal += (1.0f - fracs[iRay]) * (worldPos - segments[iRay].GetEnd());
                    }
                }

                JiggleMath.NormalizeSafe(ref groundNormal);
            }
            else
            {
                groundNormal = groundNormals[bestIRay];
            }

            //Assert(otherSkin);
            Body worldBody = otherSkin.Owner;

            displacement = rayLen * (1.0f - frac);
            displacement = MathHelper.Clamp(displacement, 0, travel);

            float displacementForceMag = displacement * spring;

            // reduce force when suspension is par to ground
            displacementForceMag *= Vector3.Dot(groundNormals[bestIRay], worldAxis);

            // apply damping
            float dampingForceMag = upSpeed * damping;

            float totalForceMag = displacementForceMag + dampingForceMag;

            if (totalForceMag < 0.0f) totalForceMag = 0.0f;

            Vector3 extraForce = totalForceMag * worldAxis;

            force += extraForce;

            // side-slip friction and drive force. Work out wheel- and floor-relative coordinate frame
            Vector3 groundUp = groundNormal;
            Vector3 groundLeft = Vector3.Cross(groundNormal, wheelFwd);
            JiggleMath.NormalizeSafe(ref groundLeft);

            Vector3 groundFwd = Vector3.Cross(groundLeft, groundUp);

            Vector3 wheelPointVel = carBody.Velocity +
             Vector3.Cross(carBody.AngularVelocity, Vector3.Transform( pos, carBody.Orientation));// * mPos);

            Vector3 rimVel = angVel * Vector3.Cross(wheelLeft, groundPos - worldPos);
            wheelPointVel += rimVel;

            // if sitting on another body then adjust for its velocity.
            if (worldBody != null)
            {
                Vector3 worldVel = worldBody.Velocity +
                 Vector3.Cross(worldBody.AngularVelocity, groundPos - worldBody.Position);

                wheelPointVel -= worldVel;
            }

            // sideways forces
            float noslipVel = 0.2f;
            float slipVel = 0.4f;
            float slipFactor = 0.7f;

            float smallVel = 3;
            float friction = sideFriction;

            float sideVel = Vector3.Dot(wheelPointVel, groundLeft);

            if ((sideVel > slipVel) || (sideVel < -slipVel))
                friction *= slipFactor;
            else
                if ((sideVel > noslipVel) || (sideVel < -noslipVel))
                    friction *= 1.0f - (1.0f - slipFactor) * (System.Math.Abs(sideVel) - noslipVel) / (slipVel - noslipVel);

            if (sideVel < 0.0f)
                friction *= -1.0f;

            if (System.Math.Abs(sideVel) < smallVel)
                friction *= System.Math.Abs(sideVel) / smallVel;

            float sideForce = -friction * totalForceMag;

            extraForce = sideForce * groundLeft;
            force += extraForce;

            // fwd/back forces
            friction = fwdFriction;
            float fwdVel = Vector3.Dot(wheelPointVel, groundFwd);

            if ((fwdVel > slipVel) || (fwdVel < -slipVel))
                friction *= slipFactor;
            else
                if ((fwdVel > noslipVel) || (fwdVel < -noslipVel))
                    friction *= 1.0f - (1.0f - slipFactor) * (System.Math.Abs(fwdVel) - noslipVel) / (slipVel - noslipVel);

            if (fwdVel < 0.0f)
                friction *= -1.0f;

            if (System.Math.Abs(fwdVel) < smallVel)
                friction *= System.Math.Abs(fwdVel) / smallVel;

            float fwdForce = -friction * totalForceMag;

            extraForce = fwdForce * groundFwd;
            force += extraForce;

            //if (!force.IsSensible())
            //{
            //  TRACE_FILE_IF(ONCE_1)
            //    TRACE("Bad force in car wheel\n");
            //  return true;
            //}

            // fwd force also spins the wheel
            Vector3 wheelCentreVel = carBody.Velocity +
             Vector3.Cross(carBody.AngularVelocity, Vector3.Transform(pos, carBody.Orientation));// * mPos);

            angVelForGrip = Vector3.Dot(wheelCentreVel, groundFwd) / radius;
            torque += -fwdForce * radius;

            // add force to car
            carBody.AddWorldForce(force, groundPos);

            //if (float.IsNaN(force.X))
            //    while(true){}
            //System.Diagnostics.Debug.WriteLine(force.ToString());

            // add force to the world
            if (worldBody != null && !worldBody.Immovable)
            {
                // todo get the position in the right place...
                // also limit the velocity that this force can produce by looking at the 
                // mass/inertia of the other object
                float maxOtherBodyAcc = 500.0f;
                float maxOtherBodyForce = maxOtherBodyAcc * worldBody.Mass;

                if (force.LengthSquared() > (maxOtherBodyForce * maxOtherBodyForce))
                    force *= maxOtherBodyForce / force.Length();

                worldBody.AddWorldForce(-force, groundPos);
            }
            return true;
        }

        /// <summary>
        /// Updates the rotational state etc
        /// </summary>
        /// <param name="dt"></param>
        public void Update(float dt)
        {
            if (dt <= 0.0f)
                return;

            float origAngVel = angVel;
            upSpeed = (displacement - lastDisplacement) / System.Math.Max(dt, JiggleMath.Epsilon);

            if (locked)
            {
                angVel = 0;
                torque = 0;
            }
            else
            {
                angVel += torque * dt / inertia;
                torque = 0;

                // prevent friction from reversing dir - todo do this better
                // by limiting the torque
                if (((origAngVel > angVelForGrip) && (angVel < angVelForGrip)) ||
                     ((origAngVel < angVelForGrip) && (angVel > angVelForGrip)))
                    angVel = angVelForGrip;

                angVel += driveTorque * dt / inertia;
                driveTorque = 0;

                float maxAngVel = 200;
                angVel = MathHelper.Clamp(angVel, -maxAngVel, maxAngVel);

                axisAngle += MathHelper.ToDegrees(dt * angVel);
            }
        }

        /// <summary>
        /// get steering angle in degrees
        /// </summary>
        public float SteerAngle
        {
            get { return steerAngle; }
            set { steerAngle = value; }
        }

        /// <summary>
        /// lock/unlock the wheel
        /// </summary>
        public bool Lock
        {
            get { return locked; }
            set { locked = value; }
        }

        /// <summary>
        /// power
        /// </summary>
        /// <param name="torque"></param>
        public void AddTorque(float torque)
        {
            driveTorque += torque;
        }

        /// <summary>
        /// the basic origin position
        /// </summary>
        public Vector3 Pos
        {
            get { return pos; }
        }

        /// <summary>
        /// the suspension axis in the car's frame
        /// </summary>
        public Vector3 LocalAxisUp
        {
            get { return axisUp; }
        }

        /// <summary>
        /// wheel radius
        /// </summary>
        public float Radius
        {
            get { return radius; }
        }

        /// <summary>
        /// the displacement along our up axis
        /// </summary>
        public float Displacement
        {
            get { return displacement; }
        }

        public float AxisAngle
        {
            get { return axisAngle; }
        }



        public bool OnFloor
        {
            get { return lastOnFloor; }
        }
    }

    /// Predicate for the wheel->world intersection test
    class WheelPred : CollisionSkinPredicate1
    {
        CollisionSkin mSkin;

        public  WheelPred(CollisionSkin carSkin)
        {
            mSkin = carSkin;
        }

        public override bool ConsiderSkin(CollisionSkin skin)
        {
            return (skin.ID != mSkin.ID);
        }
    }

}
