using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering.Geometry;

namespace Gaia.Rendering
{

    public struct GUIElement
    {
        public Vector4 ScaleOffset;
        public Texture Image;
        public Vector4 Color;

        public GUIElement(Vector2 min, Vector2 max, Texture image)
        {
            Color = Vector4.One;
            ScaleOffset = Vector4.Zero;
            Image = image;
            SetDimensions(min, max);
        }

        public GUIElement(Vector2 min, Vector2 max, Texture image, Vector3 color)
        {
            Color = new Vector4(color,1.0f);
            ScaleOffset = Vector4.Zero;
            Image = image;
            SetDimensions(min, max);
        }

        public GUIElement(Vector2 min, Vector2 max, Texture image, Vector4 color)
        {
            Color = color;
            ScaleOffset = Vector4.Zero;
            Image = image;
            SetDimensions(min, max);
        }

        public void SetDimensions(Vector2 min, Vector2 max)
        {
            Vector2 tempMin = Vector2.Min(min, max);
            Vector2 tempMax = Vector2.Max(min, max);

            Vector2 scale = (tempMax - tempMin) * 0.5f;
            Vector2 offset = (tempMax + tempMin) * 0.5f;
            ScaleOffset = new Vector4(scale.X, scale.Y, offset.X, offset.Y);
        }
    };

    public struct GUIElementTC
    {
        public Vector4 ScaleOffset;
        public Texture Image;
        public Vector4 Color;
        public Vector4 TCMinMax;

        public GUIElementTC(Vector2 min, Vector2 max, Texture image, Vector2 minTC, Vector2 maxTC)
        {
            Color = Vector4.One;
            ScaleOffset = Vector4.Zero;
            TCMinMax = new Vector4(minTC.X, minTC.Y, maxTC.X, maxTC.Y);
            Image = image;
            SetDimensions(min, max);
        }

        public GUIElementTC(Vector2 min, Vector2 max, Texture image, Vector3 color, Vector2 minTC, Vector2 maxTC)
        {
            Color = new Vector4(color, 1.0f);
            ScaleOffset = Vector4.Zero;
            TCMinMax = new Vector4(minTC.X, minTC.Y, maxTC.X, maxTC.Y);
            Image = image;
            SetDimensions(min, max);
        }

        public GUIElementTC(Vector2 min, Vector2 max, Texture image, Vector4 color, Vector2 minTC, Vector2 maxTC)
        {
            Color = color;
            ScaleOffset = Vector4.Zero;
            TCMinMax = new Vector4(minTC.X, minTC.Y, maxTC.X, maxTC.Y);
            Image = image;
            SetDimensions(min, max);
        }

        public void SetDimensions(Vector2 min, Vector2 max)
        {
            Vector2 tempMin = Vector2.Min(min, max);
            Vector2 tempMax = Vector2.Max(min, max);

            Vector2 scale = (tempMax - tempMin) * 0.5f;
            Vector2 offset = (tempMax + tempMin) * 0.5f;
            ScaleOffset = new Vector4(scale.X, scale.Y, offset.X, offset.Y);
        }
    };

    public struct GUITextElement
    {
        public Vector2 Position;
        public string Text;
        public Vector4 Color;
        public float Size;

        public GUITextElement(Vector2 pos, string text)
        {
            Position = pos;
            Text = text;
            Color = Vector4.One;
            Size = 1;
        }

        public GUITextElement(Vector2 pos, string text, Vector3 color)
        {
            Position = pos;
            Text = text;
            Color = new Vector4(color, 1.0f);
            Size = 1;
        }
        public GUITextElement(Vector2 pos, string text, Vector4 color)
        {
            Position = pos;
            Text = text;
            Color = color;
            Size = 1;
        }
    }

    public class GUIElementManager
    {
        Shader basicImageShader;

        Queue<GUIElement> Elements = new Queue<GUIElement>();
        Queue<GUITextElement> TextElements = new Queue<GUITextElement>();
        Queue<GUIElementTC> ElementsTC = new Queue<GUIElementTC>();

        Texture2D whiteTexture;

        SpriteBatch spriteBatch;

        public SpriteFont DefaultFont;

        VertexPositionTexture[] verts = null;
        short[] ib = null;

