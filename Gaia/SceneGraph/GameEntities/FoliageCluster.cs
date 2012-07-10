using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Core;
using Gaia.Rendering;
using Gaia.Resources;
using Gaia.Voxels;

namespace Gaia.SceneGraph.GameEntities
{
    public class Cluster
    {
        public BoundingBox Bounds;
        public Matrix[] Transform;
    }

    public class FoliageCluster : Entity
    {
        List<Material> materials = new List<Material>();
        SortedList<Material, List<Cluster>> clusters = new SortedList<Material,List<Cluster>>();
        RenderElement[] renderElements;
        int minSides = 1;
        int maxSides = 5;
        int clusterSize;
        public Vector3 minScale = Vector3.One * 5.35f;
        public Vector3 maxScale = Vector3.One * 37.5f;
        BoundingBox region;

        public BoundingBox GetRegion()
        {
            return region;
        }

        public void SetRegion(BoundingBox region)
        {
            this.region = region;
        }

        public void SetClusterSize(int clusterSize)
        {
            this.clusterSize = clusterSize;
        }

        public void SetSides(int minSides, int maxSides)
        {
            this.minSides = minSides;
            this.maxSides = maxSides;
        }

        public void SetDimensions(Vector3 minScale, Vector3 maxScale)
        {
            this.minScale = minScale;
            this.maxScale = maxScale;
        }

        public FoliageCluster(int clusterSize, int minSides, int maxSides)
        {
            this.minSides = minSides;
            this.maxSides = maxSides;
            this.clusterSize = clusterSize;
            
        }

        public void AddMaterial(Material material)
        {
            materials.Add(material);
        }

        void CreateRenderElements()
        {
            renderElements = new RenderElement[materials.Count];
            for (int i = 0; i < renderElements.Length; i++)
            {
                renderElements[i] = new RenderElement();
                renderElements[i].VertexCount = 4;
                renderElements[i].PrimitiveCount = 4;
                renderElements[i].StartVertex = 0;
                renderElements[i].VertexStride = VertexPTI.SizeInBytes;
                renderElements[i].VertexDec = GFXVertexDeclarations.PTIDec;
                renderElements[i].VertexBuffer = GFXPrimitives.Quad.GetInstanceVertexBuffer();
                renderElements[i].IndexBuffer = GFXPrimitives.Quad.GetInstanceIndexBufferDoubleSided();
            }
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            CreateRenderElements();
            InitializeClusters();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            for (int i = 0; i < clusters.Count; i++)
                clusters.Values[i].Clear();
            clusters.Clear();
        }

        void RandomizeOrientation(Cluster cluster, Vector3 position, Vector3 surfaceNormal)
        {

            cluster.Transform = new Matrix[RandomHelper.RandomGen.Next(minSides, maxSides)];
            Vector3 randScale = Vector3.Lerp(minScale, maxScale, (float)RandomHelper.RandomGen.NextDouble());
            cluster.Bounds.Min = Vector3.One * float.PositiveInfinity;
            cluster.Bounds.Max = Vector3.One * float.NegativeInfinity;
            Vector3 fwd = new Vector3(surfaceNormal.Z, surfaceNormal.X, surfaceNormal.Y);
            fwd = Vector3.Normalize(fwd - Vector3.Dot(fwd, surfaceNormal) * surfaceNormal);
            Vector3 right = Vector3.Cross(fwd, surfaceNormal);
            for (int i = 0; i < cluster.Transform.Length; i++)
            {
                float randAngle = MathHelper.TwoPi * (float)RandomHelper.RandomGen.NextDouble();
                Matrix orientation = Matrix.Identity;
                
                orientation.Up = surfaceNormal;


                orientation.Right = right;// Vector3.Normalize(new Vector3(normal.Z, normal.X, normal.Y));
                orientation.Forward = fwd;// Vector3.Normalize(Vector3.Cross(worldMatrix.Up, worldMatrix.Right));
                //orientation *= Matrix.CreateFromAxisAngle(surfaceNormal, randAngle);
                cluster.Transform[i] = Matrix.CreateScale(randScale) * Matrix.CreateRotationY(randAngle) * orientation;
                /*
                cluster.Transform[i] = Matrix.CreateRotationY(randAngle);
                cluster.Transform[i].Up = surfaceNormal;
                cluster.Transform[i] *= Matrix.CreateScale(randScale);
                */
                cluster.Transform[i].Translation = position;// +surfaceNormal * randScale;
                Vector3 min = Vector3.Transform(new Vector3(-1,-1,-0.15f), cluster.Transform[i]);
                Vector3 max = Vector3.Transform(new Vector3(1, 1, 0.15f), cluster.Transform[i]);
                cluster.Bounds.Min = Vector3.Min(min, cluster.Bounds.Min);
                cluster.Bounds.Max = Vector3.Max(max, cluster.Bounds.Max);
            }
        }

        public void InitializeClusters()
        {
            clusters.Clear();
            List<TriangleGraph> availableTriangles;
            if (scene.MainTerrain.GetTrianglesInRegion(RandomHelper.RandomGen, out availableTriangles, region))
            {
                for (int i = 0; i < clusterSize; i++)
                {
                    Material mat = materials[RandomHelper.RandomGen.Next(materials.Count)];

                    if (!clusters.ContainsKey(mat))
                    {
                        clusters.Add(mat, new List<Cluster>());
                    }

                    int randomIndex = RandomHelper.RandomGen.Next(i % availableTriangles.Count, availableTriangles.Count);
                    TriangleGraph triangle = availableTriangles[randomIndex];
                    Vector3 position = triangle.GeneratePointInTriangle(RandomHelper.RandomGen);
                    Vector3 normal = triangle.Normal;
                    //position = Vector3.Transform(position, scene.MainTerrain.Transformation.GetTransform());
                    Cluster cluster = new Cluster();
                    RandomizeOrientation(cluster, position, normal);
                    clusters[mat].Add(cluster);
                }
            }
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            BoundingFrustum frustum = view.GetFrustum();
            if (frustum.Contains(region) == ContainmentType.Disjoint)
                return;

            for (int i = 0; i < clusters.Keys.Count; i++)
            {
                Material key = clusters.Keys[i];
                
                List<Matrix> elemsMatrix = new List<Matrix>();
                
                for (int j = 0; j < clusters[key].Count; j++)
                {
                    if(frustum.Contains(clusters[key][j].Bounds) != ContainmentType.Disjoint)
                    {
                        elemsMatrix.AddRange(clusters[key][j].Transform);
                    }
                }

                renderElements[i].Transform = elemsMatrix.ToArray();
                view.AddElement(key, renderElements[i]);
            }
            base.OnRender(view);
        }


    }
}
