using System;
using System.Collections.Generic;
using System.Xml;

namespace Gaia.Resources
{
    public class ItemDatablock : IResource
    {
        string name;
        public string Name { get { return name; } }

        Mesh mesh = null;

        float mass = 0.0f;

        public float GetMass()
        {
            return mass;
        }

        public Mesh GetMesh()
        {
            return mesh;
        }

        string description;

        public string GetDescription()
        {
            return description;
        }

        bool canDrop = true;

        public bool CanDrop()
        {
            return canDrop;
        }

        void IResource.Destroy()
        {
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.Name.ToLower())
                {
                    case "name":
                        name = attrib.Value;
                        break;
                    case "mesh":
                        mesh = ResourceManager.Inst.GetMesh(attrib.Value);
                        break;
                    case "summary":
                        description = attrib.Value;
                        break;
                    case "mass":
                        mass = float.Parse(attrib.Value);
                        break;
                    case "candrop":
                        canDrop = bool.Parse(attrib.Value);
                        break;
                }
            }
        }
    }

    public class ItemAttribute
    {

    }
}