        public GUIElementManager(SpriteFont defaultFont)
        {
            basicImageShader = new Shader();
            basicImageShader.CompileFromFiles("Shaders/PostProcess/GUIP.hlsl", "Shaders/PostProcess/GUIV.hlsl");
            whiteTexture = new Texture2D(GFX.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            Color[] color = new Color[] { Color.White };
            whiteTexture.SetData<Color>(color);
            spriteBatch = new SpriteBatch(GFX.Device);
            DefaultFont = defaultFont;
            CreateQuad();
        }

        void CreateQuad()
        {
            verts = new VertexPositionTexture[]
                    {
                        new VertexPositionTexture(
                                new Vector3(1,-1,0),
                                new Vector2(1,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,-1,0),
                                new Vector2(0,1)),
                            new VertexPositionTexture(
                                new Vector3(-1,1,0),
                                new Vector2(0,0)),
                            new VertexPositionTexture(
                                new Vector3(1,1,0),
                                new Vector2(1,0))
                    };

            ib = new short[] { 0, 1, 2, 2, 3, 0 };
        }

        public void AddElement(GUIElement element)
        {
            Elements.Enqueue(element);
        }

        public void AddElement(GUITextElement element)
        {
            TextElements.Enqueue(element);
        }

        public void AddElement(GUIElementTC element)
        {
            ElementsTC.Enqueue(element);
        }

        void DrawQuad(Vector4 TCMinMax)
        {
            GFX.Device.VertexDeclaration = GFXVertexDeclarations.PTDec;


            verts[0].TextureCoordinate = new Vector2(TCMinMax.Z, TCMinMax.W);
            verts[1].TextureCoordinate = new Vector2(TCMinMax.X, TCMinMax.W);
            verts[2].TextureCoordinate = new Vector2(TCMinMax.X, TCMinMax.Y);
            verts[3].TextureCoordinate = new Vector2(TCMinMax.Z, TCMinMax.Y);

            GFX.Device.DrawUserIndexedPrimitives<VertexPositionTexture> (PrimitiveType.TriangleList, verts, 0, 4, ib, 0, 2);
        }

        public void Render()
        {
            for(int i = 0; i < 4; i++)
            {
                GFX.Inst.SetTextureFilter(i, TextureFilter.Anisotropic);
                GFX.Inst.SetTextureAddressMode(i, TextureAddressMode.Wrap);
            }
            GFX.Device.RenderState.CullMode = CullMode.None;
            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;
            GFX.Device.RenderState.AlphaBlendEnable = true;

            GFX.Device.RenderState.SourceBlend = Blend.SourceAlpha;
            GFX.Device.RenderState.DestinationBlend = Blend.InverseSourceAlpha;

            basicImageShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_INVTEXRES, Vector2.Zero);

            while (Elements.Count > 0)
            {
                GUIElement elem = Elements.Dequeue();
                GFX.Device.Textures[0] = (elem.Image == null) ? whiteTexture : elem.Image;
                GFX.Device.SetVertexShaderConstant(0, elem.ScaleOffset);
                GFX.Device.SetPixelShaderConstant(0, elem.Color);
                GFXPrimitives.Quad.Render();
            }

            while (ElementsTC.Count > 0)
            {
                GUIElementTC elem = ElementsTC.Dequeue();
                GFX.Device.Textures[0] = (elem.Image == null) ? whiteTexture : elem.Image;
                GFX.Device.SetVertexShaderConstant(0, elem.ScaleOffset);
                GFX.Device.SetPixelShaderConstant(0, elem.Color);
                DrawQuad(elem.TCMinMax);
            }

            GFX.Device.RenderState.AlphaBlendEnable = false;

            DrawTextElements();

            GFX.Inst.ResetState();            
        }

        private void DrawTextElements()
        {
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Deferred, SaveStateMode.SaveState);
            while (TextElements.Count > 0)
            {
                GUITextElement element = TextElements.Dequeue();
                Vector2 pos = element.Position * new Vector2(0.5f, -0.5f) + Vector2.One * 0.5f;
                pos *= GFX.Inst.DisplayRes;
                Vector2 textSize = DefaultFont.MeasureString(element.Text);
                pos -= textSize *0.5f; //Align text to center of pos
                //spriteBatch.DrawString(DefaultFont, element.Text, pos, new Color(element.Color));
                spriteBatch.DrawString(DefaultFont, element.Text, pos, new Color(element.Color), 0, Vector2.Zero, element.Size, SpriteEffects.None, 0);
            }
            spriteBatch.End();
        }
    }
}
