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
            Element.IndexBuffer = GFXPrimitives.Quad.GetInstanceIndexBufferDoubleSided();
            Element.VertexBuffer = GFXPrimitives.Quad.GetInstanceVertexBuffer();
            Element.VertexCount = 4;
            Element.VertexDec = GFXVertexDeclarations.PTIDec;
            Element.VertexStride = VertexPTI.SizeInBytes;
            Element.StartVertex = 0;
            Element.IsAnimated = false;
            Element.PrimitiveCount = 4;
        }
    }

    public class ClutterPlacement
    {
        TerrainHeightmap terrain;
        RenderView view;
        Material grassMaterial;
        SortedList<int, GrassPatch> patches = new SortedList<int, GrassPatch>();
        GrassPatch patch;
        int numPatches = 3;
        int grassPerPatch = 32;

        public ClutterPlacement(TerrainHeightmap terrain, RenderView renderView)
        {
            this.terrain = terrain;
            this.view = renderView;
            grassMaterial = ResourceManager.Inst.GetMaterial("GrassMat0");
            
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
            int width = terrain.GetWidth() / grassPerPatch;
            int depth = terrain.GetDepth() / grassPerPatch;

            int camX = (int)MathHelper.Clamp(camPosTerrain.X * width, 0, width - 1);
            int camZ = (int)MathHelper.Clamp(camPosTerrain.Z * depth, 0, depth - 1);

            int halfCount = numPatches / 2;
            int startX = Math.Min(width - 1, Math.Max(0, camX - halfCount));
            int startZ = Math.Min(depth - 1, Math.Max(0, camZ - halfCount));

            int endX = Math.Min(width - 1, Math.Max(0, camX + halfCount));
            int endZ = Math.Min(depth - 1, Math.Max(0, camZ + halfCount));

            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {
                    int index = x + z * width;
                    if (!patches.ContainsKey(index))
                        patches.Add(index, CreatePatchAtPoint(x * grassPerPatch, z * grassPerPatch));
                    
                }
            }
            /*
            patch = CreatePatchAtPoint(camX, camZ);
            
            List<Vector3> positions = new List<Vector3>();

            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {
                    Vector3 normal = Vector3.Up;
                    float height = terrain.GetHeightValue(x, z);
                    terrain.ComputeVertexNormal(x, z, out normal);
                    Vector3 posWorldSpace = Vector3.Transform(new Vector3(((float)x / (float)width)*2.0f-1.0f, height, ((float)z / (float)depth)*2.0f-1.0f), terrain.Transformation.GetTransform());
                    positions.Add(posWorldSpace);
                }
            }
            patch = new GrassPatch();
            patch.Element.Transform = new Matrix[positions.Count];
            for (int i = 0; i < patch.Element.Transform.Length; i++)
            {
                patch.Element.Transform[i] = Matrix.CreateScale(Vector3.One * 5.0f) * Matrix.CreateConstrainedBillboard(positions[i], view.GetPosition(), Vector3.Up, null, null);
                patch.Element.Transform[i].Translation = positions[i];
            }
            */
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
