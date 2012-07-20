using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Animation;
using Gaia.SceneGraph;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering.RenderViews;
using Gaia.Rendering;

namespace Gaia.SceneGraph.GameEntities
{
    public class ViewModel
    {
        protected Mesh mesh;
        protected VertexBuffer vertexBuffer;
        protected VertexPNTTI[] vertices;
        protected RenderElement[] renderElements;
        protected AnimationNode[] rootNodes;
        protected AnimationNode[] orderedNodes;
        protected SortedList<string, AnimationNode> nodes;
        protected SortedList<string, AnimationLayer> animationLayers = new SortedList<string, AnimationLayer>();
        protected SortedList<string, Vector3> defaultTranslations = new SortedList<string, Vector3>();
        protected SortedList<string, Vector3> defaultRotations = new SortedList<string, Vector3>();

        Transform transform;

        Matrix customMatrix = Matrix.Identity;

        public ViewModel(string name)
        {
            InitializeMesh(name);
        }

        public BoundingBox GetMeshBounds()
        {
            return mesh.GetBounds();
        }

        public void SetCustomMatrix(Matrix value)
        {
            customMatrix = value;
        }

        protected void InitializeMesh(string name)
        {
            mesh = ResourceManager.Inst.GetMesh(name);
            rootNodes = mesh.GetRootNodes(out nodes);

            int vertexCount = 0;
            VertexBuffer origBuffer = mesh.GetVertexBuffer(out vertexCount);
            vertices = new VertexPNTTI[vertexCount];
            origBuffer.GetData<VertexPNTTI>(vertices);
            vertexBuffer = new VertexBuffer(GFX.Device, VertexPNTTI.SizeInBytes * vertexCount, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPNTTI>(vertices);

            List<AnimationNode> orderedNodes = new List<AnimationNode>();
            for (int i = 0; i < nodes.Count; i++)
            {
                string currKey = nodes.Keys[i];
                defaultTranslations.Add(currKey, nodes[currKey].Translation);
                defaultRotations.Add(currKey, nodes[currKey].Rotation);
            }

            AnimationNode[] tempNodes = mesh.GetNodes();
            for(int i = 0; i < tempNodes.Length; i++)
            {
                orderedNodes.Add(nodes[tempNodes[i].Name]);
            }
            this.orderedNodes = orderedNodes.ToArray();
        }

        public void SetTransform(Transform transform)
        {
            this.transform = transform;
        }

        public void SetAnimationLayer(string name, float weight)
        {
            if (!animationLayers.ContainsKey(name))
                animationLayers.Add(name, new AnimationLayer(name, this, weight));
            else
            {
                animationLayers[name].Weight = weight;
            }
        }

        public void RemoveAnimationLayer(string name)
        {
            if (animationLayers.ContainsKey(name))
                animationLayers.Remove(name);
        }

        protected void UpdateAnimation(float timeDT)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                string currKey = nodes.Keys[i];
                nodes[currKey].Translation = defaultTranslations[currKey];
                nodes[currKey].Rotation = defaultRotations[currKey];
            }

            for (int i = 0; i < animationLayers.Count; i++)
            {
                animationLayers.Values[i].UpdateAnimation(timeDT, this.nodes);
            }

            for (int i = 0; i < rootNodes.Length; i++)
                rootNodes[i].ApplyTransform(ref customMatrix);

            for (int i = 0; i < vertices.Length; i++)
            {
                VertexPNTTI currVertex = mesh.GetVertex(i);
                int index = (int)currVertex.Index;
                vertices[i].Position = Vector3.Transform(currVertex.Position, orderedNodes[index].Transform);
                vertices[i].Normal = Vector3.TransformNormal(currVertex.Normal, orderedNodes[index].TransformIT);
                vertices[i].Tangent = Vector3.TransformNormal(currVertex.Tangent, orderedNodes[index].TransformIT);
                vertices[i].Index = 0;
            }
            vertexBuffer.SetData<VertexPNTTI>(vertices);
        }

        public void OnUpdate()
        {
            UpdateAnimation(Time.GameTime.ElapsedTime);
        }

        public void OnRender(RenderView view, bool performCulling)
        {
            if (rootNodes != null && rootNodes.Length > 0)
                mesh.Render(transform.GetTransform(), vertexBuffer, view, performCulling);
            else
                mesh.Render(transform.GetTransform(), view, performCulling);
        }
    }
}
