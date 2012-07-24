using System;
using System.Collections.Generic;

using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph;
using Gaia.Resources;
using Gaia.Core;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Gaia.SceneGraph.GameEntities
{
    public class GrassPatch
    {
        public RenderElement[] Elements;

        public Material[] Materials;

        public BoundingBox Bounds;

        public bool CanRender;
    }

    public struct Location
    {
        public int X;
        public int Y;

        public Location(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
    };

    public class ClimateInfo
    {
        public SortedList<int, List<Location>> AvailableSpots;

        public ClimateInfo(int dummy)
        {
            AvailableSpots = new SortedList<int, List<Location>>();
        }

        public void ShufflePlantSpots()
        {
            for (int i = 0; i < AvailableSpots.Count; i++)
            {
                if (AvailableSpots.Values[i] != null)
                {
                    for (int j = 0; j < AvailableSpots.Values[i].Count; j++)
                    {
                        Location tempLoc = AvailableSpots.Values[i][j];
                        int shuffleIndex = RandomHelper.RandomGen.Next(0, j);
                        AvailableSpots.Values[i][j] = AvailableSpots.Values[i][shuffleIndex];
                        AvailableSpots.Values[i][shuffleIndex] = tempLoc;
                    }
                }
            }
        }
    };

    public class ClutterPlacement
    {
        TerrainHeightmap terrain;
        RenderView view;
        TerrainClimate climate;
        SortedList<int, GrassPatch> patches = new SortedList<int, GrassPatch>();
        
        int numVisiblePatches = 15;
        int grassPerPatch = 16;
        int numPatchesX;
        int numPatchesZ;

        Vector3[] corners = new Vector3[8];

        const int PLACEMENT_THRESHOLD = 60;

        ClimateInfo[] plantSpots;

        public ClutterPlacement(TerrainHeightmap terrain, RenderView renderView)
        {
            this.terrain = terrain;
            this.view = renderView;
            this.climate = terrain.GetClimate();
            BuildPlantSpots();
        }

        public void BuildPlantSpots()
        {
            int width = terrain.GetWidth()-1;
            int depth = terrain.GetDepth()-1;
            numPatchesX = width / grassPerPatch;
            numPatchesZ = depth / grassPerPatch;
            plantSpots = new ClimateInfo[numPatchesX * numPatchesZ];

            List<Color[]> blendLayers = new List<Color[]>();
            Texture2D[] blendTextures = terrain.GetBlendMaps();

            Color[] blendData = new Color[width * depth];

            for (int i = 0; i < blendTextures.Length; i++)
            {
                blendTextures[i].GetData<Color>(blendData);
                blendLayers.Add(blendData);
            }

            for (int i = 0; i < numPatchesX; i++)
            {
                for (int j = 0; j < numPatchesZ; j++)
                {
                    int index = i + j * numPatchesX;

                    plantSpots[index] = new ClimateInfo(3);

                    int startX = i * grassPerPatch;
                    int startZ = j * grassPerPatch;

                    int endX = Math.Min(startX + grassPerPatch, width-1);
                    int endZ = Math.Min(startZ + grassPerPatch, depth-1);

                    for (int k = 0; k < blendLayers.Count; k++)
                    {
                        List<Location>[] locations = new List<Location>[4];
                        for (int l = 0; l < locations.Length; l++)
                            locations[l] = new List<Location>();

                        for (int x = startX; x < endX; x++)
                        {
                            for (int z = startZ; z < endZ; z++)
                            {
                                int blendIndex = x + z * width;
                                Color currColor = blendLayers[k][blendIndex];
                                Location currLoc = new Location(x, z);
                                if (currColor.R > PLACEMENT_THRESHOLD)
                                    locations[0].Add(currLoc);
                                if (currColor.G > PLACEMENT_THRESHOLD)
                                    locations[1].Add(currLoc);
                                if (currColor.B > PLACEMENT_THRESHOLD)
                                    locations[2].Add(currLoc);
                                if (currColor.A > PLACEMENT_THRESHOLD)
                                    locations[3].Add(currLoc);
                            }
                        }

                        int currBlendIndex = k * 4;
                        for (int l = 0; l < locations.Length; l++)
                        {
                            if (locations[l].Count > 0 && climate.ClutterMaterials[currBlendIndex + l] != null)
                                plantSpots[index].AvailableSpots.Add(currBlendIndex + l, locations[l]);
                        }
                    }
                    
                }
            }

            for (int i = 0; i < plantSpots.Length; i++)
                plantSpots[i].ShufflePlantSpots();
        }

        public GrassPatch CreatePatchAtPoint(int patchX, int patchZ)
        {
            int width = terrain.GetWidth()-1;
            int depth = terrain.GetDepth()-1;

            int xOrigin = patchX * grassPerPatch;
            int zOrigin = patchZ * grassPerPatch;

            int startX = Math.Min(width - 1, Math.Max(0, xOrigin));
            int startZ = Math.Min(depth - 1, Math.Max(0, zOrigin));

            int endX = Math.Min(width - 1, Math.Max(0, xOrigin + grassPerPatch));
            int endZ = Math.Min(depth - 1, Math.Max(0, zOrigin + grassPerPatch));

            GrassPatch currPatch = new GrassPatch();

            Vector3 minPos = Vector3.One * float.PositiveInfinity;
            Vector3 maxPos = Vector3.One * float.NegativeInfinity;

            ClimateInfo currClimate = plantSpots[patchX + patchZ * numPatchesX];

            currPatch.Elements = new RenderElement[currClimate.AvailableSpots.Count];
            currPatch.Materials = new Material[currClimate.AvailableSpots.Count];

            for(int i = 0; i < currClimate.AvailableSpots.Count; i++)
            {
                int currKey = currClimate.AvailableSpots.Keys[i];
                int numTransforms = (int)(currClimate.AvailableSpots[currKey].Count * climate.ClutterDensity[currKey]);
                currPatch.Elements[i] = GFXPrimitives.CreateBillboardElement();
                currPatch.Elements[i].Transform = new Matrix[numTransforms];
                currPatch.Materials[i] = climate.ClutterMaterials[currKey];
                for(int j = 0; j < numTransforms; j++)
                {
                    Location loc = currClimate.AvailableSpots[currKey][j];
                    float height = terrain.GetHeightValue(loc.X, loc.Y);
                    float randX = (float)loc.X + (float)RandomHelper.RandomGen.NextDouble();
                    float randZ = (float)loc.Y + (float)RandomHelper.RandomGen.NextDouble();
                    float randScale = MathHelper.Lerp(0.95f, 3.0f, (float)RandomHelper.RandomGen.NextDouble());
                    Vector3 posWorldSpace = new Vector3(randX, height, randZ);// Vector3.Transform(new Vector3((randX / (float)width) * 2.0f - 1.0f, height, (randZ / (float)depth) * 2.0f - 1.0f), terrain.Transformation.GetTransform());
                    currPatch.Elements[i].Transform[j] = Matrix.CreateScale(randScale);
                    currPatch.Elements[i].Transform[j].Translation = posWorldSpace;

                    minPos = Vector3.Min(posWorldSpace, minPos);
                    maxPos = Vector3.Max(posWorldSpace, maxPos);
                }
                
            }

            currPatch.Bounds = new BoundingBox(minPos, maxPos);

            return currPatch;
        }

        public void PlaceGrass()
        {
            Vector3 camPos = view.GetPosition();
            //Vector3 camPosTerrain = Vector3.Transform(camPos, terrain.Transformation.GetObjectSpace());
            //camPosTerrain = (camPosTerrain + Vector3.One) * 0.5f;
            Vector3 camPosTerrain = camPos / new Vector3(terrain.GetWidth(), 1, terrain.GetDepth());
            int camX = (int)MathHelper.Clamp(camPosTerrain.X * numPatchesX, 0, numPatchesX - 1);
            int camZ = (int)MathHelper.Clamp(camPosTerrain.Z * numPatchesZ, 0, numPatchesZ - 1);

            int halfCount = numVisiblePatches / 2;
            int startX = Math.Min(numPatchesX - 1, Math.Max(0, camX - halfCount));
            int startZ = Math.Min(numPatchesZ - 1, Math.Max(0, camZ - halfCount));

            int endX = Math.Min(numPatchesX - 1, Math.Max(0, camX + halfCount));
            int endZ = Math.Min(numPatchesZ - 1, Math.Max(0, camZ + halfCount));

            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {
                    int index = x + z * numPatchesX;
                    if (!patches.ContainsKey(index))
                        patches.Add(index, CreatePatchAtPoint(x, z));
                    
                }
            }
        }

        public void OnUpdate()
        {
            Vector3 camPos = view.GetPosition();
            Vector3 halfBox = new Vector3(grassPerPatch, terrain.MaximumHeight*0.5f, grassPerPatch);// new Vector3((float)grassPerPatch / (float)terrain.GetWidth(), 1, (float)grassPerPatch / (float)terrain.GetDepth());
            halfBox *= (float)numVisiblePatches*0.75f;//*terrain.Transformation.GetScale();
            BoundingBox clipBounds = new BoundingBox(camPos-halfBox, camPos+halfBox);

            for (int i = 0; i < patches.Keys.Count; i++)
            {
                int key = patches.Keys[i];
                GrassPatch currPatch = patches[key];
                Vector3[] corners = new Vector3[8];
                currPatch.Bounds.GetCorners(corners);
                currPatch.CanRender = false;
                for (int j = 0; j < corners.Length; j++)
                {
                    if (Vector3.Distance(corners[j], camPos) < GFXShaderConstants.GRASSFALLOFF)
                    {
                        currPatch.CanRender = true;
                        break;
                    }
                }

                if (clipBounds.Contains(currPatch.Bounds) == ContainmentType.Disjoint)
                {
                    patches.Remove(key);
                }
            }

            PlaceGrass();
        }

        public void OnRender(RenderView renderView)
        {
            BoundingFrustum frustum = renderView.GetFrustum();
            if (renderView.GetRenderType() != RenderViewType.MAIN)
                return;
            for (int j = 0; j < patches.Count; j++)
            {
                if (patches.Values[j].CanRender && frustum.Contains(patches.Values[j].Bounds) != ContainmentType.Disjoint)
                {
                    for (int i = 0; i < patches.Values[j].Elements.Length; i++)
                        renderView.AddElement(patches.Values[j].Materials[i], patches.Values[j].Elements[i]);
                }
            }
        }
    }
}
