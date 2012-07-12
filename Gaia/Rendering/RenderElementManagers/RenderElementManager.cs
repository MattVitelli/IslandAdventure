using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Rendering.RenderViews;

namespace Gaia.Rendering
{
    public class RenderElement
    {
        public int StartVertex;

        public int VertexCount;

        public int VertexStride;

        public int PrimitiveCount;

        public Matrix[] Transform;

        public IndexBuffer IndexBuffer;

        public VertexBuffer VertexBuffer;

        public VertexDeclaration VertexDec;

        public bool IsAnimated = false;

    }

    public abstract class RenderElementManager
    {
        protected RenderView renderView;

        public RenderElementManager(RenderView renderView)
        {
            this.renderView = renderView;
        }

        public virtual void Render() { }
    }
}
