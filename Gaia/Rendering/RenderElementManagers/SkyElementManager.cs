using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.Rendering.RenderViews;

namespace Gaia.Rendering
{
    public struct SkyRenderElement
    {
        public Vector3 rayleighColor;

        public float rayleighHeight;

        public float rayleighGain;

        public Vector3 mieColor;

        public float mieHeight;

        public float mieGain;

        public Texture2D cloudTexture;
    }

    public class SkyElementManager : RenderElementManager
    {
        public List<SkyRenderElement> Elements = new List<SkyRenderElement>();

        Shader skyShaderPrepass;
        Shader skyShader;
        Shader cloudShader;
        RenderTarget2D skyTexture;

        TextureResource nightTexture;

        RenderTarget2D targetToRenderTo = null;
        RenderTargetCube targetToRenderToCube = null;
        CubeMapFace faceToRenderOn;

        Matrix cloudMatrix = Matrix.Identity;

        public SkyElementManager(RenderView renderView)
            : base(renderView)
        {
            skyShaderPrepass = ResourceManager.Inst.GetShader("SkyShaderPrepass");
            skyShader = ResourceManager.Inst.GetShader("SkyShader");
            cloudShader = ResourceManager.Inst.GetShader("CloudShader");

            nightTexture = ResourceManager.Inst.GetTexture("Textures/Sky/StarrySky.dds");

            skyTexture = new RenderTarget2D(GFX.Device, 16, 16, 1, SurfaceFormat.Color);

            cloudMatrix = Matrix.CreateRotationX(MathHelper.PiOver2); 
            cloudMatrix.Translation = Vector3.Up * 0.09f;
        }

        public Texture2D GetTexture()
        {
            return skyTexture.GetTexture();
        }

        public void Render(RenderTarget2D activeRT)
        {
            targetToRenderTo = activeRT;
            this.Render();
        }

        public void Render(RenderTargetCube activeRT, CubeMapFace activeFace)
        {
            targetToRenderToCube = activeRT;
            faceToRenderOn = activeFace;
            this.Render();
        }

        void RenderClouds()
        {
            GFX.Device.RenderState.AlphaBlendEnable = true;
            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

            cloudShader.SetupShader();
            Matrix[] matrices = new Matrix[2] { cloudMatrix, new Matrix(0.5f, 0.25f, 0.0024f, 0.0014f, 0.5f, 0.25f, 1.24f, 3.24f, 0.5f, 0.25f, 1.24f, 3.24f, 0.5f, 0.25f, 1.24f, 3.24f)};
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, matrices);


            GFX.Inst.SetTextureAddressMode(0, TextureAddressMode.Wrap);
            GFX.Inst.SetTextureFilter(0, TextureFilter.Anisotropic);

            for (int i = 0; i < Elements.Count; i++)
            {
                if (Elements[i].cloudTexture != null)
                {
                    GFX.Device.Textures[0] = Elements[i].cloudTexture;
                    GFXPrimitives.Quad.Render();
                }
            }
            GFX.Device.RenderState.AlphaBlendEnable = false;
        }

        public override void Render()
        {
            GFX.Device.RenderState.CullMode = CullMode.None;
            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;

            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PTDec;
            GFX.Device.SetRenderTarget(0, skyTexture);
            GFX.Device.Clear(Color.Black);

            skyShaderPrepass.SetupShader();

            GFX.Device.SetPixelShaderConstant(4, Vector4.One * -3.0f); //Exposure
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, renderView.GetViewProjectionLocal());
            
            for (int i = 0; i < Elements.Count; i++)
            {
                GFX.Device.SetPixelShaderConstant(0, Elements[i].rayleighColor); //Rayleigh Term
                GFX.Device.SetPixelShaderConstant(1, Elements[i].mieColor);      //Mie Term
                GFX.Device.SetPixelShaderConstant(2, new Vector4(Elements[i].rayleighHeight, Elements[i].mieHeight, Elements[i].rayleighGain, Elements[i].mieGain)); //Height Term
                GFXPrimitives.Cube.Render();
            }
            
            GFX.Device.SetRenderTarget(0, targetToRenderTo);

            if (targetToRenderToCube != null)
            {
                GFX.Device.SetRenderTarget(0, targetToRenderToCube, faceToRenderOn);
            }

            GFX.Device.Clear(Color.Black);
            
            skyShader.SetupShader();
 
            GFX.Device.Textures[0] = skyTexture.GetTexture();
            GFX.Device.Textures[1] = nightTexture.GetTexture();

            GFX.Inst.SetTextureFilter(0, TextureFilter.Linear);
            GFX.Inst.SetTextureAddressMode(0, TextureAddressMode.Clamp);
            GFX.Inst.SetTextureFilter(1, TextureFilter.Anisotropic);
            GFX.Device.SetPixelShaderConstant(0, Vector2.One / new Vector2(skyTexture.Width, skyTexture.Height));

            GFXPrimitives.Cube.Render();
            
            RenderClouds();

            GFX.Inst.ResetState();
            Elements.Clear();
        }
    }
}
