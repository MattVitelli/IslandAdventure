using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Animation;
using Gaia.SceneGraph;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering.RenderViews;

namespace Gaia.SceneGraph.GameEntities
{
    public class ViewModel
    {
        protected Mesh mesh;
        protected AnimationNode[] rootNodes;
        protected SortedList<string, AnimationNode> nodes;
        protected SortedList<string, AnimationLayer> animationLayers = new SortedList<string, AnimationLayer>();
        protected SortedList<string, Vector3> defaultTranslations = new SortedList<string, Vector3>();
        protected SortedList<string, Vector3> defaultRotations = new SortedList<string, Vector3>();

        Transform transform;

        bool useCustomMatrix = false;
        Matrix customMatrix = Matrix.Identity;

        public ViewModel(string name)
        {
            InitializeMesh(name);
        }

        public void SetCustomMatrix(Matrix value)
        {
            customMatrix = value;
            useCustomMatrix = true;
        }

        protected void InitializeMesh(string name)
        {
            mesh = ResourceManager.Inst.GetMesh(name);
            rootNodes = mesh.GetRootNodes(out nodes);
            for (int i = 0; i < nodes.Count; i++)
            {
                string currKey = nodes.Keys[i];
                defaultTranslations.Add(currKey, nodes[currKey].Translation);
                defaultRotations.Add(currKey, nodes[currKey].Rotation);
            }
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
        }

        public void OnUpdate()
        {
            UpdateAnimation(Time.GameTime.ElapsedTime);
            Matrix xform = transform.GetTransform();
            if (useCustomMatrix)
                xform = customMatrix * xform;
            for (int i = 0; i < rootNodes.Length; i++)
                rootNodes[i].ApplyTransform(ref xform);
        }

        public void OnRender(RenderView view, bool performCulling)
        {
            if (rootNodes != null && rootNodes.Length > 0)
                mesh.Render(transform.GetTransform(), nodes, view, performCulling);
            else
                mesh.Render(transform.GetTransform(), view, performCulling);
        }
    }
}
