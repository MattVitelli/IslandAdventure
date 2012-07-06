using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.SceneGraph;
using Gaia.Resources;
namespace Gaia.Rendering.RenderViews
{
    public class SceneRenderView : RenderView
    {
        public RenderTarget2D ColorMap;
        public RenderTarget2D DepthMap;
        public RenderTarget2D NormalMap;
        public RenderTarget2D LightMap;

        public RenderTarget2D ParticleBuffer;

        public RenderTarget2D ReflectionMap;

        protected Matrix TexGen;
        public Scene scene;

        protected int width;
        protected int height;

        public bool PerformFullShading() { return performFullShading; }

        bool performFullShading = false;

        public bool enableClipPlanes = false;

        RenderTargetCube cubeMapRef = null;
        CubeMapFace cubeMapFace;

        public void SetCubeMapTarget(RenderTargetCube cubemap, CubeMapFace faceMode)
        {
            this.cubeMapRef = cubemap;
            this.cubeMapFace = faceMode;
            this.ReflectionMap.Dispose();
            this.ReflectionMap = null;
        }

        public Vector2 GetResolution()
        {
            return new Vector2(width, height);
        }
        
        public SceneRenderView(Scene scene, Matrix view, Matrix projection, Vector3 position, float nearPlane, float farPlane, int width, int height, bool performFullShading)
            : base(RenderViewType.REFLECTIONS, view, projection, position, nearPlane, farPlane)
        {
            this.performFullShading = performFullShading;
            this.width = width;
            this.height = height;
            this.scene = scene;
            this.ElementManagers.Add(RenderPass.Sky, new SkyElementManager(this));
            this.ElementManagers.Add(RenderPass.Scene, new SceneElementManager(this));
            this.ElementManagers.Add(RenderPass.Light, new LightElementManager(this));
            this.ElementManagers.Add(RenderPass.Particles, new ParticleElementManager(this));
            this.ElementManagers.Add(RenderPass.PostProcess, new PostProcessReflectionsElementManager(this));

            InitializeTextures();
        }

        protected virtual void InitializeTextures()
        {
            TexGen = GFX.Inst.ComputeTextureMatrix(new Vector2(width, height));

            ColorMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Color);
            if (performFullShading)
            {
                DepthMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Single);
                NormalMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.HalfVector2);
                LightMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Color);

                ParticleBuffer = new RenderTarget2D(GFX.Device, width / 4, height / 4, 1, SurfaceFormat.Color);
            }

            ReflectionMap = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Color);
        }

        public override void AddElement(Material material, RenderElement element)
        {
            if (material.IsTranslucent)
            {
                //TransparentElementManager transMgr = (TransparentElementManager)ElementManagers[RenderPass.Translucent];
                //transMgr.AddElement(material, element);
            }
            else
            {
                if (material.IsEmissive)
                {
                    //We don't render glowy materials in reflections...
                }
                else
                {
                    SceneElementManager sceneMgr = (SceneElementManager)ElementManagers[RenderPass.Scene];
                    sceneMgr.AddElement(material, element);
                }
            }
        }

        Plane CreatePlane(float height, Vector3 planeNormalDirection, bool clipSide)
        {
            planeNormalDirection.Normalize();
            Vector4 planeCoeffs = new Vector4(planeNormalDirection, height);
            if (clipSide)
                planeCoeffs *= -1;
            planeCoeffs = Vector4.Transform(planeCoeffs, Matrix.Transpose(this.GetInverseViewProjection()));
            Plane finalPlane = new Plane(planeCoeffs);

            return finalPlane;
        }

        public override void Render()
        {
            if (enableClipPlanes)
            {
                Plane reflectionPlane = CreatePlane(0, -1.0f*Vector3.Up, true);
                GFX.Device.ClipPlanes[0].Plane = reflectionPlane;
                GFX.Device.ClipPlanes[0].IsEnabled = true;
            }
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, GetViewProjection());
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_EYEPOS, GetEyePosShader());
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_EYEPOS, GetEyePosShader());

            GFX.Device.SetRenderTarget(0, ColorMap);
            if (performFullShading)
            {
                GFX.Device.SetRenderTarget(1, NormalMap);
                GFX.Device.SetRenderTarget(2, DepthMap);
            }
            GFX.Device.Clear(Color.TransparentBlack);

            ElementManagers[RenderPass.Scene].Render();

            GFX.Device.SetRenderTarget(0, null);
            if (performFullShading)
            {
                GFX.Device.SetRenderTarget(1, null);
                GFX.Device.SetRenderTarget(2, null);

                GFX.Device.Textures[0] = NormalMap.GetTexture();
                GFX.Device.Textures[1] = DepthMap.GetTexture();

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
            }
            GFX.Device.ClipPlanes[0].IsEnabled = false;

            GFX.Device.Clear(Color.TransparentBlack);
            GFX.Device.SetPixelShaderConstant(3, scene.MainLight.Transformation.GetPosition()); //Light Direction for sky
            SkyElementManager skyMgr = (SkyElementManager)ElementManagers[RenderPass.Sky];
            if (cubeMapRef == null)
            {
                skyMgr.Render(ReflectionMap);//This'll change the modelview
            }
            else
            {
                skyMgr.Render(cubeMapRef, cubeMapFace);
            }
            ElementManagers[RenderPass.PostProcess].Render();
            GFX.Device.SetRenderTarget(0, null);
        }
    }
}
