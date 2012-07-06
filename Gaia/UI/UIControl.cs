using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Gaia.UI
{
    public abstract class UIControl
    {
        protected UIControl parent;

        protected List<UIControl> children = new List<UIControl>();

        protected bool isVisible = true;

        protected bool isFocused = false;

        protected bool canFocusOnAnimation = false;

        protected uint animationFlag = 0;

        protected float[] animationDT = new float[(int)UIAnimationFlags.Count];

        protected float[] animationElapsedTime = new float[(int)UIAnimationFlags.Count];

        protected Vector4 color = Vector4.One;

        public void SetColor(Vector4 color)
        {
            this.color = color;
        }


        protected Vector2 position = Vector2.Zero;

        protected float rotation = 0;

        protected Vector2 scale = Vector2.One;

        protected bool updateMatrix = true;

        protected Matrix transformMatrix = Matrix.Identity;

        public Vector2 Position
        {
            get { return position; }
            set { position = value; updateMatrix = true; }
        }

        public float Rotation
        {
            get { return rotation; }
            set { rotation = value; updateMatrix = true; }
        }

        public Vector2 Scale
        {
            get { return scale; }
            set { scale = value; updateMatrix = true; }
        }

        
        protected Vector4[] animationColors = new Vector4[2];

        protected Vector2[] animationOrigins = new Vector2[2];

        protected Vector2[] animationScales = new Vector2[2];

        protected float[] animationRotations = new float[2];


        public event EventHandler OnClick;

        public event EventHandler OnHover;

        public event EventHandler OnMouseMove;

        public event EventHandler OnFocused;

        public event EventHandler OnDefocused;

        protected bool prevMouseDown = false;

        protected Vector2 prevMousePos;

        public void SetAnimationFlags(uint flag)
        {
            animationFlag = flag;
        }

        protected bool UpdateAnimationFlag(int index, uint flag)
        {
            if ((animationFlag & flag) > 0)
            {
                if (animationElapsedTime[index] >= 1.0f)
                    animationFlag = (animationFlag & ~(uint)UIAnimationFlags.AnimateColor); //Turn off animation as soon as we reach the end
                return true;
            }
            return false;
        }

        protected bool UpdateAnimation(float timeDT)
        {
            bool anyChange = false;
            for (int i = 0; i < (int)UIAnimationFlags.Count; i++)
            {
                animationElapsedTime[i] = MathHelper.Clamp(animationElapsedTime[i] + animationDT[i] * timeDT, 0.0f, 1.0f);
                bool updatedAnimation = false;
                switch (i)
                {
                    case 0:
                        updatedAnimation = UpdateAnimationFlag(i, (uint)UIAnimationFlags.AnimateColor);
                        color = Vector4.Lerp(animationColors[0], animationColors[1], animationElapsedTime[i]);
                        break;
                    case 1:
                        updatedAnimation = UpdateAnimationFlag(i, (uint)UIAnimationFlags.AnimatePosition);
                        position = Vector2.Lerp(animationOrigins[0], animationOrigins[1], animationElapsedTime[i]);
                        break;
                    case 2:
                        updatedAnimation = UpdateAnimationFlag(i, (uint)UIAnimationFlags.AnimateRotation);
                        rotation = MathHelper.Lerp(animationRotations[0], animationRotations[1], animationElapsedTime[i]);
                        break;
                    case 3:
                        updatedAnimation = UpdateAnimationFlag(i, (uint)UIAnimationFlags.AnimateScale);
                        scale = Vector2.Lerp(animationScales[0], animationScales[1], animationElapsedTime[i]);
                        break;
                }
                anyChange |= updatedAnimation;
            }
            updateMatrix |= anyChange;
            return anyChange;
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
        }

        public void AddChild(UIControl control)
        {
            children.Add(control);
            control.OnAdd(this);
        }

        public void RemoveChild(UIControl control)
        {
            if (children.Contains(control))
                children.Remove(control);
            control.OnRemove();
        }

        public virtual void OnAdd(UIControl parent) 
        { 
            this.parent = parent; 
        }

        public virtual void OnRemove() 
        { 

        }

        public virtual bool IsCollision(Vector2 point)
        {
            Vector2 minCorner = this.position - this.scale;// Vector4.Transform(Vector4.One * -1, transformMatrix);
            Vector2 maxCorner = this.position + this.scale;// Vector4.Transform(Vector4.One, transformMatrix);
 
            return (minCorner.X <= point.X && point.X <= maxCorner.X
                && minCorner.Y <= point.Y && point.Y <= maxCorner.Y);
        }

        protected virtual void UpdateFocus()
        {
            Vector2 cursorPosition = Input.InputManager.Inst.GetMousePositionHomogenous();
            bool mouseClicked = Input.InputManager.Inst.IsKeyDownOnce(Gaia.Input.GameKey.Fire);
            if(IsCollision(cursorPosition))
            {
                if (mouseClicked)
                {
                    if(OnClick != null)
                        OnClick(this, null);
                }
                else
                {
                    if(OnHover != null)
                        OnHover(this, null);
                }
            }

        }

        void UpdateTransform()
        {
            if (!updateMatrix)
                return;
            
        }

        public virtual void OnUpdate(float timeDT) 
        {
            //if (!UpdateAnimation(timeDT) || canFocusOnAnimation)
            {
                UpdateFocus();
            }

            UpdateTransform();

            for (int i = 0; i < children.Count; i++)
                children[i].OnUpdate(timeDT);
        }

        protected virtual void OnRender()
        {

        }

        public virtual void OnDraw() 
        {
            if (!isVisible)
                return;

            OnRender();

            for (int i = 0; i < children.Count; i++)
                children[i].OnDraw();
        }
    }
}
