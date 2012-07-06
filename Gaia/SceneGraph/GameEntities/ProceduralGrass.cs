using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework; 

namespace Gaia.SceneGraph.GameEntities
{
    public class ProceduralGrass : Entity
    {
        FoliageCluster[] grassIndices;
        int numGrassClusters = 5;
        Vector3 grassClusterSize = new Vector3(10, 10, 10);

        public override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}
