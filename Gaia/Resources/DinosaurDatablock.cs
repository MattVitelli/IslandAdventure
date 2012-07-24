using System;
using System.Collections.Generic;
using System.Xml;

namespace Gaia.Resources
{
    public class DinosaurDatablock : IResource
    {
        string name;

        public string Name { get { return name; } }

        public float Health = 10;

        public float Damage = 1.0f;

        public float Aggressiveness = 1.0f;

        public float Fear = 0.0f;

        public float Hearing = 1.0f;

        public float Sight = 1.0f;

        public TextureResource HuntImage;

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
                    case "health":
                        Health = float.Parse(attrib.Value);
                        break;
                    case "damage":
                        Damage = float.Parse(attrib.Value);
                        break;
                    case "aggressive":
                        Aggressiveness = float.Parse(attrib.Value);
                        break;
                    case "fear":
                        Fear = float.Parse(attrib.Value);
                        break;
                    case "hearing":
                        Hearing = float.Parse(attrib.Value);
                        break;
                    case "sight":
                        Sight = float.Parse(attrib.Value);
                        break;
                    case "huntimage":
                        HuntImage = ResourceManager.Inst.GetTexture(attrib.Value);
                        break;
                }
            }
        }
    }
}
