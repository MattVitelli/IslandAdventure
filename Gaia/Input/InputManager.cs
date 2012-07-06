using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Gaia.Core;
using Gaia.Rendering;

namespace Gaia.Input
{
    public enum GameKey
    {
        Sprint,
        MoveFoward,
        MoveBackward,
        MoveLeft,
        MoveRight,
        TurnUp,
        TurnDown,
        TurnLeft,
        TurnRight,
        Jump,
        Crouch,
        Pause,
        Fire,
        AltFire,
        Interact,
        ToggleCamera,
        DropPlayerAtCamera,
        DecreaseCameraHeight,
        IncreaseCameraHeight,
        LaunchEditor,
        ShowWeapons
    }

    public enum ExtendedKeys
    {
        None,
        MouseLeft,
        MouseRight,
        MouseMiddle,
        MouseWheelUp,
        MouseWheelDown
    }

    public class KeyStroke
    {
        bool keyDown;
        bool lastKeyDown;

        public Keys Key;
        public float TimeScale;
        public ExtendedKeys ExtendedKey;

        public KeyStroke(Keys _key)
        {
            keyDown = false;
            lastKeyDown = false;
            Key = _key;
            TimeScale = 0;
            ExtendedKey = ExtendedKeys.None;
        }

        public KeyStroke(ExtendedKeys _key)
        {
            keyDown = false;
            lastKeyDown = false;
            Key = Keys.F19;
            TimeScale = 0;
            ExtendedKey = _key;
        }

        public bool IsPressed
        {
            get { return keyDown; }
        }
        public bool IsPressedOnce
        {
            get { return (keyDown && !lastKeyDown); }
        }

        public bool IsReleasedOnce
        {
            get { return (!keyDown && lastKeyDown); }
        }

        public void Update()
        {
            lastKeyDown = keyDown;
            if (ExtendedKey != ExtendedKeys.None)
            {
                switch (ExtendedKey)
                {
                    case ExtendedKeys.MouseLeft:
                        keyDown = (InputManager.Inst.currentMouseState.LeftButton == ButtonState.Pressed);
                        break;
                    case ExtendedKeys.MouseRight:
                        keyDown = (InputManager.Inst.currentMouseState.RightButton == ButtonState.Pressed);
                        break;
                    case ExtendedKeys.MouseMiddle:
                        keyDown = (InputManager.Inst.currentMouseState.MiddleButton == ButtonState.Pressed);
                        break;
                    case ExtendedKeys.MouseWheelDown:
                        keyDown = (InputManager.Inst.currentMouseState.ScrollWheelValue - InputManager.Inst.lastMouseState.ScrollWheelValue) < 0;
                        break;
                    case ExtendedKeys.MouseWheelUp:
                        keyDown = (InputManager.Inst.currentMouseState.ScrollWheelValue - InputManager.Inst.lastMouseState.ScrollWheelValue) > 0;
                        break;
                }
            }
            else
                keyDown = InputManager.Inst.currentKeyState.IsKeyDown(Key);

            if (IsPressed)
                TimeScale += Time.GameTime.ElapsedTime;
            else
                TimeScale = 0;

        }
    }

    public class InputManager
    {
        SortedList<GameKey, KeyStroke> keyList = new SortedList<GameKey, KeyStroke>();
        public KeyboardState currentKeyState;
        public KeyboardState lastKeyState;
        public MouseState currentMouseState;
        public MouseState lastMouseState;
        Vector2 centerCoord;
        Vector2 displacement;
        float sensitivity = 0.0001f;
        static InputManager inst = null;
        public bool StickyInput = false;

        public static InputManager Inst
        {
            get { return inst; }
        }

        public InputManager()
        {
            inst = this;
            InitializeInput();
        }

