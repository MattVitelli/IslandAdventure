using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.SceneGraph.GameEntities;
using Gaia.Rendering.RenderViews;
using Gaia.Resources;
namespace Gaia.Rendering
{
    public class DecalElementManager : RenderElementManager
    {
        SortedList<Material, List<Matrix>> Elements = new SortedList<Material, List<Matrix>>();
        protected Matrix[] tempTransforms = new Matrix[GFXShaderConstants.NUM_INSTANCES];

        public DecalElementManager(RenderView renderView) : base(renderView) { }

        public void AddElement(Material material, Matrix transform)
        {
            if (!Elements.ContainsKey(material))
                Elements.Add(material, new List<Matrix>());
            Elements[material].Add(transform);
        }

        public override void Render()
        {
            
            GFX.Device.RenderState.DepthBufferEnable = true; 
            GFX.Device.RenderState.DepthBufferWriteEnable = false;
            GFX.Device.RenderState.CullMode = CullMode.None;
            GFX.Device.RenderState.AlphaBlendEnable = true;
            float oldDepthBias = GFX.Device.RenderState.DepthBias;
            GFX.Device.RenderState.DepthBias = -0.0001f;
            
            
            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;
            

            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PTIDec;
            GFX.Device.Indices = GFXPrimitives.Decal.GetInstanceIndexBuffer();
            GFX.Device.Vertices[0].SetSource(GFXPrimitives.Decal.GetInstanceVertexBuffer(), 0, VertexPTI.SizeInBytes);
            for (int i = 0; i < Elements.Keys.Count; i++)
            {
                Material key = Elements.Keys[i];

                if (Elements[key].Count > 0)
                    key.SetupMaterial();
                for (int j = 0; j < Elements[key].Count; j += GFXShaderConstants.NUM_INSTANCES)
                {
                    int binLength = Elements[key].Count - j;

                    if (binLength > GFXShaderConstants.NUM_INSTANCES)
                        binLength = GFXShaderConstants.NUM_INSTANCES;

                    // Upload transform matrices as shader constants.
                    Elements[key].CopyTo(j, tempTransforms, 0, binLength);
                    GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, tempTransforms);
                    GFX.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 4 * binLength, 0, 2 * binLength);
                }
                Elements[key].Clear();
            }

            GFX.Device.RenderState.DepthBias = oldDepthBias;
            GFX.Device.RenderState.AlphaBlendEnable = false;
            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
        }
        
    }
}
