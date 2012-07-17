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
using Gaia.Game;

namespace Gaia.SceneGraph.GameEntities
{
    public class Player : Entity
    {
        const float MAX_HEALTH = 100;
        const float MAX_ENERGY = 100;
        
        protected bool isEnabled = false;
        protected bool isControllable = false;

        protected float health = MAX_HEALTH;

        protected float energy = MAX_ENERGY;

        protected float energyRechargeRate = 15;

        protected float sprintEnergyCost = 20;

        protected float sprintSpeedBoost = 5.5f;

        protected bool isCrouching = false;

        CharacterBody body;

        CollisionSkin collision;

        Capsule standCapsule;
        Capsule crouchCapsule;

        Camera camera;
        Weapon gun;

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

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);

            PhysicsSystem world = scene.GetPhysicsEngine();
            Vector3 pos = Vector3.Up * 256;
            Vector3 normal = Vector3.Up;
            scene.MainTerrain.GenerateRandomTransform(RandomHelper.RandomGen, out pos, out normal);
            pos = pos + Vector3.Up * 5;

            body = new CharacterBody();
            collision = new CollisionSkin(body);

            standCapsule = new Capsule(Vector3.Zero, Matrix.CreateRotationX(MathHelper.PiOver2), 1.0f, 1.778f);
            crouchCapsule = new Capsule(Vector3.Zero, Matrix.CreateRotationX(MathHelper.PiOver2), 1.0f, 1.0f);
            SetupPosture(isCrouching);
            collision.AddPrimitive(standCapsule, (int)MaterialTable.MaterialID.NormalRough);
            body.CollisionSkin = collision;
            Vector3 com = PhysicsHelper.SetMass(75.0f, body, collision);

            body.MoveTo(pos + com, Matrix.Identity);
            collision.ApplyLocalTransform(new JigLibX.Math.Transform(-com, Matrix.Identity));

            body.SetBodyInvInertia(0.0f, 0.0f, 0.0f);

            body.AllowFreezing = false;
            body.EnableBody();
            Transformation = new Transform(body);

            camera = scene.MainPlayer;

            gun = new Weapon("P90_V", camera.Transformation, scene);
        }

        void SetupPosture(bool crouching)
        {           
            collision.RemoveAllPrimitives();
            Capsule currCapsule = (crouching) ? crouchCapsule : standCapsule;
            collision.AddPrimitive(currCapsule, (int)MaterialTable.MaterialID.NormalRough);
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
        }
        public void SetControllable(bool controllable)
        {
            isControllable = controllable;
        }

        void UpdateControls()
        {
            if (isControllable)
            {
                Vector3 velocity = Vector3.Zero;
                Vector3 rot = scene.MainPlayer.Transformation.GetRotation();
                Matrix transform = Matrix.CreateRotationY(rot.Y);
                if (InputManager.Inst.IsKeyDown(GameKey.MoveFoward))
                {
                    velocity += transform.Forward;
                }
                if (InputManager.Inst.IsKeyDown(GameKey.MoveBackward))
                {
                    velocity -= transform.Forward;
                }
                if (InputManager.Inst.IsKeyDown(GameKey.MoveLeft))
                {
                    velocity -= transform.Right;
                }
                if (InputManager.Inst.IsKeyDown(GameKey.MoveRight))
                {
                    velocity += transform.Right;
                }
                if (InputManager.Inst.IsKeyDownOnce(GameKey.Jump))
                {
                    body.Jump(12.5f);
                }
                if (InputManager.Inst.IsKeyDownOnce(GameKey.Crouch))
                {
                    SetupPosture(true);
                }
                if (InputManager.Inst.IsKeyUpOnce(GameKey.Crouch))
                {
                    SetupPosture(false);
                }

                float sprintCoeff = 0;
                if (InputManager.Inst.IsKeyDown(GameKey.Sprint) && velocity.Length() > 0.001f)
                {
                    energy -= Time.GameTime.ElapsedTime * sprintEnergyCost;
                    sprintCoeff = sprintSpeedBoost * MathHelper.Clamp(energy, 0, 1);
                }

                body.DesiredVelocity = velocity * (7.5f + sprintCoeff);

                if (InputManager.Inst.IsLeftMouseDown())
                {
                    gun.OnFire(camera.Transformation.GetPosition(), camera.Transformation.GetTransform().Forward);
                }
            }
        }

        void UpdateState()
        {
            energy += Time.GameTime.ElapsedTime * energyRechargeRate;
        }

        public override void OnUpdate()
        {
            if (isEnabled)
            {
                camera.Transformation.SetPosition(this.Transformation.GetPosition() + Vector3.Up * this.standCapsule.Length * 0.5f);
            }

            gun.OnUpdate();

            UpdateControls();

            UpdateState();
           
            base.OnUpdate();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            base.OnRender(view);
            gun.OnRender(view);   
        }
    }
}
