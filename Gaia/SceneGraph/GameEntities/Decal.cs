using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Gaia.Rendering;
using Gaia.Resources;
using Gaia.Core;
using Gaia.Rendering.RenderViews;

namespace Gaia.SceneGraph.GameEntities
{
    public class Decal : Entity
    {
        Material material;

        Vector3 normal;
        Vector2 scale;

        bool updateTransform = true;

        public Vector2 Scale { get { return scale; } set { scale = value; updateTransform = true; } }

        public Vector3 Normal { get { return normal; } set { normal = Vector3.Normalize(value); updateTransform = true; } }

        Matrix transform = Matrix.Identity;

        BoundingBox bounds;



        const float DefaultLifeTime = 30;

        public bool IsPersistent;

        public float LifeTime = DefaultLifeTime;

        public void SetMaterial(string materialName)
        {
            material = ResourceManager.Inst.GetMaterial(materialName);
        }

        public Decal()
        {
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!IsPersistent)
            {
                LifeTime -= Time.GameTime.ElapsedTime;
                if (LifeTime <= 0)
                    scene.RemoveEntity(this);
            }

            if (updateTransform)
            {
                updateTransform = false;
                transform = Matrix.Identity;
                transform.Up = Normal;
                transform.Right = new Vector3(normal.Y, normal.Z, normal.X);
                transform.Forward = new Vector3(normal.Z, normal.X, normal.Y);
                transform = Matrix.CreateScale(new Vector3(scale.X, 1, scale.Y)) * transform;
                transform.Translation = Transformation.GetPosition();
                Matrix boundsTransform = Matrix.CreateScale(Math.Max(scale.X, scale.Y));
                boundsTransform.Translation = Transformation.GetPosition();
                bounds.Min = Vector3.Transform(Vector3.One * -1, boundsTransform);
                bounds.Max = Vector3.Transform(Vector3.One, boundsTransform);
            }
        }

        public override void OnRender(RenderView view)
        {
            base.OnRender(view);
            if (view.GetRenderType() == RenderViewType.MAIN)
            {
                if (view.GetFrustum().Contains(bounds) != ContainmentType.Disjoint)
                {
                    MainRenderView renderView = (MainRenderView)view;
                    renderView.AddDecalElement(material, transform);
                }
            }
        }
    }
}
