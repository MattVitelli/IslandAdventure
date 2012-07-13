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
    }

    public class SkyElementManager : RenderElementManager
    {
        public List<SkyRenderElement> Elements = new List<SkyRenderElement>();

        Shader skyShaderPrepass;
        Shader skyShader;
        RenderTarget2D skyTexture;

        TextureResource nightTexture;

        RenderTarget2D targetToRenderTo = null;
        RenderTargetCube targetToRenderToCube = null;
        CubeMapFace faceToRenderOn;

        public SkyElementManager(RenderView renderView)
            : base(renderView)
        {
            skyShaderPrepass = ResourceManager.Inst.GetShader("SkyShaderPrepass");
            skyShader = ResourceManager.Inst.GetShader("SkyShader");

            nightTexture = ResourceManager.Inst.GetTexture("Textures/Sky/StarrySky.dds");

            skyTexture = new RenderTarget2D(GFX.Device, 32, 32, 1, SurfaceFormat.Color);
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

            GFX.Inst.ResetState();
            Elements.Clear();
        }
    }
}
