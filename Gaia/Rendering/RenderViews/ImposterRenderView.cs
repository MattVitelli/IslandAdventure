using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.SceneGraph;
using Gaia.Resources;
namespace Gaia.Rendering.RenderViews
{
    public class ImposterRenderView : RenderView
    {
        Matrix TexGen;

        public ImposterRenderView(Matrix view, Matrix projection, Vector3 position, float nearPlane, float farPlane)
            : base(RenderViewType.MAIN, view, projection, position, nearPlane, farPlane)
        {
            InitializeManagers();
        }

        void InitializeManagers()
        {
            this.ElementManagers.Add(RenderPass.Scene, new SceneElementManager(this));
            this.ElementManagers.Add(RenderPass.Foliage, new FoliageElementManager(this));
        }

        public override void AddElement(Material material, RenderElement element)
        {
            if (material.IsFoliage)
            {
                FoliageElementManager mgr = (FoliageElementManager)ElementManagers[RenderPass.Foliage];
                mgr.AddElement(material, element);
            }
            SceneElementManager sceneMgr = (SceneElementManager)ElementManagers[RenderPass.Scene];
            sceneMgr.AddElement(material, element);

        }

        public override void Render()
        {
            base.Render();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_MODELVIEW, GetViewProjection());
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_EYEPOS, GetEyePosShader());
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_EYEPOS, GetEyePosShader());

            ElementManagers[RenderPass.Scene].Render();
        }

        public void RenderBlended()
        {
            ElementManagers[RenderPass.Foliage].Render();
        }
    }
}
