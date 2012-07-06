using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.Resources;
using Gaia.Core;

namespace Gaia.SceneGraph.GameEntities
{
    public class ParticleEmitter : Entity
    {
        int particleCount = 3000;

        public RenderTarget2D sizeData;
        public Texture2D positionData;
        public Texture2D velocityData;
        public RenderTarget2D colorData;

        public RenderTarget2D positionTarget;
        public RenderTarget2D velocityTarget;

        public bool EmitOnce = false;

        float LifeTime = 0;

        ParticleEffect particleEffect;

        public ParticleEffect GetParticleEffect()
        {
            return particleEffect;
        }

        public int GetParticleCount()
        {
            return particleCount;
        }

        public int GetTextureSize()
        {
            return textureSize;
        }

        public Scene GetScene()
        {
            return scene;
        }

        int textureSize;

        Vector3 particleColor = Vector3.One;

        bool useCustomColor = false;

        public Vector3 GetParticleColor()
        {
            return (useCustomColor) ? particleColor : particleEffect.color;
        }

        public ParticleEmitter(ParticleEffect particleEffect, int particleCount)
            : base()
        {
            this.particleEffect = particleEffect;
            this.particleCount = particleCount;
            ComputeTextures();
        }

        void ComputeTextures()
        {
            textureSize = (int)Math.Sqrt(particleCount - (particleCount % 2)) + 1;
            positionTarget = new RenderTarget2D(GFX.Device, textureSize, textureSize, 1, SurfaceFormat.Vector4);
            velocityTarget = new RenderTarget2D(GFX.Device, textureSize, textureSize, 1, SurfaceFormat.Vector4);
            colorData = new RenderTarget2D(GFX.Device, textureSize, textureSize, 1, SurfaceFormat.Color);
            sizeData = new RenderTarget2D(GFX.Device, textureSize, textureSize, 1, SurfaceFormat.Single);
        }

        public void SetColor(Vector3 color)
        {
            particleColor = color;
            useCustomColor = true;
        }

        public override void OnAdd(Scene scene)
        {
            GFX.Inst.particleSystem.AddEmitter(this);
            base.OnAdd(scene);
        }

        public override void OnDestroy()
        {
            GFX.Inst.particleSystem.RemoveEmitter(this);
            positionTarget.Dispose();
            velocityTarget.Dispose();
            colorData.Dispose();
            sizeData.Dispose();
            base.OnDestroy();
        }

        public override void OnUpdate()
        {
            if (EmitOnce)
            {
                LifeTime += Time.GameTime.ElapsedTime;
                if (LifeTime > particleEffect.lifetime)
                {
                    scene.RemoveEntity(this);
                }
            }
            base.OnUpdate();
        }

        public override void OnRender(RenderView view)
        {
            ParticleElementManager particleMgr = (ParticleElementManager)view.GetRenderElementManager(RenderPass.Particles);
            if (particleMgr != null)
            {
                particleMgr.AddElement(particleEffect.material, this);
            }
            base.OnRender(view);
        }
    }
}
