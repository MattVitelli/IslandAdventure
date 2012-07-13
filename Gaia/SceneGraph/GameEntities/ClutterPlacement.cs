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
        public RenderElement Element;

        public BoundingBox Bounds;

        public GrassPatch()
        {
            Element = new RenderElement();
            Element.IndexBuffer = GFXPrimitives.Quad.GetInstanceIndexBuffer();
            Element.VertexBuffer = GFXPrimitives.Quad.GetInstanceVertexBuffer();
            Element.VertexCount = 4;
            Element.VertexDec = GFXVertexDeclarations.PTIDec;
            Element.VertexStride = VertexPTI.SizeInBytes;
            Element.StartVertex = 0;
            Element.IsAnimated = false;
            Element.PrimitiveCount = 4;
        }
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

    public struct ClimateInfo
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
        Material grassMaterial;
        TerrainClimate climate;
        SortedList<int, GrassPatch> patches = new SortedList<int, GrassPatch>();
        int numPatches = 3;
        int grassPerPatch = 32;
        int numPatchesX;
        int numPatchesZ;

        const int PLACEMENT_THRESHOLD = 3;

        ClimateInfo[] plantSpots;

        public ClutterPlacement(TerrainHeightmap terrain, RenderView renderView)
        {
            this.terrain = terrain;
            this.view = renderView;
            this.climate = terrain.GetClimate();
            grassMaterial = ResourceManager.Inst.GetMaterial("GrassMat0");
            BuildPlantSpots();
        }

        public void BuildPlantSpots()
        {
            int width = terrain.GetWidth();
            int depth = terrain.GetDepth();
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

                    int endX = startX + grassPerPatch;
                    int endZ = startZ + grassPerPatch;

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
                            if(locations[l].Count > 0)
                                plantSpots[index].AvailableSpots.Add(currBlendIndex + l, locations[l]);
                        }
                    }
                    
                }
            }
        }

        public GrassPatch CreatePatchAtPoint(int xOrigin, int zOrigin)
        {
            GrassPatch currPatch = new GrassPatch();

            int width = terrain.GetWidth();
            int depth = terrain.GetDepth();

            int startX = Math.Min(width - 1, Math.Max(0, xOrigin));
            int startZ = Math.Min(depth - 1, Math.Max(0, zOrigin));

            int endX = Math.Min(width - 1, Math.Max(0, xOrigin + grassPerPatch));
            int endZ = Math.Min(depth - 1, Math.Max(0, zOrigin + grassPerPatch));

            int index = 0;
            currPatch.Element.Transform = new Matrix[(endX-startX) * (endZ-startZ)];
            Vector3 minPos = Vector3.One * float.PositiveInfinity;
            Vector3 maxPos = Vector3.One * float.NegativeInfinity;
            for (int x = startX; x < endX; x++)
            {
                for (int z = startZ; z < endZ; z++)
                {
                    Vector3 normal = Vector3.Up;
                    float height = terrain.GetHeightValue(x, z);
                    terrain.ComputeVertexNormal(x, z, out normal);
                    float randX = (float)x + (float)RandomHelper.RandomGen.NextDouble();
                    float randZ = (float)z + (float)RandomHelper.RandomGen.NextDouble();
                    Vector3 posWorldSpace = Vector3.Transform(new Vector3((randX / (float)width) * 2.0f - 1.0f, height, (randZ / (float)depth) * 2.0f - 1.0f), terrain.Transformation.GetTransform());
                    currPatch.Element.Transform[index] = Matrix.CreateScale(5.0f);
                    currPatch.Element.Transform[index].Translation = posWorldSpace;
                    index++;

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
            Vector3 camPosTerrain = Vector3.Transform(camPos, terrain.Transformation.GetObjectSpace());
            camPosTerrain = (camPosTerrain + Vector3.One) * 0.5f;
            
            int camX = (int)MathHelper.Clamp(camPosTerrain.X * numPatchesX, 0, numPatchesX - 1);
            int camZ = (int)MathHelper.Clamp(camPosTerrain.Z * numPatchesZ, 0, numPatchesZ - 1);

            int halfCount = numPatches / 2;
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
                        patches.Add(index, CreatePatchAtPoint(x * grassPerPatch, z * grassPerPatch));
                    
                }
            }
        }

        public void OnUpdate()
        {
            Vector3 camPos = view.GetPosition();
            Vector3 halfBox = new Vector3((float)grassPerPatch / (float)terrain.GetWidth(), 1, (float)grassPerPatch / (float)terrain.GetDepth());
            halfBox *= (float)numPatches*0.75f*terrain.Transformation.GetScale();
            BoundingBox clipBounds = new BoundingBox(camPos-halfBox, camPos+halfBox);

            for (int i = 0; i < patches.Keys.Count; i++)
            {
                int key = patches.Keys[i];
                GrassPatch currPatch = patches[key];

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
            for (int j = 0; j < patches.Count; j++)
            {
                if(frustum.Contains(patches.Values[j].Bounds) != ContainmentType.Disjoint)
                    renderView.AddElement(grassMaterial, patches.Values[j].Element);
            }
        }
    }
}
