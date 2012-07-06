using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gaia.UI;
using Gaia.Rendering;
using Gaia.Core;
using Gaia.Resources;

namespace Gaia.UI
{
    public class UICompass : UIControl
    {
        string[] compassDirs = { "W", "N", "E", "S" };
        float[] compassTCs = { 0, 0.25f, 0.5f, 0.75f };
        const int compassTextureWidth = 512;
        const int compassTextureHeight = 2;
        const int compassTickDelta = 15;
        Texture2D compassTexture;
        Texture2D markerImage;

        Transform currTransform = null;
        List<Transform> markers = new List<Transform>();

        public void SetTransformation(Transform transform)
        {
            currTransform = transform;
        }

        public void AddMarker(Transform transform)
        {
            markers.Add(transform);
        }

        public void RemoveMarker(Transform transform)
        {
            markers.Remove(transform);
        }

        public UICompass()
        {
            CreateCompassTexture();
            markerImage = (Texture2D)ResourceManager.Inst.GetTexture("UI/Game/marker.dds").GetTexture();
        }

        void CreateCompassTexture()
        {
            compassTexture = new Texture2D(GFX.Device, compassTextureWidth, compassTextureHeight, 1, TextureUsage.None, SurfaceFormat.Color);
            Color[] colorData = new Color[compassTextureWidth * compassTextureHeight];
            int lineIndex = (int)(((float)compassTextureWidth * (float)compassTickDelta) / 360.0f);
            int lineThickness = 4;
            for (int y = 0; y < compassTextureHeight; y++)
            {
                for (int x = 0; x < compassTextureWidth; x++)
                {
                    int index = x % lineIndex;
                    colorData[x + y * compassTextureWidth] = (index < lineThickness && x < (compassTextureWidth - lineThickness * 2)) ? Color.Black : Color.TransparentBlack;
                }
            }
            compassTexture.SetData<Color>(colorData);
        }

        void DrawMarkers(Vector2 min, Vector2 max, Vector2 minTC, Vector2 maxTC)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                Vector3 dir = markers[i].GetPosition() - currTransform.GetPosition();
                float theta = 0.5f + 0.5f * (float)Math.Atan2(dir.Z, dir.X) / MathHelper.Pi;
                if (theta < minTC.X)
                    theta++;
                if (minTC.X < theta && theta < maxTC.X)
                {
                    float lerpAmount = (theta - minTC.X) / (maxTC.X - minTC.X);
                    Vector2 textPos = Vector2.Lerp(min, max, lerpAmount);
                    textPos.Y = (min.Y + max.Y) * 0.5f;
                    Vector2 markerMin = textPos - Vector2.One * 0.05f;
                    Vector2 markerMax = textPos + Vector2.One * 0.05f;
                    GUIElement markerElem = new GUIElement(markerMin, markerMax, markerImage, new Vector4(1.0f, 0, 0, 1.0f));
                    GFX.Inst.GetGUI().AddElement(markerElem);
                }
            }
        }

        protected override void OnRender()
        {
            if (currTransform == null)
                return;

            base.OnRender();
            Vector2 min = this.position - this.scale; // new Vector2(-0.25f, 0.6f);
            Vector2 max = this.position + this.scale; //new Vector2(0.25f, 0.7f);

            float pivot = (float)compassTickDelta / 65.0f;

            Vector3 dir = currTransform.GetTransform().Forward;
            float theta = 0.5f + 0.5f * (float)Math.Atan2(dir.Z, dir.X) / MathHelper.Pi;

            Vector2 minTC = new Vector2(theta - pivot, 0);
            Vector2 maxTC = new Vector2(theta + pivot, 1);

            GUIElementTC compass = new GUIElementTC(min, max, compassTexture, Vector4.One, minTC, maxTC);
            GFX.Inst.GetGUI().AddElement(compass);

            for (int i = 0; i < compassDirs.Length; i++)
            {
                float currDirTC = compassTCs[i];
                if (currDirTC < minTC.X)
                    currDirTC++;
                if (minTC.X < currDirTC && currDirTC < maxTC.X)
                {
                    float lerpAmount = (currDirTC - minTC.X) / (maxTC.X - minTC.X);
                    Vector2 textPos = Vector2.Lerp(min, max, lerpAmount);
                    textPos.Y = (min.Y + max.Y) * 0.5f;
                    GUITextElement cardinalDirection = new GUITextElement(textPos, compassDirs[i], Vector4.One);
                    GFX.Inst.GetGUI().AddElement(cardinalDirection);
                }
            }

            DrawMarkers(min, max, minTC, maxTC);
            
            const float offsetY = 0.045f;
            float offsetX = offsetY * GFX.Inst.DisplayRes.Y / GFX.Inst.DisplayRes.X;
            Vector2[] corners = { new Vector2(min.X-offsetX, min.Y), new Vector2(min.X, max.Y),
                                  new Vector2(max.X, min.Y), new Vector2(max.X+offsetX, max.Y),
                                  new Vector2(min.X-offsetX, min.Y-offsetY), new Vector2(max.X+offsetX, min.Y),
                                  new Vector2(min.X-offsetX, max.Y), new Vector2(max.X+offsetX, max.Y+offsetY)
                                };
            int quadCount = corners.Length / 2;
            for (int i = 0; i < quadCount; i++)
            {
                int index = i * 2;
                GUIElement box = new GUIElement(corners[index], corners[index + 1], null, Vector3.One * 0.15f);
                GFX.Inst.GetGUI().AddElement(box);
            }
            
        }
    }
}
