using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using JigLibX.Physics;
using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Vehicles;

using Gaia.Input;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Voxels;
using Gaia.Rendering;
using Gaia.Physics;

namespace Gaia.SceneGraph.GameEntities
{
    public class Tank : Model
    {
        const float MAX_HEALTH = 100;
        const float WHEEL_SIZE = 1.20f;
        const float WHEEL_VEL_MAG = 0.25f;
        const float STEER_ACCEL_MAG = 80.3f;
        const float MAX_STEER_ANGLE = MathHelper.PiOver2 * 0.75f;
        const float MAX_CANNON_ANGLE = MathHelper.PiOver2 * 0.65f;
        const string TURRET_NODE_NAME = "turret";
        const string CANNON_NODE_NAME = "cannon";
        const string MUZZLE_NODE_NAME = "muzzle";
        const string CAMERA_ORBIT_NODE_NAME = "camera_orbit";
        const string CAMERA_FPS_NODE_NAME = "camera_fps";
        const string NAME_LABEL_NODE = "name_label";
        
        protected bool isEnabled = false;
        protected bool isControllable = false;

        float wheelAngle = 0;
        float steerAngle = 0;

        float turretAngle = 0;
        float cannonAngle = 0;
       
        string[] wheelNames = { "l_back_wheel", "l_front_wheel", "r_back_wheel", "r_front_wheel" };
        string[] steerNames = { "l_front_wheel", "l_steer", "r_front_wheel", "r_steer" };

        protected float health = MAX_HEALTH;

        protected bool isCrouching = false;

        CharacterBody body;

        CollisionSkin collision;

        //Car carBody;

        Capsule standCapsule;
        Capsule crouchCapsule;

        public bool IsDead()
        {
            return (health <= 0.0f);
        }

        public float GetHealth()
        {
            return health;
        }

        public float GetHealthPercent()
        {
            return health / MAX_HEALTH;
        }

        public Tank() : base("Tank")
        {

        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);

            PhysicsSystem world = scene.GetPhysicsEngine();
            Vector3 pos = Vector3.Up * 256;

            /*
            carBody = new Car(true, true, 70.0f, 5.0f, 4.7f, 5.0f, 0.20f, 0.4f, 0.05f, 0.45f, 0.3f, 6, 50.0f, world.Gravity.Length());
            carBody.EnableCar();
            carBody.Chassis.Body.MoveTo(pos, Matrix.Identity);
            carBody.Chassis.Skin.SetMaterialProperties(0,new MaterialProperties(0, 0.3f, 0.3f));
            carBody.Chassis.SetDims(this.mesh.GetBounds().Min, this.mesh.GetBounds().Max);
            world.AddBody(carBody.Chassis.Body);
            */
            body = new CharacterBody();
            collision = new CollisionSkin(body);

            standCapsule = new Capsule(Vector3.Zero, Matrix.CreateRotationX(MathHelper.PiOver2), 1.0f, 1.778f);
            crouchCapsule = new Capsule(Vector3.Zero, Matrix.CreateRotationX(MathHelper.PiOver2), 1.0f, 1.0f);
            SetupPosture(isCrouching);
            collision.AddPrimitive(standCapsule, (int)MaterialTable.MaterialID.NormalRough);
            //.Player);
            //collision.AddPrimitive(new Box(Vector3.Zero, Matrix.Identity, Vector3.One), (int)MaterialTable.MaterialID.NotBouncyNormal);
            body.CollisionSkin = collision;
            Vector3 com = PhysicsHelper.SetMass(75.0f, body, collision);

            body.MoveTo(pos + com, Matrix.Identity);
            collision.ApplyLocalTransform(new JigLibX.Math.Transform(-com, Matrix.Identity));

            body.SetBodyInvInertia(0.0f, 0.0f, 0.0f);

            body.AllowFreezing = false;
            body.EnableBody();
            Transformation = new Transform(body);
        }

        /*
        private void SetCarMass(float mass)
        {
            carBody.Chassis.Body.Mass = mass;
            Vector3 min, max;
            carBody.Chassis.GetDims(out min, out max);
            Vector3 sides = max - min;

            float Ixx = (1.0f / 12.0f) * mass * (sides.Y * sides.Y + sides.Z * sides.Z);
            float Iyy = (1.0f / 12.0f) * mass * (sides.X * sides.X + sides.Z * sides.Z);
            float Izz = (1.0f / 12.0f) * mass * (sides.X * sides.X + sides.Y * sides.Y);

            Matrix inertia = Matrix.Identity;
            inertia.M11 = Ixx; inertia.M22 = Iyy; inertia.M33 = Izz;
            carBody.Chassis.Body.BodyInertia = inertia;
            carBody.SetupDefaultWheels();
        }
        */

