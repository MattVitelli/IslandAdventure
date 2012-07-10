using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Resources;
using Gaia.Rendering.Geometry;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering;
namespace Gaia.SceneGraph.GameEntities
{
    public class GrassPlacement : Entity
    {
        Material[] grassMaterials;

        float grassScale = 10;
        int grassCount = 8;
        BoundingBox cameraClipBounds;

        SortedList<int, FoliageCluster> grassTransforms = new SortedList<int, FoliageCluster>();

        public override void OnAdd(Scene scene)
        {
            grassMaterials = new Material[] { ResourceManager.Inst.GetMaterial("GrassMat0") };
            base.OnAdd(scene);
        }

        public override void OnDestroy()
        {
            for (int i = 0; i < grassTransforms.Values.Count; i++)
                grassTransforms.Values[i].OnDestroy();

            base.OnDestroy();
        }

        void UpdateGrassPlacement()
        {
            Vector3 camPos = scene.MainCamera.GetPosition()/grassScale;

            int initX = (int)camPos.X;
            int initY = (int)camPos.Y;
            int initZ = (int)camPos.Z;

            int grassCountOver2 = grassCount / 2;

            for (int z = initZ - grassCountOver2; z < initZ + grassCountOver2; z++)
            {
                int zOff = grassCount * grassCount * z;
                for (int y = initY - grassCountOver2; y < initY + grassCountOver2; y++)
                {
                    int yOff = grassCount * y;

                    for (int x = initX - grassCountOver2; x < initX + grassCountOver2; x++)
                    {
                        int idx = x + yOff + zOff;

                        if (!grassTransforms.ContainsKey(idx))
                        {
                            FoliageCluster cluster = new FoliageCluster(20, 2, 4);
                            for (int i = 0; i < grassMaterials.Length; i++)
                                cluster.AddMaterial(grassMaterials[i]);
                            cluster.SetDimensions(new Vector3(2.75f, 1.45f, 2.75f), new Vector3(5.35f, 3.5f, 5.35f));
                            cluster.SetRegion(new BoundingBox(new Vector3(x, y, z)*grassScale, new Vector3(x+1, y+1, z+1)*grassScale));
                            cluster.OnAdd(this.scene);

                            grassTransforms.Add(idx, cluster);
                        }
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            for (int i = 0; i < grassTransforms.Values.Count; i++)
                grassTransforms.Values[i].OnUpdate();

            Vector3 camPos = scene.MainCamera.GetPosition();
            Vector3 halfBox = Vector3.One * grassScale * grassCount / 2.0f;
            cameraClipBounds.Min = camPos - halfBox;
            cameraClipBounds.Max = camPos + halfBox;
            
            for (int i = 0; i < grassTransforms.Keys.Count; i++)
            {
                int key = grassTransforms.Keys[i];
                FoliageCluster elem = grassTransforms[key];
                
                if (cameraClipBounds.Contains(elem.GetRegion()) == ContainmentType.Disjoint)
                {
                    grassTransforms[key].OnDestroy();
                    grassTransforms.Remove(key);
                }
            }
            
            UpdateGrassPlacement();
            
            base.OnUpdate();
        }

        public override void OnRender(RenderView view)
        {
            for (int i = 0; i < grassTransforms.Values.Count; i++)
                grassTransforms.Values[i].OnRender(view);
            base.OnRender(view);
        }
    }
}
