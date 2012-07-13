using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.SceneGraph;
using Gaia.Resources;
namespace Gaia.Rendering.RenderViews
{
    public class MainRenderView : RenderView
    {
        public RenderTarget2D ColorMap;
        public RenderTarget2D DepthMap;
        public RenderTarget2D NormalMap;
        public RenderTarget2D DataMap;
        public RenderTarget2D LightMap;

        public RenderTarget2D ParticleBuffer;

        public ResolveTexture2D BackBufferTexture;

        public SceneRenderView planarReflection;

        public SceneRenderView[] reflectionViews;

        int CubeMapSize = 32;

        public RenderTargetCube CubeMap; 

        Matrix TexGen;
        public Scene scene;

        public MainRenderView(Scene scene, Matrix view, Matrix projection, Vector3 position, float nearPlane, float farPlane)
            : base(RenderViewType.MAIN, view, projection, position, nearPlane, farPlane)
        {
            this.scene = scene;
            InitializeTextures();
            InitializeRenderViews();
            InitializeManagers();
        }
        ~MainRenderView()
        {
            DestroyRenderViews();
        }

        public Texture2D GetSkyTexture()
        {
            return ((SkyElementManager)ElementManagers[RenderPass.Sky]).GetTexture();
        }

        void InitializeManagers()
        {
            this.ElementManagers.Add(RenderPass.Sky, new SkyElementManager(this));
            this.ElementManagers.Add(RenderPass.Scene, new SceneElementManager(this));
            this.ElementManagers.Add(RenderPass.Emissive, new SceneElementManager(this));
            this.ElementManagers.Add(RenderPass.PostProcess, new PostProcessElementManager(this));
            this.ElementManagers.Add(RenderPass.Light, new LightElementManager(this));
            this.ElementManagers.Add(RenderPass.Particles, new ParticleElementManager(this));
            this.ElementManagers.Add(RenderPass.TransparentGBuffer, new SceneElementManager(this));
            this.ElementManagers.Add(RenderPass.TransparentColor, new SceneElementManager(this));
            this.ElementManagers.Add(RenderPass.FirstPersonPrepass, new SceneElementManager(this));
            this.ElementManagers.Add(RenderPass.FirstPerson, new SceneElementManager(this));
            this.ElementManagers.Add(RenderPass.Decal, new DecalElementManager(this));
            this.ElementManagers.Add(RenderPass.Terrain, new TerrainElementManager(this));
            this.ElementManagers.Add(RenderPass.Foliage, new FoliageElementManager(this));
        }

