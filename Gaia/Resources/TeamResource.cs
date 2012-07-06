using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml;

using Gaia.Rendering;

namespace Gaia.Resources
{
    public class TeamResource : IResource
    {
        string name;
        public string Name { get { return name; } }

        public string TeamName;

        public List<string> TeamMembers = new List<string>();

        void IResource.Destroy()
        {
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            //try
            {
                foreach (XmlAttribute attrib in node.Attributes)
                {
                    switch (attrib.Name.ToLower())
                    {
                        case "name":
                            TeamName = attrib.Value;
                            name = TeamName;
                            break;
                    }
                }

                for(int i = 0; i < node.ChildNodes.Count; i++)
                {
                    XmlNode child = node.ChildNodes[i];
                    //if(child.Name.ToLower() == "teammate")
                        TeamMembers.Add(child.InnerText);
                }
            }
            //catch { }
        }
    }
}
