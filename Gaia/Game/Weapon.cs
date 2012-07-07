﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;

using JigLibX.Collision;
using JigLibX.Geometry;

namespace Gaia.Game
{
    public class Weapon
    {
        Scene scene;
        ViewModel fpsModel;
        float coolDownTimeRemaining = 0;

        public Weapon(string modelName, Transform transform, Scene scene)
        {
            this.scene = scene;
            this.fpsModel = new ViewModel(modelName);
            fpsModel.SetTransform(transform);
        }

        public void OnUpdate(Transform transform)
        {
            fpsModel.OnUpdate();

            if(coolDownTimeRemaining > 0)
                coolDownTimeRemaining -= Time.GameTime.ElapsedTime;
        }

        public void OnFire(Vector3 muzzlePosition, Vector3 muzzleDir)
        {
            if (coolDownTimeRemaining <= 0)
            {
                coolDownTimeRemaining = 0.18f;
                Vector3 ray = Vector3.Zero;
                float dist;
                CollisionSkin skin;
                Vector3 pos, normal;

                Segment seg = new Segment(muzzlePosition, muzzleDir * 200);

                scene.GetPhysicsEngine().CollisionSystem.SegmentIntersect(out dist, out skin, out pos, out normal, seg, null);
                if (skin != null)
                {
                    Decal decal = new Decal();
                    decal.SetMaterial("BulletMetal1");
                    decal.Normal = normal;
                    decal.Scale = new Vector2(0.25f, 0.25f);
                    decal.Transformation.SetPosition(pos);
                    decal.IsPersistent = false;
                    scene.AddEntity("bullet", decal);
                    ParticleEffect bulletEffect = ResourceManager.Inst.GetParticleEffect("BulletEffect");
                    ParticleEmitter collideEmitter = new ParticleEmitter(bulletEffect, 16);
                    collideEmitter.EmitOnce = true;
                    NormalTransform newTransform = new NormalTransform();
                    newTransform.ConformToNormal(normal);
                    newTransform.SetPosition(pos);
                    collideEmitter.Transformation = newTransform;
                    scene.AddEntity("bulletEmitter", collideEmitter);
                }
            }
        }

        public void OnRender(RenderView view)
        {
            fpsModel.OnRender(view);
        }
    }
}