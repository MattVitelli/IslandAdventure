using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Rendering;
using Gaia.Resources;

namespace Gaia.UI
{
    public class UIList : UIControl
    {
        bool recomputeLayout = true;

        int scrollIndex = 0;

        int displayCount = 20;

        UIButton[] displayItems = null;

        UISlider slider;
        
        public int DisplayCount
        {
            get { return displayCount; }
            set { displayCount = value; recomputeLayout = true; updateSlider = true; }
        }

        Vector4 itemColor;
        public Vector4 ItemColor
        {
            get { return itemColor; }
            set
            {
                itemColor = value;
                if (displayItems != null)
                {
                    for (int i = 0; i < displayItems.Length; i++)
                    {
                        displayItems[i].SetTextColor(itemColor);
                    }
                }
            }
        }

        public List<string> Items = new List<string>();

        public Vector2 Position
        {
            get { return position; }
            set
            {
                position = value;
                updateSlider = true;
            }
        }

        public Vector2 Scale
        {
            get { return scale; }
            set 
            { 
                scale = value;
                updateSlider = true;
            }
        }
        bool updateSlider = true;

        public UIList()
        {
            slider = new UISlider();
            slider.SetScrollBarImage(ResourceManager.Inst.GetTexture("UI/Generic/ScrollBar.dds"));
            slider.SetSliderImage(ResourceManager.Inst.GetTexture("UI/Generic/Slider.dds"));
            
            this.children.Add(slider);
        }

        public void GetSelectedIndex()
        {

        }

        public void RecomputeLayout()
        {
            recomputeLayout = false;
           
            Vector2 minSize = this.position - this.scale;
            Vector2 maxSize = this.position + this.scale;
            Vector2 textSize = GFX.Inst.GetGUI().DefaultFont.MeasureString("A") / GFX.Inst.DisplayRes;

            displayCount = (int)(this.scale.Y * 2.0f / (textSize.Y * 3.0f));
            displayItems = new UIButton[displayCount];

            float deltaHeight = textSize.Y;
            for (int i = 0; ((i < displayCount) && ((scrollIndex + i) < Items.Count)); i++)
            {
                displayItems[i] = new UIButton(null, this.itemColor, Items[scrollIndex + i]);
                displayItems[i].Position = new Vector2(this.position.X, maxSize.Y - deltaHeight * 2 * (i+1));
                displayItems[i].Scale = new Vector2(scale.X, deltaHeight);
                displayItems[i].SetTextColor(itemColor);
            }
        }

        void UpdateList()
        {
            if (Items.Count > displayCount)
            {
                int newScrollIndex = (int)(slider.GetScrollPercentage() * (Items.Count - displayCount));
                if (newScrollIndex != scrollIndex)
                {
                    scrollIndex = newScrollIndex;
                    for (int i = 0; i < displayCount; i++)
                    {
                        displayItems[i].SetText(Items[scrollIndex + i]);
                    }
                }
            }
        }

        void UpdateSlider()
        {
            updateSlider = false;
            slider.SliderRatio = Math.Min((float)displayCount / (float)Items.Count, 1.0f);
            slider.Position = this.position + new Vector2(this.scale.X, 0);
            slider.Scale = new Vector2(0.05f, this.scale.Y);
        }

        public override void OnUpdate(float timeDT)
        {
            base.OnUpdate(timeDT);
            if (recomputeLayout)
            {
                RecomputeLayout();
            }

            if (updateSlider)
            {
                UpdateSlider();
            }

            UpdateList();
        }

        protected override void OnRender()
        {
            base.OnRender();
            GUIElement element = new GUIElement(position - scale, position + scale, null, color);
            GFX.Inst.GetGUI().AddElement(element);

            if (displayItems != null)
            {
                for (int i = 0; i < displayItems.Length; i++)
                {
                    if(displayItems[i] != null)
                        displayItems[i].OnDraw();
                }
            }
        }
    }
}
