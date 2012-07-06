#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using JigLibX.Collision;
using JigLibX.Physics;
using JigLibX.Geometry;
#endregion

namespace JigLibX.Vehicles
{
    /// <summary>
    /// basic rigid body to represent a single chassis - at the moment 
    /// no moving components. You can inherit from this and pass your 
    /// own version to Car (TODO - tidy up this)
    /// </summary>
    public class Chassis
    {
        private ChassisBody body;
        private CollisionSkin collisionSkin;

        private Vector3 dimsMin;
        private Vector3 dimsMax;

        public Chassis(Car car)
        {
            body = new ChassisBody(car);
            collisionSkin = new CollisionSkin(body);

            body.CollisionSkin = collisionSkin;

            float length = 6.0f;
            float width = 2.3f;
            float height = 1.6f;

            Vector3 min = new Vector3(-0.5f * length, 0.0f, -width * 0.5f);
            Vector3 max = new Vector3(0.5f * length, height, width * 0.5f);

            SetDims(min, max);
        }

        /// <summary>
        /// Set the dimensions of the chassis, specified by the extreme corner points.
        /// This will also call Car.SetupDefaultWheels();
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public virtual void SetDims(Vector3 min, Vector3 max)
        {
            dimsMin = min;
            dimsMax = max;
            Vector3 sides = max - min;

            // ratio of top/bottom depths
            float topBotRatio = 0.4f;

            // the bottom box
            Vector3 max1 = max;
            max1.Y -= topBotRatio * sides.Y;
            Box box1 = new Box(min, Matrix.Identity, max1 - min);

            // the top box
            Vector3 min2 = min;
            min2.Y += topBotRatio * sides.Y;
            Vector3 max2 = max;
            min2.X += sides.X * 0.05f;
            max2.X -= sides.X * 0.3f;
            min2.Z *= 0.9f;
            max2.Z *= 0.9f;

            Box box2 = new Box(min2, Matrix.Identity, max2 - min2);

            collisionSkin.RemoveAllPrimitives();
            collisionSkin.AddPrimitive(box1, new MaterialProperties(0.3f, 0.5f, 0.3f));
            collisionSkin.AddPrimitive(box2, new MaterialProperties(0.3f, 0.5f, 0.3f));

            body.Car.SetupDefaultWheels();
        }

        /// <summary>
        /// Set the dimensions of the chassis, specified by the extreme corner points.
        /// This will also call Car.SetupDefaultWheels();
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public virtual void GetDims(out Vector3 min, out Vector3 max)
        {
            min = dimsMin;
            max = dimsMax;
        }



        /// <summary>
        /// Register with physics
        /// </summary>
        public void EnableChassis()
        {
            body.EnableBody();
        }

        /// <summary>
        /// remove from the physics system
        /// </summary>
        public void DisableChassis()
        {
            body.DisableBody();
        }

        public Body Body
        {
            get { return body; }
        }

        public CollisionSkin Skin
        {
            get { return collisionSkin; }
        }

    }

    /// <summary>
    /// extend tBody to allow for adding on car-specific forces (e.g.
    /// wheel/drive forces) - i.e. when we get asked to add on forces
    /// give the car the opportunity to do stuff
    /// </summary>
    public class ChassisBody : Body
    {
        private Car mCar;

        public ChassisBody(Car car)
        {
            mCar = car;
        }

        /// inherited from tBody
        public override void AddExternalForces(float dt)
        {
            if (mCar == null)
                return;

            ClearForces();
            AddGravityToExternalForce();
            mCar.AddExternalForces(dt);

        }
        public override void PostPhysics(float dt)
        {
            if (mCar == null)
                return;

            mCar.PostPhysics(dt);

        }

        public Car Car
        {
            get { return mCar; }
        }


    }
}
