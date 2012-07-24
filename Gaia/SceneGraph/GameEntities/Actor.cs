using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using JigLibX.Physics;
using JigLibX.Collision;
using JigLibX.Geometry;
using JigLibX.Vehicles;

using Gaia.Core;
using Gaia.Physics;

namespace Gaia.SceneGraph.GameEntities
{
    public class Actor : Entity
    {
        const float MAX_HEALTH = 100;
        const float MAX_ENERGY = 100;

        protected float health = MAX_HEALTH;

        protected float energy = MAX_ENERGY;

        protected float energyRechargeRate = 15;

        protected float sprintEnergyCost = 20;

        protected float sprintSpeedBoost = 5.5f;

        protected bool isCrouching = false;

        protected int team = 0;

        public int GetTeam()
        {
            return team;
        }

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

        protected CharacterBody body;

        protected CollisionSkin collision;

        protected Capsule standCapsule;
        protected Capsule crouchCapsule;

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);

            scene.AddActor(this);

            PhysicsSystem world = scene.GetPhysicsEngine();
            
            Vector3 pos = Vector3.Up * 256 + 15*(new Vector3((float)RandomHelper.RandomGen.NextDouble(), (float)RandomHelper.RandomGen.NextDouble(), (float)RandomHelper.RandomGen.NextDouble())*2-Vector3.One);
            Vector3 normal = Vector3.Up;
            scene.MainTerrain.GenerateRandomTransform(RandomHelper.RandomGen, out pos, out normal);
            //pos = pos + Vector3.Up * 5;

            body = new CharacterBody();
            collision = new CollisionSkin(body);

            standCapsule = new Capsule(Vector3.Zero, Matrix.CreateRotationX(MathHelper.PiOver2), 1.0f, 1.778f);
            crouchCapsule = new Capsule(Vector3.Zero, Matrix.CreateRotationX(MathHelper.PiOver2), 1.0f, 1.0f);
            SetupPosture(false);
            collision.AddPrimitive(standCapsule, (int)MaterialTable.MaterialID.NormalRough);
            body.CollisionSkin = collision;
            Vector3 com = PhysicsHelper.SetMass(75.0f, body, collision);

            body.MoveTo(pos + com, Matrix.Identity);
            collision.ApplyLocalTransform(new JigLibX.Math.Transform(-com, Matrix.Identity));

            body.SetBodyInvInertia(0.0f, 0.0f, 0.0f);

            body.AllowFreezing = false;
            body.EnableBody();
            Transformation = new Transform(body);

            ResetState();
        }

        protected void SetupPosture(bool crouching)
        {
            collision.RemoveAllPrimitives();
            Capsule currCapsule = (crouching) ? crouchCapsule : standCapsule;
            collision.AddPrimitive(currCapsule, (int)MaterialTable.MaterialID.NormalRough);
        }

        protected virtual void UpdateState()
        {
            energy += Time.GameTime.ElapsedTime * energyRechargeRate;
        }

        protected virtual void OnDeath()
        {

        }

        protected virtual void ResetState()
        {

        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            UpdateState();
        }
    }
}