        void InitializeTextures()
        {
            int width = GFX.Device.PresentationParameters.BackBufferWidth;
            int height = GFX.Device.PresentationParameters.BackBufferHeight;

            TexGen = GFX.Inst.ComputeTextureMatrix(new Vector2(width, height));

            ColorMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Color);
            DepthMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Single);
            NormalMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.HalfVector2);
            DataMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Color);
            LightMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Color);

            ParticleBuffer = new RenderTarget2D(GFX.Device, width / 8, height / 8, 1, SurfaceFormat.Color);

            BackBufferTexture = new ResolveTexture2D(GFX.Device, width, height, 1, SurfaceFormat.Color);

            CubeMap = new RenderTargetCube(GFX.Device, CubeMapSize, 1, SurfaceFormat.Color);
        }

        void InitializeRenderViews()
        {
            planarReflection = new SceneRenderView(scene, Matrix.Identity, Matrix.Identity, Vector3.Zero, 0.1f, 1000.0f, 256, 256, true);

            reflectionViews = new SceneRenderView[6];
            for (int i = 0; i < reflectionViews.Length; i++)
            {
                reflectionViews[i] = new SceneRenderView(scene, Matrix.Identity, Matrix.Identity, Vector3.Zero, 0.1f, 1000.0f, CubeMapSize, CubeMapSize, false);
                reflectionViews[i].SetCubeMapTarget(CubeMap, (CubeMapFace)i);
                //scene.AddRenderView(reflectionViews[i]);
            }
            
            //scene.AddRenderView(planarReflection);
        }

        void DestroyRenderViews()
        {
            //scene.RemoveRenderView(planarReflection);
            for (int i = 0; i < reflectionViews.Length; i++)
            {
                //scene.RemoveRenderView(reflectionViews[i]);
            }
        }

        Vector3 GetCubeMapDir(CubeMapFace face)
        {
            switch (face)
            {
                case CubeMapFace.PositiveX:
                    return Vector3.Right;
                case CubeMapFace.NegativeX:
                    return Vector3.Left;
                case CubeMapFace.PositiveY:
                    return Vector3.Up;
                case CubeMapFace.NegativeY:
                    return Vector3.Down;
                case CubeMapFace.PositiveZ:
                    return Vector3.Forward;
                case CubeMapFace.NegativeZ:
                    return Vector3.Backward;
            }
            return Vector3.Forward;
        }

        Vector3 GetCubeMapUp(CubeMapFace face)
        {
            switch (face)
            {
                case CubeMapFace.PositiveY:
                    return Vector3.Backward;
                case CubeMapFace.NegativeY:
                    return Vector3.Forward;
            }

            return Vector3.Up;
        }

        public void UpdateRenderViews()
        {
            Plane waterPlane = new Plane(Vector3.Up, 0);
            Matrix reflectMat = Matrix.CreateReflection(waterPlane);
            planarReflection.SetNearPlane(this.GetNearPlane());
            planarReflection.SetFarPlane(this.GetFarPlane());
            planarReflection.SetPosition(this.GetPosition());
            planarReflection.SetView(reflectMat*this.GetView());
            planarReflection.SetProjection(this.GetProjection());
            planarReflection.enableClipPlanes = true;

            Vector3 cubemapPos = this.GetPosition();

            for (int i = 0; i < reflectionViews.Length; i++)
            {
                Matrix viewMat = Matrix.CreateLookAt(cubemapPos, cubemapPos + GetCubeMapDir((CubeMapFace)i), GetCubeMapUp((CubeMapFace)i));
                reflectionViews[i].SetNearPlane(this.GetNearPlane());
                reflectionViews[i].SetFarPlane(this.GetFarPlane());
                reflectionViews[i].SetPosition(cubemapPos);
                reflectionViews[i].SetView(viewMat);
                reflectionViews[i].SetProjection(Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1, this.GetNearPlane(), this.GetFarPlane()));
            }
            
        }

        public void RenderFirstPerson()
        {
            //GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, GetViewProjectionLocal());
            ElementManagers[RenderPass.FirstPerson].Render();
            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;
        }

        public void AddDecalElement(Material material, Matrix transform)
        {
            DecalElementManager decal = (DecalElementManager)ElementManagers[RenderPass.Decal];
            decal.AddElement(material, transform);
        }

        public override void AddElement(Material material, RenderElement element)
        {
            if (material.IsTranslucent)
            {
                PostProcessElementManager ppMgr = (PostProcessElementManager)ElementManagers[RenderPass.PostProcess];
                ppMgr.AddElement(material, element);
                /*
                SceneElementManager gbufferTrans = (SceneElementManager)ElementManagers[RenderPass.TransparentGBuffer];
                SceneElementManager colorTrans = (SceneElementManager)ElementManagers[RenderPass.TransparentColor];
                gbufferTrans.AddElement(material, element);
                colorTrans.AddElement(material, element);
                */
            }
            else
            {
                if (material.IsFoliage)
                {
                    FoliageElementManager mgr = (FoliageElementManager)ElementManagers[RenderPass.Foliage];
                    mgr.AddElement(material, element);
                }
                    if (material.IsEmissive)
                    {
                        Material mat = ResourceManager.Inst.GetMaterial(material.EmissiveMaterial);
                        if (mat == null)
                            mat = material;
                        SceneElementManager glowMgr = (SceneElementManager)ElementManagers[RenderPass.Emissive];
                        glowMgr.AddElement(mat, element);
                    }
                    else
                    {
                        SceneElementManager sceneMgr = (SceneElementManager)ElementManagers[RenderPass.Scene];
                        sceneMgr.AddElement(material, element);
                    }
                
            }
        }

        public override void Render()
        {
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, GetViewProjection());
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_EYEPOS, GetEyePosShader());
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_EYEPOS, GetEyePosShader());

            TerrainElementManager terrMgr = (TerrainElementManager)ElementManagers[RenderPass.Terrain];

            terrMgr.PerformBlending();

            GFX.Device.SetRenderTarget(0, ColorMap);
            GFX.Device.SetRenderTarget(1, NormalMap);
            GFX.Device.SetRenderTarget(2, DepthMap);
            GFX.Device.SetRenderTarget(3, DataMap);

            GFX.Device.Clear(Color.TransparentBlack);            

            ElementManagers[RenderPass.Terrain].Render();
            ElementManagers[RenderPass.Scene].Render();
            /*
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, GetViewProjectionLocal());
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_EYEPOS, GetEyePosLocalShader());
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_EYEPOS, GetEyePosLocalShader());
            */

            ElementManagers[RenderPass.FirstPersonPrepass].Render();
            ElementManagers[RenderPass.Decal].Render();
            
            /*
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, GetViewProjection());
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_EYEPOS, GetEyePosShader());
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_EYEPOS, GetEyePosShader());
            */
            GFX.Device.SetRenderTarget(1, null);
            GFX.Device.SetRenderTarget(2, null);
            GFX.Device.SetRenderTarget(3, null);

            ElementManagers[RenderPass.Foliage].Render();
            //FoliageElementManager foliageMgr = (FoliageElementManager)ElementManagers[RenderPass.Foliage];
            GFX.Device.SetRenderTarget(0, null);

            GFX.Device.Textures[0] = NormalMap.GetTexture();
            GFX.Device.Textures[1] = DepthMap.GetTexture();
            GFX.Device.Textures[2] = DataMap.GetTexture();
            
            GFX.Device.SetRenderTarget(0, LightMap);
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_TEXGEN, TexGen);
            GFX.Device.Clear(Color.TransparentBlack);
            ElementManagers[RenderPass.Light].Render();
            GFX.Device.SetRenderTarget(0, null);

            GFX.Device.SetRenderTarget(0, ParticleBuffer);
            GFX.Device.SetPixelShaderConstant(0, Vector2.One / new Vector2(ParticleBuffer.Width, ParticleBuffer.Height));
            GFX.Inst.SetTextureFilter(2, TextureFilter.Point);
            GFX.Device.Textures[2] = DepthMap.GetTexture();
            GFX.Device.Clear(Color.TransparentBlack);
            ElementManagers[RenderPass.Particles].Render();
            GFX.Device.SetRenderTarget(0, null);
            PostProcessElementManager mgr = (PostProcessElementManager)ElementManagers[RenderPass.PostProcess];
            mgr.BlurParticles();

            GFX.Device.Clear(Color.TransparentBlack);
            GFX.Device.SetPixelShaderConstant(3, scene.GetMainLightDirection()); //Light Direction for sky
            ElementManagers[RenderPass.Sky].Render(); //This'll change the modelview

            ElementManagers[RenderPass.PostProcess].Render();            
        }
    }
}
