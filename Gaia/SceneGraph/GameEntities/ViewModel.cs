using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.SceneGraph;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Rendering.RenderViews;

namespace Gaia.SceneGraph.GameEntities
{
    class ViewModel
    {
        protected Mesh mesh;
        protected AnimationNode[] rootNodes;
        protected SortedList<string, AnimationNode> nodes;

        Transform transform;

        public ViewModel(string name)
        {
            InitializeMesh(name);
        }

        protected void InitializeMesh(string name)
        {
            mesh = ResourceManager.Inst.GetMesh(name);
            rootNodes = mesh.GetRootNodes(out nodes);
        }

        public void SetTransform(Transform transform)
        {
            this.transform = transform;
        }

        public void OnUpdate()
        {
            Matrix xform = transform.GetTransform();
            for (int i = 0; i < rootNodes.Length; i++)
                rootNodes[i].ApplyTransform(ref xform);
        }

        public void OnRender(RenderView view)
        {
            if (rootNodes != null && rootNodes.Length > 0)
                mesh.Render(transform.GetTransform(), nodes, view);
            else
                mesh.Render(transform.GetTransform(), view);
        }
    }
}
