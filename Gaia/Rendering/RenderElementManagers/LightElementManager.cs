using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph.GameEntities;
namespace Gaia.Rendering
{
    public class LightElementManager : RenderElementManager
    {
        public Queue<Light> AmbientLights = new Queue<Light>();
        public Queue<Light> DirectionalLights = new Queue<Light>();
        public Queue<Light> DirectionalShadowLights = new Queue<Light>();
        public Queue<Light> PointLights = new Queue<Light>();
        public Queue<Light> SpotLights = new Queue<Light>();

        Shader ambientLightShader;
        Shader directionalLightShader;
        Shader directionalLightShadowsShader;
        Shader pointLightShader;
        Shader spotLightShader;

        public LightElementManager(RenderView renderView)
            : base(renderView)
        {
            ambientLightShader = ResourceManager.Inst.GetShader("AmbientLightShader");
            directionalLightShader = ResourceManager.Inst.GetShader("DirectionalLightShader");
            directionalLightShadowsShader = ResourceManager.Inst.GetShader("DirectionalLightShadowShader");
            pointLightShader = ResourceManager.Inst.GetShader("PointLightShader");
            spotLightShader = ResourceManager.Inst.GetShader("PointLightShader");
        }

        void SetupLightParameters(Light light)
        {
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTPOS, light.Transformation.GetPosition());
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTCOLOR, light.Color);
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTPARAMS, light.Parameters);
        }

        public override void Render()
        {

            GFX.Device.RenderState.AlphaBlendEnable = true;
            GFX.Device.RenderState.AlphaBlendOperation = BlendFunction.Add;
            GFX.Device.RenderState.AlphaSourceBlend = Blend.One;
            GFX.Device.RenderState.AlphaDestinationBlend = Blend.One;
            GFX.Device.RenderState.BlendFunction = BlendFunction.Add;
            GFX.Device.RenderState.SourceBlend = Blend.One;
            GFX.Device.RenderState.DestinationBlend = Blend.One;
            GFX.Device.RenderState.DepthBufferEnable = false;

            GFX.Device.RenderState.CullMode = CullMode.None;

            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PDec;   

            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, renderView.GetViewProjectionLocal());
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, Matrix.Identity);

            ambientLightShader.SetupShader();

            while (AmbientLights.Count > 0)
            {
                Light currLight = AmbientLights.Dequeue();
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTCOLOR, currLight.Color);
                GFXPrimitives.Cube.Render();
            }


            directionalLightShader.SetupShader();

            while (DirectionalLights.Count > 0)
            {
                Light currLight = DirectionalLights.Dequeue();
                SetupLightParameters(currLight);
                GFXPrimitives.Cube.Render();
            }

            directionalLightShadowsShader.SetupShader();
            GFX.Device.SamplerStates[3].MagFilter = TextureFilter.Point;
            GFX.Device.SamplerStates[3].MinFilter = TextureFilter.Point;
            GFX.Device.SamplerStates[3].MipFilter = TextureFilter.None;

            GFX.Device.SetPixelShaderConstant(0, renderView.GetView());
            while (DirectionalShadowLights.Count > 0)
            {
                Light currLight = DirectionalShadowLights.Dequeue();
                SetupLightParameters(currLight);
                Texture2D shadowMap = currLight.GetShadowMap();
                GFX.Device.Textures[3] = shadowMap;
                
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTMODELVIEW, currLight.GetModelViews());
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTCLIPPLANE, currLight.GetClipPlanes());
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_LIGHTCLIPPOS, currLight.GetClipPositions());
                GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_INVSHADOWRES, Vector2.One / new Vector2(shadowMap.Width, shadowMap.Height));
                GFXPrimitives.Cube.Render();
            }

            GFX.Device.RenderState.CullMode = CullMode.CullClockwiseFace;
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, renderView.GetViewProjection());

            pointLightShader.SetupShader();
            while (PointLights.Count > 0)
            {
                Light currLight = PointLights.Dequeue();
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, currLight.Transformation.GetTransform());
                SetupLightParameters(currLight);
                GFXPrimitives.Cube.Render();
            }

            spotLightShader.SetupShader();
            while (SpotLights.Count > 0)
            {
                Light currLight = SpotLights.Dequeue();
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, currLight.Transformation.GetTransform());
                SetupLightParameters(currLight);
                GFXPrimitives.Cube.Render();
            }

            GFX.Inst.ResetState();
        }
    }
}
