using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml;

using Gaia.Rendering;

namespace Gaia.Resources
{
    public class QuestUpdate
    {
        public string JournalEntry;
        public string TargetName;
        public QuestUpdate NextNode;
    }

    public class QuestResource : IResource
    {
        string name;
        public string Name { get { return name; } }

        public string QuestName;

        public QuestUpdate Start;

        QuestUpdate[] Entries;

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
                        QuestName = attrib.Value;
                        name = attrib.Value;
                        break;
                }
            }

            SortedList<string, QuestUpdate> nodes = new SortedList<string,QuestUpdate>();
            SortedList<string, string> nodesNextNames = new SortedList<string,string>();

            for(int i = 0; i < node.ChildNodes.Count; i++)
            {
                XmlNode child = node.ChildNodes[i];
                if (child.Name == "QuestUpdate")
                {
                    QuestUpdate currUpdate = new QuestUpdate();
                    string currName = string.Empty;
                    string nextName = string.Empty;
                    for (int j = 0; j < child.Attributes.Count; j++)
                    {
                        XmlAttribute attrib = child.Attributes[j];
                        switch (attrib.Name.ToLower())
                        {
                            case "summary":
                                currUpdate.JournalEntry = attrib.Value;
                                break;
                            case "target":
                                currUpdate.TargetName = attrib.Value;
                                break;
                            case "name":
                                currName = attrib.Value;
                                break;
                            case "next":
                                nextName = attrib.Value;
                                break;
                            case "startnode":
                                if (bool.Parse(attrib.Value))
                                    Start = currUpdate;
                                break;
                        }
                    }

                    nodes.Add(currName, currUpdate);
                    nodesNextNames.Add(currName, nextName);
                }
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                string currKey = nodes.Keys[i];
                QuestUpdate currNode = nodes[currKey];
                if(nodesNextNames.ContainsKey(currKey))
                {
                    string nextKey = nodesNextNames[currKey];
                    if (nodes.ContainsKey(nextKey))
                        currNode.NextNode = nodes[nextKey];
                    else
                        currNode.NextNode = null;
                }
            }

            nodes.Values.CopyTo(Entries, 0);
        }
    }
}
