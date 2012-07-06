using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Gaia.Rendering;
using Gaia.Resources;

namespace Gaia.UI
{
    public class UIButton : UIControl
    {
        protected TextureResource buttonImage;
        protected Vector4 buttonColor = Vector4.One*0.5f;
        protected Vector4 textColor = Vector4.One;
        protected string buttonText = string.Empty;

        public TextureResource GetButtonImage() { return buttonImage; }

        public void SetButtonImage(TextureResource image)
        {
            buttonImage = image;
        }

        public Vector4 GetButtonColor() { return buttonColor; }

        public void SetButtonColor(Vector4 color)
        {
            buttonColor = color;
        }

        public string GetText() { return buttonText; }

        public void SetText(string text)
        {
            buttonText = text;
        }

        public Vector4 GetTextColor() { return textColor; }

        public void SetTextColor(Vector4 color)
        {
            textColor = color;
        }

        public UIButton(TextureResource image, Vector4 color, string text)
        {
            buttonImage = image;
            buttonColor = color;
            buttonText = text;
        }

        protected override void OnRender()
        {
            base.OnRender();
            Vector2 minSize = this.position - this.scale;
            Vector2 maxSize = this.position + this.scale;

            if (buttonImage != null)
            {
                GUIElement renderElement = new GUIElement(minSize, maxSize, buttonImage.GetTexture(), buttonColor);
                GFX.Inst.GetGUI().AddElement(renderElement);
            }

            if (buttonText != string.Empty)
            {
                GUITextElement textRenderElement = new GUITextElement(this.position, buttonText, textColor);
                GFX.Inst.GetGUI().AddElement(textRenderElement);
            }
            
        }
    }
}
