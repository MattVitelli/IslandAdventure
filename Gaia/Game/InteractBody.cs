using System;
using System.Collections.Generic;
using JigLibX.Collision;
using JigLibX.Physics;

namespace Gaia.Game
{
    public class InteractSkinPredicate : CollisionSkinPredicate1
    {
        public override bool ConsiderSkin(CollisionSkin skin0)
        {
            if (skin0.Owner != null && (skin0.Owner is InteractBody))
                return true;

            else
                return false;
        }
    }

    public class InteractBody : Body
    {
        public InteractNode Node;

        public InteractBody(InteractNode node) : base()
        {
            this.Node = node;
        }
    }
}
