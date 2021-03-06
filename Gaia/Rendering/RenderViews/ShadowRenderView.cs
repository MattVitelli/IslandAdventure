﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.SceneGraph.GameEntities;
using Gaia.Resources;
namespace Gaia.Rendering.RenderViews
{
    public class ShadowRenderView : RenderView
    {
        Viewport viewPort;
        int splitIndex;

        Light parent;

        public ShadowRenderView(Light parent, Viewport viewport, int splitIndex, Matrix view, Matrix projection, Vector3 position, float nearPlane, float farPlane)
            : base(RenderViewType.SHADOWS, view, projection, position, nearPlane, farPlane)
        {
            this.parent = parent;
            this.viewPort = viewport;
            this.splitIndex = splitIndex;
            this.Name = "ShadowRenderView" + this.GetHashCode();

            this.ElementManagers.Add(RenderPass.Terrain, new TerrainShadowElementManager(this));
            this.ElementManagers.Add(RenderPass.Shadows, new ShadowElementManager(this));
            ShadowElementManager foliageManager = new ShadowElementManager(this);
            foliageManager.SetShaders(ResourceManager.Inst.GetShader("ShadowVSMTransparent"), ResourceManager.Inst.GetShader("ShadowVSMTransparentInst"));
            this.ElementManagers.Add(RenderPass.Foliage, foliageManager);
        }

        public override void AddElement(Material material, RenderElement element)
        {
            ShadowElementManager sceneMgr = (ShadowElementManager)((material.IsFoliage) ? ElementManagers[RenderPass.Foliage] : ElementManagers[RenderPass.Shadows]);
            sceneMgr.AddElement(material, element);
        }

        public override void Render()
        {
            if (splitIndex == 0)
                parent.BeginShadowMapping();
            GFX.Device.Viewport = viewPort;

            base.Render();

            for (int i = 0; i < ElementManagers.Keys.Count; i++)
            {
                RenderPass pass = ElementManagers.Keys[i];
                ElementManagers[pass].Render();
            }

            if (splitIndex == GFXShaderConstants.NUM_SPLITS - 1)
                parent.EndShadowMapping();
        }
    }
}
