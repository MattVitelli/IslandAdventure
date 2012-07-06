using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.SceneGraph.GameEntities;
using Gaia.Rendering.RenderViews;

namespace Gaia.Rendering.Simulators
{
    public class ParticleSimulator
    {
        List<ParticleEmitter> emitters = new List<ParticleEmitter>();

        Shader updatePhysicsShader;
        Shader updateColorShader;
        Shader updateSizeShader;

        Texture2D[] randomTextures;

        Random rand;

        public ParticleSimulator()
        {
            rand = new Random();
            randomTextures = new Texture2D[3];
            int randSize = 64;
            float[] randData = new float[randSize * randSize];
            for (int i = 0; i < randomTextures.Length; i++)
            {
                randomTextures[i] = new Texture2D(GFX.Device, randSize, randSize, 1, TextureUsage.None, SurfaceFormat.Single);
                for (int j = 0; j < randData.Length; j++)
                {
                    randData[j] = (float)(rand.NextDouble() * 2.0 - 1.0);
                }
                randomTextures[i].SetData<float>(randData);
            }

            updatePhysicsShader = new Shader();
            updatePhysicsShader.VSTarget = 2;
            updatePhysicsShader.PSTarget = 3;
            updatePhysicsShader.CompileFromFiles("Shaders/Simulation/ParticlePhysicsP.hlsl", "Shaders/Simulation/ParticlesV.hlsl");

            updateColorShader = new Shader();
            updateColorShader.VSTarget = 2;
            updateColorShader.PSTarget = 3;
            updateColorShader.CompileFromFiles("Shaders/Simulation/ParticleColorsP.hlsl", "Shaders/Simulation/ParticlesV.hlsl");

            updateSizeShader = new Shader();
            updateSizeShader.VSTarget = 2;
            updateSizeShader.PSTarget = 3;
            updateSizeShader.CompileFromFiles("Shaders/Simulation/ParticleSizeP.hlsl", "Shaders/Simulation/ParticlesV.hlsl");
            
        }

        public void AddEmitter(ParticleEmitter emitter)
        {
            emitters.Add(emitter);
            InitializeEmitter(emitter);
        }

        public void RemoveEmitter(ParticleEmitter emitter)
        {
            emitters.Remove(emitter);
        }

        void InitializeEmitter(ParticleEmitter emitter)
        {
            emitter.positionData = new Texture2D(GFX.Device, emitter.GetTextureSize(), emitter.GetTextureSize(), 1, TextureUsage.None, SurfaceFormat.Vector4);
            emitter.velocityData = new Texture2D(GFX.Device, emitter.GetTextureSize(), emitter.GetTextureSize(), 1, TextureUsage.None, SurfaceFormat.Vector4);
                       
            ParticleEffect effect = emitter.GetParticleEffect();

            int sizeSquared = emitter.GetTextureSize() * emitter.GetTextureSize();

            Vector3 emitPos = emitter.Transformation.GetPosition();

            Vector4[] initPosition = new Vector4[sizeSquared];
            for (int i = 0; i < initPosition.Length; i++)
            {
                Vector3 offset = new Vector3(effect.offsetParameters.X, effect.offsetParameters.Y, effect.offsetParameters.Z) * new Vector3((float)(rand.NextDouble() * 2.0 - 1.0), (float)(rand.NextDouble() * 2.0 - 1.0), (float)(rand.NextDouble() * 2.0 - 1.0));
                offset *= effect.offsetParameters.W;
                offset = Vector3.TransformNormal(offset, emitter.Transformation.GetTransform());
                initPosition[i] = new Vector4(emitPos + offset, effect.lifetime * (float)rand.NextDouble());
                
            }

            Vector4[] initVelocity = new Vector4[sizeSquared];
            for (int i = 0; i < initVelocity.Length; i++)
            {
                Vector3 randVel = effect.initialDirection * effect.initialSpeed + Vector3.One * effect.initialSpeedVariance * (float)(rand.NextDouble() * 2.0 - 1.0);
                randVel = Vector3.TransformNormal(randVel, emitter.Transformation.GetTransform());
                float randMass = effect.mass + effect.massVariance * (float)(rand.NextDouble() * 2.0 - 1.0);
                initVelocity[i] = new Vector4(randVel, randMass);
            }

            emitter.positionData.SetData<Vector4>(initPosition);
            emitter.velocityData.SetData<Vector4>(initVelocity);

        }

