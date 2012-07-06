using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Rendering;
using Gaia.Rendering.RenderViews;

namespace Gaia.SceneGraph.GameEntities
{
    public class Sky : Entity
    {
        const float invFactor = 0.0001f;
        const float Factor = 1.0f / invFactor;
        SkyRenderElement renderElement;
        float rayleighExp = -4.593f;
        float mieExp = -0.5624f;

        public override void OnAdd(Scene scene)
        {
            renderElement = new SkyRenderElement();
            renderElement.mieHeight = 0.0022f;
            renderElement.rayleighHeight = 0.0035f;
            renderElement.rayleighGain = 9.235767f;
            renderElement.mieGain = 16.199024f;
            SetColor(new Vector3(0.8353f, 0.6119f, 0.4780f));

            base.OnAdd(scene);
        }

        public override void OnRender(RenderView view)
        {
            if (view.GetRenderType() != RenderViewType.MAIN)
                return;
            SkyElementManager s = (SkyElementManager)view.GetRenderElementManager(RenderPass.Sky);
            if (s != null)
                s.Elements.Add(renderElement);
            base.OnRender(view);
        }

        public float MieHeight
        {
            get { return renderElement.mieHeight * Factor; }
            set { renderElement.mieHeight = value * invFactor; }
        }

        public float RayleighHeight
        {
            get { return renderElement.rayleighHeight * Factor; }
            set { renderElement.rayleighHeight = value * invFactor; }
        }

        public void SetGains(float rayleighGain, float mieGain)
        {
            renderElement.rayleighGain = rayleighGain;
            renderElement.mieGain = mieGain;
        }

        void SetColor(Vector3 color)
        {
            renderElement.rayleighColor.X = (float)Math.Pow(color.X, rayleighExp);
            renderElement.rayleighColor.Y = (float)Math.Pow(color.Y, rayleighExp);
            renderElement.rayleighColor.Z = (float)Math.Pow(color.Z, rayleighExp);

            renderElement.mieColor.X = (float)Math.Pow((double)color.X, mieExp);
            renderElement.mieColor.Y = (float)Math.Pow((double)color.Y, mieExp);
            renderElement.mieColor.Z = (float)Math.Pow((double)color.Z, mieExp);
        }
    }
}
