using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Rendering.RenderViews;
using Gaia.Resources;
namespace Gaia.Rendering
{
    public class TransparentElementManager : RenderElementManager
    {
        SortedList<Material, Queue<RenderElement>> Elements = new SortedList<Material, Queue<RenderElement>>();

        public TransparentElementManager(RenderView renderView) : base(renderView) { }

        public void AddElement(Material material, RenderElement element)
        {
            if (!Elements.ContainsKey(material))
                Elements.Add(material, new Queue<RenderElement>());
            Elements[material].Enqueue(element);
        }

        public override void Render()
        {
            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.Less;
            for (int i = 0; i < Elements.Keys.Count; i++)
            {
                Material key = Elements.Keys[i];

                if (Elements[key].Count > 0)
                    key.SetupMaterial();

                while (Elements[key].Count > 0)
                {
                    RenderElement currElem = Elements[key].Dequeue();
                    if (currElem.VertexDec != GFX.Device.VertexDeclaration)
                        GFX.Device.VertexDeclaration = currElem.VertexDec;
                    GFX.Device.Indices = currElem.IndexBuffer;
                    GFX.Device.Vertices[0].SetSource(currElem.VertexBuffer, 0, currElem.VertexStride);
                    GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, currElem.Transform);
                    GFX.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, currElem.StartVertex, currElem.VertexCount, 0, currElem.PrimitiveCount);
                }
            }
        }
    }
}