        public void AdvanceSimulation(float timeDT)
        {
            updatePhysicsShader.SetupShader();

            Vector3[] particleForces = new Vector3[GFXShaderConstants.MAX_PARTICLEFORCES];
            particleForces[0] = new Vector3(0,-1,0)*9.821765f;
            GFX.Inst.SetTextureFilter(0, TextureFilter.Point);
            GFX.Inst.SetTextureFilter(1, TextureFilter.Point);
            GFX.Inst.SetTextureFilter(2, TextureFilter.Point);
            GFX.Inst.SetTextureFilter(3, TextureFilter.Point);

            for (int i = 0; i < randomTextures.Length; i++)
            {
                GFX.Device.Textures[4 + i] = randomTextures[i];
            }
            if (emitters.Count > 0)
            {
                MainRenderView renderView = (MainRenderView)emitters[0].GetScene().MainCamera;

                if (renderView != null && Core.Time.RenderTime.TotalTime > 0.1f)
                {
                    GFX.Device.Textures[2] = renderView.DepthMap.GetTexture();
                    GFX.Device.Textures[3] = renderView.NormalMap.GetTexture();
                    GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_EYEPOSPHYSICS, renderView.GetEyePosShader());
                    GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_VIEWMATRIXPHYSICS, renderView.GetViewProjection());
                }
            }
            else
            {
                GFX.Device.Textures[2] = null;
                GFX.Device.Textures[3] = null;
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_EYEPOSPHYSICS, new Vector4(0, 0, 0, 999));
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_VIEWMATRIXPHYSICS, Matrix.Identity);
            }

            for(int i = 0; i < emitters.Count; i++)
            {
                ParticleEffect effect = emitters[i].GetParticleEffect();

                GFX.Device.Textures[0] = emitters[i].positionData;//.GetTexture();
                GFX.Device.Textures[1] = emitters[i].velocityData;//.GetTexture();

                Vector4 emitOrigin = new Vector4(emitters[i].Transformation.GetPosition(), effect.size);
                Vector4 dataParams0 = new Vector4(effect.initialDirection, effect.randomInitSpeed);
                Vector4 dataParams1 = new Vector4(effect.initialSpeed, effect.initialSpeedVariance, (emitters[i].EmitOnce)?1:0, 0);
                Vector4 lifeTimeParams = new Vector4(effect.lifetime, effect.lifetimeVariance, effect.mass, effect.massVariance);
                Vector2 randVector = (new Vector2((float)rand.NextDouble(),(float)rand.NextDouble())*2.0f-Vector2.One)*0.5f;

                Vector2 invSize = Vector2.One / (Vector2.One * emitters[i].GetTextureSize());
                GFX.Device.SetVertexShaderConstant(0, invSize);
                GFX.Device.SetVertexShaderConstant(1, randVector);
                GFX.Device.SetPixelShaderConstant(0, emitOrigin);
                GFX.Device.SetPixelShaderConstant(1, lifeTimeParams);
                GFX.Device.SetPixelShaderConstant(2, dataParams0);
                GFX.Device.SetPixelShaderConstant(3, dataParams1);
                GFX.Device.SetPixelShaderConstant(4, Vector4.One * timeDT);
                GFX.Device.SetPixelShaderConstant(5, effect.offsetParameters);
                GFX.Device.SetPixelShaderConstant(6, emitters[i].Transformation.GetTransform());
                GFX.Device.SetPixelShaderConstant(9, particleForces);

                GFX.Device.SetRenderTarget(0, emitters[i].positionTarget);
                GFX.Device.SetRenderTarget(1, emitters[i].velocityTarget);

                GFXPrimitives.Quad.Render();

                GFX.Device.SetRenderTarget(0, null);
                GFX.Device.SetRenderTarget(1, null);

                emitters[i].positionData = emitters[i].positionTarget.GetTexture();
                emitters[i].velocityData = emitters[i].velocityTarget.GetTexture();
            }
            GFX.Inst.SetTextureFilter(0, TextureFilter.Anisotropic);
            GFX.Inst.SetTextureFilter(1, TextureFilter.Anisotropic);
            GFX.Inst.SetTextureFilter(2, TextureFilter.Anisotropic);
            GFX.Inst.SetTextureFilter(3, TextureFilter.Anisotropic);
        }

        public void ComputeParticleSizes(Matrix viewProjection, float screenSize)
        {
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;

            GFX.Inst.SetTextureFilter(0, TextureFilter.Point);

            updateSizeShader.SetupShader();
            GFX.Device.SetPixelShaderConstant(0, viewProjection);
            for (int i = 0; i < emitters.Count; i++)
            {
                ParticleEffect effect = emitters[i].GetParticleEffect();
                GFX.Device.SetRenderTarget(0, emitters[i].sizeData);
                GFX.Device.Textures[0] = emitters[i].positionData;//.GetTexture();
                GFX.Device.SetVertexShaderConstant(0, Vector2.One / new Vector2(emitters[i].positionData.Width, emitters[i].positionData.Height));
                GFX.Device.SetPixelShaderConstant(0, viewProjection);
                GFX.Device.SetPixelShaderConstant(4, Vector4.One*effect.size*screenSize);
                GFXPrimitives.Quad.Render();
                GFX.Device.SetRenderTarget(0, null);
            }
            GFX.Inst.SetTextureFilter(0, TextureFilter.Anisotropic);
            GFX.Device.DepthStencilBuffer = dsOld;
        }
    }
}