        public void InitializeInput()
        {
            keyList.Add(GameKey.Jump, new KeyStroke(Keys.Space));
            keyList.Add(GameKey.Crouch, new KeyStroke(Keys.LeftControl));
            keyList.Add(GameKey.MoveFoward, new KeyStroke(Keys.W));
            keyList.Add(GameKey.MoveBackward, new KeyStroke(Keys.S));
            keyList.Add(GameKey.MoveLeft, new KeyStroke(Keys.A));
            keyList.Add(GameKey.MoveRight, new KeyStroke(Keys.D));
            keyList.Add(GameKey.TurnDown, new KeyStroke(Keys.Down));
            keyList.Add(GameKey.TurnUp, new KeyStroke(Keys.Up));
            keyList.Add(GameKey.TurnLeft, new KeyStroke(Keys.Left));
            keyList.Add(GameKey.TurnRight, new KeyStroke(Keys.Right));
            keyList.Add(GameKey.Sprint, new KeyStroke(Keys.LeftShift));
            keyList.Add(GameKey.Pause, new KeyStroke(Keys.Escape));
            keyList.Add(GameKey.Fire, new KeyStroke(ExtendedKeys.MouseLeft));
            keyList.Add(GameKey.AltFire, new KeyStroke(ExtendedKeys.MouseRight));
            keyList.Add(GameKey.Interact, new KeyStroke(Keys.E));
            keyList.Add(GameKey.ToggleCamera, new KeyStroke(Keys.C));
            keyList.Add(GameKey.DropPlayerAtCamera, new KeyStroke(Keys.Z));
            keyList.Add(GameKey.IncreaseCameraHeight, new KeyStroke(Keys.E));
            keyList.Add(GameKey.DecreaseCameraHeight, new KeyStroke(Keys.Q));
            keyList.Add(GameKey.ShowWeapons, new KeyStroke(Keys.Tab));
            keyList.Add(GameKey.LaunchEditor, new KeyStroke(Keys.F11));
        }

        public void Update()
        {
            lastKeyState = currentKeyState;
            currentKeyState = Keyboard.GetState();
            centerCoord = GFX.Inst.DisplayRes / 2;
            lastMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

            for (int i = 0; i < keyList.Count; i++)
            {
                keyList.Values[i].Update();
            }

            displacement = (centerCoord - new Vector2(currentMouseState.X, currentMouseState.Y)) / centerCoord;
            if (StickyInput)
                Mouse.SetPosition((int)centerCoord.X, (int)centerCoord.Y);
        }

        public Vector2 GetMouseDisplacement()
        {
            return displacement;
        }

        public Vector2 GetMousePosition()
        {
            return new Vector2(currentMouseState.X, currentMouseState.Y);
        }

        public Vector2 GetMousePositionHomogenous()
        {
            Vector2 mousePos = (new Vector2(currentMouseState.X, currentMouseState.Y) / GFX.Inst.DisplayRes) * 2.0f - Vector2.One;
            return mousePos * new Vector2(1, -1);
        }

        public bool IsLeftMouseDown()
        {
            return (currentMouseState.LeftButton == ButtonState.Pressed);
        }

        public bool IsLeftMouseDownOnce()
        {
            return (currentMouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released);
        }

        public bool IsLeftJustReleased()
        {
            return (currentMouseState.LeftButton == ButtonState.Released && lastMouseState.LeftButton == ButtonState.Pressed);
        }

        public bool IsRightMouseDown()
        {
            return (currentMouseState.RightButton == ButtonState.Pressed);
        }

        public bool IsRightMouseDownOnce()
        {
            return (currentMouseState.RightButton == ButtonState.Pressed && lastMouseState.RightButton == ButtonState.Released);
        }

        public bool IsRightJustReleased()
        {
            return (currentMouseState.RightButton == ButtonState.Released && lastMouseState.RightButton == ButtonState.Pressed);
        }

        public bool IsKeyDown(GameKey key)
        {
            return keyList[key].IsPressed;
        }

        public bool IsKeyDownOnce(GameKey key)
        {
            return keyList[key].IsPressedOnce;
        }

        public bool IsKeyUpOnce(GameKey key)
        {
            return keyList[key].IsReleasedOnce;
        }

        public float GetPressTime(GameKey key)
        {
            return keyList[key].TimeScale;
        }
    }
}
