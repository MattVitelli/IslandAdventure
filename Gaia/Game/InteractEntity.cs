using System;
using System.Collections.Generic;
using Gaia.SceneGraph;

namespace Gaia.Game
{
    public abstract class InteractNode
    {
        public virtual void OnInteract() { }

        public virtual string GetInteractText() { return "Examine NULL"; }
    }
}
