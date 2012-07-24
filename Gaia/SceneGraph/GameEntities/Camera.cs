using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Gaia.Input;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.Physics;
using Gaia.Core;
using Gaia.Resources;


namespace Gaia.SceneGraph.GameEntities
{
    public enum CameraFlags : int
    {
        USEINPUT = 1,
        USETARGET = 2,
        ORTHOGRAPHIC = 4,
        USEPHYSICS = 8,
        ORBITMODE = 16,
    }
    public class Camera : Entity
    {
        MainRenderView renderView;

        float aspectRatio;
        float fieldOfView;

        float forwardSpeed = 18.5f;
        float backwardSpeed = 7.5f;
        float strafeSpeed = 15f;
        float targetDistance = 15;

        int cameraBitFlag = (int)CameraFlags.USEINPUT;

        const float TARGET_SWITCH_SPEED = 1.35f;
        Vector3 target = Vector3.Forward;

        public void SetTarget(Vector3 position, bool enabled)
        {
            if (enabled)
            {
                target = position;
                cameraBitFlag |= (int)CameraFlags.USETARGET;
            }
            else
            {
                cameraBitFlag -= (cameraBitFlag & (int)CameraFlags.USETARGET);
            }
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);

            renderView = new MainRenderView(scene, Matrix.Identity, Matrix.Identity, Vector3.Zero, 1.0f, 1000);

            scene.MainCamera = renderView;
            scene.AddRenderView(renderView);

            fieldOfView = MathHelper.ToRadians(60);
            aspectRatio = GFX.Inst.DisplayRes.X / GFX.Inst.DisplayRes.Y;
        }

        public override void OnDestroy()
        {
            scene.RemoveRenderView(renderView);
            base.OnDestroy();
        }

        void HandleControls()
        {
            Matrix transform;
            if ((cameraBitFlag & (int)CameraFlags.USETARGET) == 0)
            {
                Vector3 rotation = this.Transformation.GetRotation();
                if (InputManager.Inst.IsRightMouseDown())
                {
                    Vector2 delta = InputManager.Inst.GetMouseDisplacement();
                    rotation.Y += delta.X;
                    rotation.X = MathHelper.Clamp(rotation.X + delta.Y, -1.4f, 1.4f);
                    if (rotation.Y > MathHelper.TwoPi)
                        rotation.Y -= MathHelper.TwoPi;
                    if (rotation.Y < 0)
                        rotation.Y += MathHelper.TwoPi;
                    this.Transformation.SetRotation(rotation);
                }
                rotation = this.Transformation.GetRotation();
                transform = Matrix.CreateRotationX(rotation.X) * Matrix.CreateRotationY(rotation.Y);// *Matrix.CreateRotationZ(rotation.Z);
                target = transform.Forward + this.Transformation.GetPosition();
            }
            else
                transform = Matrix.CreateLookAt(this.Transformation.GetPosition(), this.target, Vector3.Up);
            
            Vector3 moveDir = Vector3.Zero;
            if (InputManager.Inst.IsKeyDown(GameKey.MoveFoward))
                moveDir += transform.Forward * forwardSpeed * (Math.Min(1.0f, 0.2f + InputManager.Inst.GetPressTime(GameKey.MoveFoward) / 3.0f));
            if (InputManager.Inst.IsKeyDown(GameKey.MoveBackward))
                moveDir -= transform.Forward * backwardSpeed * Math.Min(1.0f, 0.2f + InputManager.Inst.GetPressTime(GameKey.MoveBackward) / 1.75f);

            if (InputManager.Inst.IsKeyDown(GameKey.MoveRight))
                moveDir += transform.Right * strafeSpeed * Math.Min(1.0f, 0.2f + InputManager.Inst.GetPressTime(GameKey.MoveRight) / 1.25f);
            if (InputManager.Inst.IsKeyDown(GameKey.MoveLeft))
                moveDir -= transform.Right * strafeSpeed * Math.Min(1.0f, 0.2f + InputManager.Inst.GetPressTime(GameKey.MoveLeft) / 1.25f);

            this.Transformation.SetPosition(this.Transformation.GetPosition() + moveDir * Time.GameTime.ElapsedTime);
            
        }

        public override void OnUpdate()
        {
            if ((cameraBitFlag & (int)CameraFlags.USEINPUT) > 0)
            {
                HandleControls();
            }

            float nearPlaneSky = 0.075f;
            float nearPlane = 0.175f;
            float farPlane = 650;

            renderView.SetPosition(this.Transformation.GetPosition());
            renderView.SetView(Matrix.CreateLookAt(this.Transformation.GetPosition(), target, Vector3.Up));
            renderView.SetProjectionLocal(Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlaneSky, farPlane));
            renderView.SetProjection(Matrix.CreatePerspectiveFieldOfView(fieldOfView, aspectRatio, nearPlane, farPlane));
            renderView.SetNearPlane(nearPlane);
            renderView.SetFarPlane(farPlane);
            renderView.UpdateRenderViews(); //Update reflections

            base.OnUpdate();
        }
    }
}
