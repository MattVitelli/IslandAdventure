using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gaia.SceneGraph.GameEntities
{
    public class NPCActor : Actor
    {
        ViewModel model;

        void UpdateAnimation()
        {

        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            UpdateAnimation();
        }
    }
}
