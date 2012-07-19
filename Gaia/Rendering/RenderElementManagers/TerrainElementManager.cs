using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.Rendering.RenderViews;

namespace Gaia.Rendering
{
    public class TerrainRenderElement
    {
        public Texture2D[] BlendMaps;
        public RenderElement Element;
    }

    public class TerrainElementManager : RenderElementManager
    {
        SortedList<TerrainClimate, Queue<TerrainRenderElement>> Elements = new SortedList<TerrainClimate, Queue<TerrainRenderElement>>();

        Queue<TerrainRenderElement> ElementsToProcess = new Queue<TerrainRenderElement>();

        RenderTarget2D BlendAlbedo;
        RenderTarget2D BlendNormal;
        DepthStencilBuffer DepthStencil;
        Texture2D nullAlbedo;
        Texture2D nullNormal;

        Shader terrainShader;
        Shader terrainBlendShader;

        MainRenderView mainRenderView;

        public TerrainElementManager(MainRenderView renderView)
            : base(renderView)
        {
            mainRenderView = renderView;
            terrainShader = ResourceManager.Inst.GetShader("TerrainHMShader");
            terrainBlendShader = ResourceManager.Inst.GetShader("TerrainBlend");
            BlendAlbedo = new RenderTarget2D(GFX.Device, renderView.ColorMap.Width/2, renderView.ColorMap.Height/2, 1, SurfaceFormat.Color);
            BlendNormal = new RenderTarget2D(GFX.Device, renderView.ColorMap.Width/2, renderView.ColorMap.Height/2, 1, SurfaceFormat.Color);
            DepthStencil = new DepthStencilBuffer(GFX.Device, renderView.ColorMap.Width, renderView.ColorMap.Height, GFX.Device.DepthStencilBuffer.Format);

            Color[] whiteColor = new Color[1] { Color.White };
            Color[] normalColor = new Color[1] { Color.Blue };
            nullAlbedo = new Texture2D(GFX.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            nullAlbedo.SetData<Color>(whiteColor);
            nullNormal = new Texture2D(GFX.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            nullNormal.SetData<Color>(normalColor);
        }

        public void AddElement(TerrainClimate climate, TerrainRenderElement element)
        {
            if (!Elements.ContainsKey(climate))
                Elements.Add(climate, new Queue<TerrainRenderElement>());

            Elements[climate].Enqueue(element);

            ElementsToProcess.Enqueue(element);
        }

        void DrawElement(TerrainRenderElement element)
        {
            //for (int i = 0; i < element.Elements.Length; i++)
            {
                RenderElement currElem = element.Element;
                GFX.Device.VertexDeclaration = currElem.VertexDec;
                GFX.Device.Indices = currElem.IndexBuffer;
                GFX.Device.Vertices[0].SetSource(currElem.VertexBuffer, 0, currElem.VertexStride);
                GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_WORLD, currElem.Transform);
                GFX.Device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, currElem.StartVertex, currElem.VertexCount, 0, currElem.PrimitiveCount);
            }
        }

        void BlendElement(TerrainClimate climate, TerrainRenderElement element, bool performAlphaBlend)
        {
            int startIndex = (performAlphaBlend) ? 1 : 0;
            for (int i = startIndex; i < element.BlendMaps.Length; i++)
            {
                int offset = i * 4;
                for (int j = 0; j < 4; j++)
                {
                    int index = j * 2;
                    int textureIndex = j + offset;
                    if (climate.BaseTextures[textureIndex] != null)
                        GFX.Device.Textures[index] = climate.BaseTextures[textureIndex].GetTexture();
                    else
                        GFX.Device.Textures[index] = nullAlbedo;
                    if (climate.NormalTextures[textureIndex] != null)
                        GFX.Device.Textures[index + 1] = climate.NormalTextures[textureIndex].GetTexture();
                    else
                        GFX.Device.Textures[index + 1] = nullNormal;
                }
                GFX.Device.Textures[8] = element.BlendMaps[i];
                DrawElement(element);
                if (!performAlphaBlend)
                    break;
            }
        }

        public void PerformBlending()
        {
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;

            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;

            GFX.Device.SetRenderTarget(0, BlendAlbedo);
            GFX.Device.SetRenderTarget(1, BlendNormal);
            GFX.Device.DepthStencilBuffer = DepthStencil;
            GFX.Device.Clear(Color.TransparentBlack);

            terrainBlendShader.SetupShader();

            for (int i = 0; i < Elements.Count; i++)
            {
                TerrainClimate currClimate = Elements.Keys[i];
                for (int j = 0; j < Elements[currClimate].Count; j++)
                {
                    TerrainRenderElement terrElement = Elements[currClimate].Dequeue();
                    BlendElement(currClimate, terrElement, false);
                    Elements[currClimate].Enqueue(terrElement);
                }
            }

            //Now the blend pass

            GFX.Device.RenderState.AlphaBlendEnable = true;
            GFX.Device.RenderState.SourceBlend = Blend.One;
            GFX.Device.RenderState.DestinationBlend = Blend.One;

            for (int i = 0; i < Elements.Count; i++)
            {
                TerrainClimate currClimate = Elements.Keys[i];
                for (int j = 0; j < Elements[currClimate].Count; j++)
                {
                    TerrainRenderElement terrElement = Elements[currClimate].Dequeue();
                    BlendElement(currClimate, terrElement, true);
                }
            }

            GFX.Device.RenderState.AlphaBlendEnable = false;

            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.SetRenderTarget(1, null);
            GFX.Device.DepthStencilBuffer = dsOld;
        }

        public override void Render()
        {
            
            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;

            terrainShader.SetupShader();
            GFX.Inst.SetTextureFilter(0, TextureFilter.Linear);
            GFX.Inst.SetTextureFilter(1, TextureFilter.Linear);
            GFX.Device.Textures[0] = BlendAlbedo.GetTexture();
            GFX.Device.Textures[1] = BlendNormal.GetTexture();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(BlendAlbedo.Width, BlendAlbedo.Height));

            while(ElementsToProcess.Count > 0)
            {
                TerrainRenderElement element = ElementsToProcess.Dequeue();
                DrawElement(element);
            }
            GFX.Device.Textures[0] = null;
            GFX.Device.Textures[1] = null;
        }
    }
}
