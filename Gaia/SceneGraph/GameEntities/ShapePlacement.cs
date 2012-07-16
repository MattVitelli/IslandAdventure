using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Resources;
using Gaia.Rendering.Geometry;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering;
using Gaia.Core;
using Gaia.Voxels;

namespace Gaia.SceneGraph.GameEntities
{
    public class ShapeCluster
    {
        public SortedList<int, List<Matrix>> Transforms = new SortedList<int, List<Matrix>>();
        public BoundingBox Bounds;
    }

    public class ShapePlacement : Entity
    {
        Mesh[] meshSets;
        float shapeFalloffScale = 10;
        int grassCount = 4;
        BoundingBox cameraClipBounds;

        SortedList<int, ShapeCluster> clusterCollection = new SortedList<int, ShapeCluster>();

        public override void OnAdd(Scene scene)
        {
            meshSets = new Mesh[] { ResourceManager.Inst.GetMesh("Bush")};//, ResourceManager.Inst.GetMesh("Cecropia") };
            shapeFalloffScale = GFXShaderConstants.GRASSFALLOFF / grassCount;
            base.OnAdd(scene);
        }

        ShapeCluster RandomizeOrientation(int clusterSize, BoundingBox region)
        {
            ShapeCluster cluster = null;
            List<TriangleGraph> availableTriangles;
            if (scene.MainTerrain.GetTrianglesInRegion(RandomHelper.RandomGen, out availableTriangles, region))
            {
                cluster = new ShapeCluster();
                cluster.Bounds.Min = Vector3.One * float.PositiveInfinity;
                cluster.Bounds.Max = Vector3.One * float.NegativeInfinity;
                for (int i = 0; i < clusterSize; i++)
                {
                    int randShape = RandomHelper.RandomGen.Next(0, meshSets.Length);
                    int randomIndex = RandomHelper.RandomGen.Next(i % availableTriangles.Count, availableTriangles.Count);
                    TriangleGraph triangle = availableTriangles[randomIndex];
                    Vector3 position = triangle.GeneratePointInTriangle(RandomHelper.RandomGen);
                    Vector3 normal = triangle.Normal;

                    Vector3 fwd = new Vector3(normal.Z, normal.X, normal.Y);
                    fwd = Vector3.Normalize(fwd - Vector3.Dot(fwd, normal) * normal);
                    Vector3 right = Vector3.Cross(fwd, normal);
                    float randAngle = MathHelper.TwoPi * (float)RandomHelper.RandomGen.NextDouble();
                    Matrix orientation = Matrix.Identity;

                    orientation.Up = normal;


                    orientation.Right = right;
                    orientation.Forward = fwd;
                    Vector3 randScale = Vector3.One * (((float)RandomHelper.RandomGen.NextDouble()+0.1f) * 5.0f);
                    Matrix transform = Matrix.CreateScale(randScale) * Matrix.CreateRotationY(randAngle) * orientation;
                    transform.Translation = position;
                    if (!cluster.Transforms.ContainsKey(randShape))
                        cluster.Transforms.Add(randShape, new List<Matrix>());
                    cluster.Transforms[randShape].Add(transform);
                    BoundingBox meshBounds = meshSets[randShape].GetBounds();
                    Vector3 min = Vector3.Transform(meshBounds.Min, transform);
                    Vector3 max = Vector3.Transform(meshBounds.Max, transform);
                    cluster.Bounds.Min = Vector3.Min(min, cluster.Bounds.Min);
                    cluster.Bounds.Max = Vector3.Max(max, cluster.Bounds.Max);
                }
            }
            return cluster;
        }

        void UpdatePlacement()
        {
            Vector3 camPos = scene.MainCamera.GetPosition() / shapeFalloffScale;

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

                        if (!clusterCollection.ContainsKey(idx))
                        {
                            BoundingBox clusterBounds = new BoundingBox(new Vector3(x, y, z)*shapeFalloffScale, new Vector3(x+1, y+1, z+1)*shapeFalloffScale);
                            ShapeCluster cluster = RandomizeOrientation(10, clusterBounds);
                            clusterCollection.Add(idx, cluster);
                        }
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            Vector3 camPos = scene.MainCamera.GetPosition();
            Vector3 halfBox = Vector3.One * shapeFalloffScale * grassCount / 2.0f;
            cameraClipBounds.Min = camPos - halfBox;
            cameraClipBounds.Max = camPos + halfBox;

            for (int i = 0; i < clusterCollection.Keys.Count; i++)
            {
                int key = clusterCollection.Keys[i];
                ShapeCluster elem = clusterCollection[key];
                
                if (elem != null && cameraClipBounds.Contains(elem.Bounds) == ContainmentType.Disjoint)
                {
                    clusterCollection.Remove(key);
                }
            }
            
            UpdatePlacement();
            
            base.OnUpdate();
        }

        public override void OnRender(RenderView view)
        {
            BoundingFrustum frustum = view.GetFrustum();
            for (int i = 0; i < clusterCollection.Values.Count; i++)
            {
                ShapeCluster currCluster = clusterCollection.Values[i];
                if (currCluster != null && frustum.Contains(currCluster.Bounds) != ContainmentType.Disjoint)
                {
                    for (int j = 0; j < currCluster.Transforms.Count; j++)
                    {
                        int currKey = currCluster.Transforms.Keys[j];
                        for (int l = 0; l < currCluster.Transforms[currKey].Count; l++)
                        {
                            meshSets[currKey].Render(currCluster.Transforms[currKey][l], view);
                        }
                    }
                }
            }
            base.OnRender(view);
        }
    }
}
