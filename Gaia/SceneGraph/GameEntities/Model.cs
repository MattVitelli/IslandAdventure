using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.Resources;

using JigLibX.Physics;
using JigLibX.Geometry;
using JigLibX.Collision;

namespace Gaia.SceneGraph.GameEntities
{
    public class Model : Entity
    {
        protected Mesh mesh;
        protected AnimationNode[] rootNodes;
        protected SortedList<string, AnimationNode> nodes;

        protected CollisionSkin collision;
        protected JigLibX.Math.Transform collisionTransform;

        public Model()
        {

        }

        public Model(string name)
        {
            InitializeMesh(name);
        }

        protected void InitializeMesh(string name)
        {
            mesh = ResourceManager.Inst.GetMesh(name);
            rootNodes = mesh.GetRootNodes(out nodes);
        }

        public override void OnSave(System.Xml.XmlWriter writer)
        {
            base.OnSave(writer);
            writer.WriteStartAttribute("meshname");
            writer.WriteValue(mesh.Name);
            writer.WriteEndAttribute();
        }

        public override void OnLoad(System.Xml.XmlNode node)
        {
            base.OnLoad(node);
            InitializeMesh(node.Attributes["meshname"].Value);
        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            if (mesh.GetCollisionMesh() != null)
            {
                Matrix currOrientation = Transformation.GetTransform();
                currOrientation.Translation = Vector3.Zero;
                Vector3 currPosition = Transformation.GetPosition();
                collisionTransform = new JigLibX.Math.Transform(currPosition, currOrientation);
                collision = new CollisionSkin(null);
                collision.AddPrimitive(mesh.GetCollisionMesh(), (int)MaterialTable.MaterialID.NotBouncyRough);
                scene.GetPhysicsEngine().CollisionSystem.AddCollisionSkin(collision);
                collision.SetNewTransform(ref collisionTransform);
            }
        }
        /*
        public void UpdateAnimation()
        {
            Matrix transform = this.Transformation.GetTransform();
            for (int i = 0; i < rootNodes.Length; i++)
            {
                rootNodes[i].ApplyTransform(ref transform);
            }
        }
        */
        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            Matrix transform = this.Transformation.GetTransform();
            if (rootNodes != null && rootNodes.Length > 0)
                mesh.Render(transform, nodes, view);
            else
                mesh.Render(transform, view);

            base.OnRender(view);
        }
    }
}
