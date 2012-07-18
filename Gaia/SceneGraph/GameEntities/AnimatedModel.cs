using System;
using System.Collections.Generic;
using Gaia.Animation;
using Gaia.Core;
using Microsoft.Xna.Framework;

namespace Gaia.SceneGraph.GameEntities
{
    public class AnimatedModel : Entity
    {
        public ViewModel Model;

        public AnimatedModel(string name)
        {
            Model = new ViewModel(name);
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            Model.SetTransform(this.Transformation);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            Model.OnUpdate();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            base.OnRender(view);
            Model.OnRender(view, true);
        }
    }
}
