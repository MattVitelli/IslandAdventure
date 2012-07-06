using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering.Geometry;

namespace Gaia.Rendering
{
    public class PostProcessReflectionsElementManager : RenderElementManager
    {
        Shader basicImageShader;
        Shader compositeShader;
        
        SceneRenderView mainRenderView; //Used to access GBuffer

        public PostProcessReflectionsElementManager(SceneRenderView renderView)
            : base(renderView)
        {
            mainRenderView = renderView;
            basicImageShader = ResourceManager.Inst.GetShader("Generic2D");
            compositeShader = ResourceManager.Inst.GetShader("Composite");
        }

        void RenderComposite()
        {
            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            
            
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / mainRenderView.GetResolution());
            GFX.Device.Textures[0] = mainRenderView.ColorMap.GetTexture();
            if (mainRenderView.PerformFullShading())
            {
                compositeShader.SetupShader();
                GFX.Device.Textures[1] = mainRenderView.LightMap.GetTexture();
                GFX.Device.Textures[2] = mainRenderView.DepthMap.GetTexture();
            }
            else
            {
                basicImageShader.SetupShader();
            }

            GFXPrimitives.Quad.Render();

            if (mainRenderView.PerformFullShading())
            {
                GFX.Device.RenderState.SourceBlend = Blend.One;
                GFX.Device.RenderState.DestinationBlend = Blend.One;
                basicImageShader.SetupShader();
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(mainRenderView.ParticleBuffer.Width, mainRenderView.ParticleBuffer.Height));
                GFX.Device.Textures[0] = mainRenderView.ParticleBuffer.GetTexture();
                GFX.Inst.SetTextureFilter(0, TextureFilter.Linear);
                GFXPrimitives.Quad.Render();
                GFX.Inst.SetTextureFilter(0, TextureFilter.Point);
            }

            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / GFX.Inst.DisplayRes);
        }

        public override void Render()
        {
            for(int i = 0; i < 4; i++)
            {
                GFX.Inst.SetTextureFilter(i, TextureFilter.Point);
                GFX.Inst.SetTextureAddressMode(i, TextureAddressMode.Clamp);
            }
            GFX.Device.RenderState.CullMode = CullMode.None;
            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;
            GFX.Device.RenderState.AlphaBlendEnable = true;

            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, mainRenderView.GetInverseViewProjectionLocal());
            
            RenderComposite();

            GFX.Device.RenderState.AlphaBlendEnable = false;

            GFX.Inst.ResetState();
        }
    }
}
