using System;
using System.Collections.Generic;

using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;


namespace Gaia.Game
{
    public class Chest : Model
    {
        InteractBody interactBody;

        public Chest(string chestName, string modelName) : base(modelName)
        {
            this.interactBody = new InteractBody(new ChestNode(chestName));
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            if (collision != null)
            {
                interactBody.CollisionSkin = collision;
                collision.Owner = interactBody;
            }
        }
    }

    public class ChestNode : InteractNode
    {
        string chestName;

        public ChestNode(string name)
        {
            this.chestName = "Open " + name;
        }

        public override void OnInteract()
        {
            base.OnInteract();
        }

        public override string GetInteractText()
        {
            return chestName;
        }
    }
}
