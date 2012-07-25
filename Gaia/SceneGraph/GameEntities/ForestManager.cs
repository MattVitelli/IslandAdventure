using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Gaia.Resources;
using Gaia.Core;
using Gaia.Rendering.RenderViews;

namespace Gaia.SceneGraph.GameEntities
{
    public class ForestElement
    {
        public Mesh Mesh;
        public Transform Transform;
    };

    public class ForestManager : Entity
    {
        public KDTree<ForestElement> visibleMeshes = new KDTree<ForestElement>(SceneCompareFunction);
        Mesh mesh;
        const int entityCount = 2000;

        public override void OnAdd(Scene scene)
        {
            mesh = ResourceManager.Inst.GetMesh("Cecropia");
            base.OnAdd(scene);
            for(int i = 0; i < entityCount; i++)
            {
                Vector3 pos;
                Vector3 normal;
                scene.MainTerrain.GenerateRandomTransform(RandomHelper.RandomGen, out pos, out normal);
                ForestElement element = new ForestElement();
                element.Transform = new Transform();
                element.Transform.SetPosition(pos);
                element.Mesh = mesh;
                visibleMeshes.AddElement(element, false);
            }
            visibleMeshes.BuildTree();
            RecursivelyBuildBounds(visibleMeshes.GetRoot());
        }

        static int SceneCompareFunction(ForestElement elementA, ForestElement elementB, int axis)
        {
            Vector3 posA = elementA.Transform.GetPosition();
            float valueA = (axis == 0) ? posA.X : (axis == 1) ? posA.Y : posA.Z;

            Vector3 posB = elementB.Transform.GetPosition();
            float valueB = (axis == 0) ? posB.X : (axis == 1) ? posB.Y : posB.Z;

            if (valueA < valueB)
                return -1;
            if (valueA > valueB)
                return 1;

            return 0;
        }

        void RecursivelyBuildBounds(KDNode<ForestElement> node)
        {
            if (node == null)
                return;

            RecursivelyBuildBounds(node.leftChild);
            RecursivelyBuildBounds(node.rightChild);
            
            node.bounds = node.element.Transform.TransformBounds(node.element.Mesh.GetBounds());

            if (node.leftChild != null)
            {
                node.bounds.Min = Vector3.Min(node.leftChild.bounds.Min, node.bounds.Min);
                node.bounds.Max = Vector3.Max(node.leftChild.bounds.Max, node.bounds.Max);
            }

            if (node.rightChild != null)
            {
                node.bounds.Min = Vector3.Min(node.rightChild.bounds.Min, node.bounds.Min);
                node.bounds.Max = Vector3.Max(node.rightChild.bounds.Max, node.bounds.Max);
            }
        }

        public override void OnRender(RenderView view)
        {
            RecursivelyRender(visibleMeshes.GetRoot(), view);
            base.OnRender(view);
        }

        void RecursivelyRender(KDNode<ForestElement> node, RenderView view)
        {
            if (node == null || view.GetFrustum().Contains(node.bounds) == ContainmentType.Disjoint)
                return;

            node.element.Mesh.Render(node.element.Transform.GetTransform(), view, false);
            RecursivelyRender(node.leftChild, view);
            RecursivelyRender(node.rightChild, view);
        }
    }
}
