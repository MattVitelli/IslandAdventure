using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Rendering;
using Gaia.Resources;
using Gaia.Input;

namespace Gaia.UI
{
    public class UISlider : UIControl
    {
        TextureResource scrollBarImage;

        UIButton sliderButton = new UIButton(null, Vector4.One, string.Empty);

        public TextureResource GetScrollBarImage() { return scrollBarImage; }

        public void SetScrollBarImage(TextureResource image)
        {
            scrollBarImage = image;
        }

        public void SetSliderImage(TextureResource image)
        {
            sliderButton.SetButtonImage(image);
        }

        public float SliderRatio = 0.15f;

        bool isSliderHeld = false;

        bool updateSliderButton = true;

        public Vector2 Position
        {
            get { return position; }
            set
            {
                position = value;
                updateSliderButton = true;
            }
        }

        public Vector2 Scale
        {
            get { return scale; }
            set
            {
                scale = value;
                updateSliderButton = true;
            }
        }

        public UISlider()
        {
            AddChild(sliderButton);
        }

        void UpdateSliderButton()
        {
            updateSliderButton = false;
            sliderButton.Position = this.position;
            sliderButton.Scale = this.scale * new Vector2(1, SliderRatio);
        }

        public float GetScrollPercentage()
        {
            Vector2 minSize = this.position - this.scale + sliderButton.Scale;
            Vector2 maxSize = this.position + this.scale - sliderButton.Scale;
            return 1.0f - (sliderButton.Position.Y - minSize.Y) / (maxSize.Y - minSize.Y);
        }

        public override void OnUpdate(float timeDT)
        {
            base.OnUpdate(timeDT);

            if (updateSliderButton)
                UpdateSliderButton();

            Vector2 mousePos = InputManager.Inst.GetMousePositionHomogenous();
            if (sliderButton.IsCollision(mousePos) && InputManager.Inst.IsLeftMouseDownOnce())
            {
                isSliderHeld = true;
            }
            if (InputManager.Inst.IsLeftJustReleased())
                isSliderHeld = false;

            if (isSliderHeld)
            {
                Vector2 minSize = this.position - this.scale + sliderButton.Scale;
                Vector2 maxSize = this.position + this.scale - sliderButton.Scale;

                Vector2 newPos = sliderButton.Position;
                newPos.Y = MathHelper.Clamp(mousePos.Y, minSize.Y, maxSize.Y);
                sliderButton.Position = newPos;
            }
        }

        protected override void OnRender()
        {
            Vector2 minSize = this.position - this.scale;
            Vector2 maxSize = this.position + this.scale;

            if (scrollBarImage != null)
            {
                GUIElement scrollRenderElement = new GUIElement(minSize, maxSize, scrollBarImage.GetTexture(), color);
                GFX.Inst.GetGUI().AddElement(scrollRenderElement);
            }
            base.OnRender();
        }
    }
}