        void SetupPosture(bool crouching)
        {           
            collision.RemoveAllPrimitives();
            Capsule currCapsule = (crouching) ? crouchCapsule : standCapsule;
            collision.AddPrimitive(currCapsule, (int)MaterialTable.MaterialID.NormalRough);
            /*
            Vector3 com = PhysicsHelper.SetMass(75.0f, body, collision);
            JigLibX.Math.Transform transform = body.Transform;
            collision.SetNewTransform(ref transform);*/
            //currCapsule.Transform = new JigLibX.Math.Transform(body.Position + com, body.Orientation);
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
        public void SetControllable(bool controllable)
        {
            isControllable = controllable;
        }

        void UpdateAnimation()
        {
            Matrix transform = this.Transformation.GetTransform();
            for (int i = 0; i < wheelNames.Length; i++)
            {
                Vector3 rot = nodes[wheelNames[i]].Rotation;
                rot.X = wheelAngle;
                nodes[wheelNames[i]].Rotation = rot;
            }
            for (int i = 0; i < steerNames.Length; i++)
            {
                nodes[steerNames[i]].Rotation.Y = steerAngle;
            }
            nodes[TURRET_NODE_NAME].Rotation.Y = turretAngle;
            nodes[CANNON_NODE_NAME].Rotation.X = cannonAngle;
            for (int i = 0; i < rootNodes.Length; i++)
            {
                rootNodes[i].ApplyTransform(ref transform);
            }
        }

        public override void OnUpdate()
        {
            Vector3 steerAccel = Vector3.Zero;
            
            if (isControllable)
            {
                float steer = 0.0f;
                float accelerate = 0.0f;

                float wheelVel = 0;
                Vector3 velocity = Vector3.Zero;
                Matrix mat = Transformation.GetTransform();
                if (InputManager.Inst.IsKeyDown(GameKey.MoveFoward))
                {
                    velocity += Vector3.Forward;
                    accelerate = 1.0f;
                    wheelVel += WHEEL_VEL_MAG * MathHelper.Min(1.0f, InputManager.Inst.GetPressTime(GameKey.MoveFoward));
                }
                if (InputManager.Inst.IsKeyDown(GameKey.MoveBackward))
                {
                    velocity -= Vector3.Forward;
                    accelerate = -1;
                    wheelVel -= WHEEL_VEL_MAG * MathHelper.Min(1.0f, InputManager.Inst.GetPressTime(GameKey.MoveBackward));
                }

                //moveAccel += wheelVel*WHEEL_SIZE*this.Transformation.GetTransform().Forward;
                wheelAngle += wheelVel;

                if (InputManager.Inst.IsKeyDown(GameKey.Fire))
                {
                    turretAngle += InputManager.Inst.GetMouseDisplacement().X;
                    cannonAngle += InputManager.Inst.GetMouseDisplacement().Y;
                }
                
                if (turretAngle >= MathHelper.TwoPi)
                    turretAngle -= MathHelper.TwoPi;
                if (turretAngle < 0)
                    turretAngle += MathHelper.TwoPi;

                cannonAngle = MathHelper.Clamp(cannonAngle, -MAX_CANNON_ANGLE, MAX_CANNON_ANGLE);

                if (InputManager.Inst.IsKeyDown(GameKey.MoveLeft))
                {
                    velocity -= Vector3.Right;
                    steer = 1;
                    //moveAccel -= this.Transformation.GetTransform().Right*WHEEL_VEL_MAG*WHEEL_SIZE;
                    steerAccel.Y += STEER_ACCEL_MAG * MathHelper.Min(1.0f, InputManager.Inst.GetPressTime(GameKey.MoveLeft));
                }
                if (InputManager.Inst.IsKeyDown(GameKey.MoveRight))
                {
                    velocity += Vector3.Right;
                    steer = -1;
                    //moveAccel += this.Transformation.GetTransform().Right * WHEEL_VEL_MAG * WHEEL_SIZE;
                    steerAccel.Y -= STEER_ACCEL_MAG * MathHelper.Min(1.0f, InputManager.Inst.GetPressTime(GameKey.MoveRight));
                }

                if (InputManager.Inst.IsKeyDown(GameKey.Jump))
                {
                    body.Jump(8.5f);
                }
                if (InputManager.Inst.IsKeyDownOnce(GameKey.Crouch))
                {
                    SetupPosture(true);
                }
                if (InputManager.Inst.IsKeyUpOnce(GameKey.Crouch))
                {
                    SetupPosture(false);
                }
                body.DesiredVelocity = velocity * 7.5f;
            }

            UpdateAnimation();

            //if (isEnabled)
            {
                scene.MainPlayer.SetTarget(nodes[MUZZLE_NODE_NAME].Transform.Translation, true);
                AnimationNode cameraNode = (InputManager.Inst.IsRightMouseDown() && isControllable) ? nodes[CAMERA_FPS_NODE_NAME] : nodes[CAMERA_ORBIT_NODE_NAME];
                scene.MainPlayer.Transformation.SetPosition(cameraNode.Transform.Translation);
            }

            base.OnUpdate();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            base.OnRender(view);                
        }
    }
}
