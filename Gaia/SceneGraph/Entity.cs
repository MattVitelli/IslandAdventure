using System;
using System.Collections.Generic;
using System.Xml;

using Microsoft.Xna.Framework;

using Gaia.Core;
using Gaia.Rendering.RenderViews;
using Gaia.Resources;

namespace Gaia.SceneGraph
{
    public abstract class Entity
    {
        protected Scene scene;

        public Transform Transformation = new Transform();

        public virtual void OnSave(XmlWriter writer)
        {
            Vector3 pos = Transformation.GetPosition();
            Vector3 rot = Transformation.GetRotation();
            Vector3 scale = Transformation.GetScale();
            writer.WriteStartAttribute("position");
            writer.WriteValue(ParseUtils.WriteVector3(pos));
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("rotation");
            writer.WriteValue(ParseUtils.WriteVector3(rot));
            writer.WriteEndAttribute();

            writer.WriteStartAttribute("scale");
            writer.WriteValue(ParseUtils.WriteVector3(scale));
            writer.WriteEndAttribute();
        }

        public virtual void OnLoad(XmlNode node)
        {
            Vector3 pos = ParseUtils.ParseVector3(node.Attributes["position"].Value);
            Vector3 rot = ParseUtils.ParseVector3(node.Attributes["rotation"].Value);
            Vector3 scale = ParseUtils.ParseVector3(node.Attributes["scale"].Value);

            Transformation.SetPosition(pos);
            Transformation.SetRotation(rot);
            Transformation.SetScale(scale);
        }

        public virtual void OnAdd(Scene scene) { this.scene = scene; }
        public virtual void OnDestroy() { }

        public virtual void OnUpdate() { }
        public virtual void OnRender(RenderView view) { }
    }
}
