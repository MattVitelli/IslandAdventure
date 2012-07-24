using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.TerrainHelper;
using Gaia.Resources;
using Gaia.Rendering.RenderViews;

namespace Gaia.Rendering
{
    public class TerrainRenderElement
    {
        public Texture2D[] BlendMaps;
        public Texture2D NormalMap;
        public Clipmap[] clips;
        public int N;
        public Vector3 CameraPos;
    }

    public class TerrainElementManager : RenderElementManager
    {
        public virtual void AddElement(TerrainClimate climate, TerrainRenderElement element) {}

        public TerrainElementManager(RenderView view) : base(view) { }

        protected void DrawElement(TerrainRenderElement element)
        {
            for (int j = element.clips.Length - 1; j >= 0; j--)
            {
                bool isVisible = (renderView.GetFrustum().Contains(element.clips[j].Bounds) != ContainmentType.Disjoint || element.clips[j].Bounds.Contains(renderView.GetPosition()) != ContainmentType.Disjoint);
                if (element.clips[j].Triangles > 0 && isVisible)
                {
                    GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_USERDEF0, new Vector4(element.N, element.clips[j].FactorG, element.CameraPos.X, element.CameraPos.Z));
                    GFX.Device.DrawUserIndexedPrimitives(PrimitiveType.TriangleStrip, element.clips[j].Vertices, 0, element.clips[j].Vertices.Length, element.clips[j].Indices, 0, element.clips[j].Triangles);
                }
            }
        }
    }

    public class TerrainSceneElementManager : TerrainElementManager
    {
        SortedList<TerrainClimate, Queue<TerrainRenderElement>> Elements = new SortedList<TerrainClimate, Queue<TerrainRenderElement>>();
        
        /*
        RenderTarget2D BlendAlbedo;
        RenderTarget2D BlendNormal;
        DepthStencilBuffer DepthStencil;
        Texture2D nullAlbedo;
        Texture2D nullNormal;
        */
        Shader terrainShader;
        Shader terrainBlendShader;

        MainRenderView mainRenderView;

        public TerrainSceneElementManager(MainRenderView renderView)
            : base(renderView)
        {
            mainRenderView = renderView;
            terrainShader = ResourceManager.Inst.GetShader("TerrainHMShader");
            /*
            BlendAlbedo = new RenderTarget2D(GFX.Device, renderView.ColorMap.Width/2, renderView.ColorMap.Height/2, 1, SurfaceFormat.Color);
            BlendNormal = new RenderTarget2D(GFX.Device, renderView.ColorMap.Width/2, renderView.ColorMap.Height/2, 1, SurfaceFormat.Color);
            DepthStencil = new DepthStencilBuffer(GFX.Device, renderView.ColorMap.Width, renderView.ColorMap.Height, GFX.Device.DepthStencilBuffer.Format);

            Color[] whiteColor = new Color[1] { Color.White };
            Color[] normalColor = new Color[1] { Color.Blue };
            nullAlbedo = new Texture2D(GFX.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            nullAlbedo.SetData<Color>(whiteColor);
            nullNormal = new Texture2D(GFX.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            nullNormal.SetData<Color>(normalColor);
            */
        }

        public override void AddElement(TerrainClimate climate, TerrainRenderElement element)
        {
            if (!Elements.ContainsKey(climate))
                Elements.Add(climate, new Queue<TerrainRenderElement>());

            Elements[climate].Enqueue(element);
        }

        /*
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
        */
        public override void Render()
        {
            
            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PDec;

            terrainShader.SetupShader();
            GFX.Inst.SetTextureFilter(0, TextureFilter.Linear);
            GFX.Inst.SetTextureFilter(1, TextureFilter.Linear);
            //GFX.Device.Textures[0] = BlendAlbedo.GetTexture();
            //GFX.Device.Textures[1] = BlendNormal.GetTexture();
            //
            for (int i = 0; i < Elements.Count; i++)
            {
                TerrainClimate currKey = Elements.Keys[i];
                const int texOffset = 2;
                for (int j = 0; j < 4; j++)
                {
                    int baseIndex = texOffset + j;
                    GFX.Device.Textures[baseIndex] = currKey.BaseTextures[j].GetTexture();
                    GFX.Inst.SetTextureFilter(baseIndex, TextureFilter.Anisotropic);
                    GFX.Inst.SetTextureAddressMode(baseIndex, TextureAddressMode.Wrap);

                    int normIndex = texOffset + 4 + j;
                    GFX.Device.Textures[normIndex] = currKey.NormalTextures[j].GetTexture();
                    GFX.Inst.SetTextureFilter(normIndex, TextureFilter.Anisotropic);
                    GFX.Inst.SetTextureAddressMode(normIndex, TextureAddressMode.Wrap);
                }
                while (Elements[currKey].Count > 0)
                {
                    TerrainRenderElement element = Elements[currKey].Dequeue();
                    GFX.Device.Textures[0] = element.BlendMaps[0];
                    GFX.Device.Textures[1] = element.NormalMap;
                    GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.One / new Vector2(element.NormalMap.Width, element.NormalMap.Height));
                    DrawElement(element);
                }
            }
            GFX.Device.Textures[0] = null;
        }
    }

    public class TerrainShadowElementManager : TerrainElementManager
    {
        Queue<TerrainRenderElement> Elements = new Queue<TerrainRenderElement>();

        Shader terrainShader;

        public TerrainShadowElementManager(RenderView renderView)
            : base(renderView)
        {
            terrainShader = ResourceManager.Inst.GetShader("ShadowTerrainVSM");
        }

        public override void AddElement(TerrainClimate climate, TerrainRenderElement element)
        {
            Elements.Enqueue(element);
        }

        public override void Render()
        {
            GFX.Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;
            GFX.Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PDec;

            terrainShader.SetupShader();

            while (Elements.Count > 0)
            {
                TerrainRenderElement element = Elements.Dequeue();
                DrawElement(element);
            }
        }
    }
}
