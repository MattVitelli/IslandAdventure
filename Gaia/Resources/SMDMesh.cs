using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Core;
using Gaia.Rendering;
namespace Gaia.Resources
{
    public class SMDMesh : IResource
    {
        string name;
        public string Name { get { return name; } }

        SortedList<string, int> nodeParents;
        SortedList<int, string> nodeNames;

        SortedList<string, SortedList<int, BoneFrame[]>> animations = new SortedList<string, SortedList<int, BoneFrame[]>>();

        ModelPart[] modelGroups;

        struct ModelPart
        {
            public Material material;
            public VertexBuffer vertexBuffer;
            public IndexBuffer indexBuffer;
            public BoundingBox bounds;

            public ModelPart(string materialName, VertexBuffer vb, IndexBuffer ib, BoundingBox bb)
            {
                this.material = ResourceManager.Inst.GetMaterial(materialName);
                this.vertexBuffer = vb;
                this.indexBuffer = ib;
                this.bounds = bb;
            }
        };

        struct BoneFrame
        {
            public Vector3 position;
            public Vector3 rotation;
            public int node;

            public BoneFrame(int node, Vector3 position, Vector3 rotation)
            {
                this.node = node;
                this.position = position;
                this.rotation = rotation;
            }
        };


        void ParseSMDNodes(StreamReader file)
        {
            if (nodeNames != null)
                return;

            nodeNames = new SortedList<int, string>();
            nodeParents = new SortedList<string, int>();

            string text = file.ReadLine();
            while (text != "end")
            {
                string[] data = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                int nodeIndex = int.Parse(data[0]);
                string nodeName = data[1].Substring(1, data[1].Length - 1);
                int parentIndex = int.Parse(data[2]);
                nodeParents.Add(nodeName, parentIndex);
                nodeNames.Add(nodeIndex, nodeName);
                text = file.ReadLine();
            }
        }

        void ParseSMDSkeleton(StreamReader file, string filename)
        {
            SortedList<int, BoneFrame[]> currAnim = new SortedList<int, BoneFrame[]>();
            List<BoneFrame> currFrame = null;
            int currFrameTime = -1;
            while (!file.EndOfStream)
            {
                string text = file.ReadLine();
                string[] data = text.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
                switch (data[0])
                {
                    case "end":
                        if (currAnim.Count > 0)
                        {
                            animations.Add(filename, currAnim);
                        }
                        return;
                    case "time":
                        int time = int.Parse(data[1]);
                        if (currFrame == null)
                        {
                            currFrame = new List<BoneFrame>();
                        }
                        else
                        {
                            currAnim.Add(currFrameTime, currFrame.ToArray());
                            currFrame = new List<BoneFrame>();
                        }
                        currFrameTime = time;
                        break;
                    default:
                        int nodeIndex = int.Parse(data[0]);
                        Vector3 pos = ParseUtils.ParseVector3(data[1] + " " + data[2] + " " + data[3]);
                        Vector3 rot = ParseUtils.ParseVector3(data[4] + " " + data[5] + " " + data[6]);
                        BoneFrame frame = new BoneFrame(nodeIndex, pos, rot);
                        currFrame.Add(frame);
                        break;
                }
            }
        }

        void ParseSMDTriangles(StreamReader file)
        {
            if (this.modelGroups != null)
                return;

            SortedList<string, List<VertexPNTTB>> vertexLists = new SortedList<string, List<VertexPNTTB>>();
            SortedList<string, List<ushort>> indicesList = new SortedList<string, List<ushort>>();
            while (!file.EndOfStream)
            {
                string text = file.ReadLine();
                if (text == "end")
                {
                    break;
                }

                string materialName = text.Split('.')[0];
                if (!vertexLists.ContainsKey(materialName))
                {
                    vertexLists.Add(materialName, new List<VertexPNTTB>());
                    indicesList.Add(materialName, new List<ushort>());
                }

                VertexPNTTB[] vertices = new VertexPNTTB[3];
                for (int i = 0; i < 3; i++)
                {
                    text = file.ReadLine();
                    string[] data = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    Vector4 bone = new Vector4(int.Parse(data[0]), 0, 0, 0);
                    Vector4 boneWeights = new Vector4(1.0f, 0, 0, 0);
                    Vector3 pos;
                    pos.X = float.Parse(data[1]);
                    pos.Y = float.Parse(data[2]);
                    pos.Z = float.Parse(data[3]);
                    Vector3 normal;
                    normal.X = float.Parse(data[4]);
                    normal.Y = float.Parse(data[5]);
                    normal.Z = float.Parse(data[6]);
                    normal.Normalize();
                    Vector2 texcoord;
                    texcoord.X = float.Parse(data[7]);
                    texcoord.Y = float.Parse(data[8]);
                    if (data.Length > 10) //Code for multiple blendweights
                    {
                        int boneBlendCount = int.Parse(data[9]);
                        for (int m = 0; m < boneBlendCount; m++)
                        {
                            int index = m * 2 + 10;
                            switch (m)
                            {
                                case 0:
                                    bone.Y = int.Parse(data[index]);
                                    boneWeights.Y = float.Parse(data[index + 1]);
                                    break;
                                case 1:
                                    bone.Z = int.Parse(data[index]);
                                    boneWeights.Z = float.Parse(data[index + 1]);
                                    break;
                                case 2:
                                    bone.W = int.Parse(data[index]);
                                    boneWeights.W = float.Parse(data[index + 1]);
                                    break;
                            }
                        }
                        float sum = Vector4.Dot(boneWeights, Vector4.One);
                        boneWeights.X = 1.0f - sum;
                    }
                    vertices[i] = new VertexPNTTB(pos, normal, texcoord, bone, boneWeights, Vector3.Zero);
                    indicesList[materialName].Add((ushort)(vertexLists[materialName].Count+i));
                }

                //Compute our tangent vectors
                MathUtils.ComputeTangent(ref vertices[0], vertices[1], vertices[2]);
                MathUtils.ComputeTangent(ref vertices[1], vertices[2], vertices[0]);
                MathUtils.ComputeTangent(ref vertices[2], vertices[0], vertices[1]);
                vertexLists[materialName].AddRange(vertices);
            }

            this.modelGroups = new ModelPart[vertexLists.Keys.Count];
            for (int i = 0; i < vertexLists.Keys.Count; i++)
            {
                string key = vertexLists.Keys[i];

                VertexBuffer vertexBuffer = new VertexBuffer(GFX.Device, vertexLists[key].Count * VertexPNTTB.SizeInBytes, BufferUsage.WriteOnly);
                vertexBuffer.SetData<VertexPNTTB>(vertexLists[key].ToArray());

                IndexBuffer indexBuffer = new IndexBuffer(GFX.Device, sizeof(ushort) * indicesList[key].Count, BufferUsage.WriteOnly, IndexElementSize.SixteenBits);
                indexBuffer.SetData<ushort>(indicesList[key].ToArray());

                Vector3 pos = new Vector3(vertexLists[key][0].Position.X, vertexLists[key][0].Position.Y, vertexLists[key][0].Position.Z);
                BoundingBox bounds = new BoundingBox(pos, pos);
                for (int j = 0; j < vertexLists[key].Count; j++)
                {
                    pos = new Vector3(vertexLists[key][j].Position.X, vertexLists[key][j].Position.Y, vertexLists[key][j].Position.Z);
                    bounds.Max = Vector3.Max(bounds.Max, pos);
                    bounds.Min = Vector3.Min(bounds.Min, pos);
                }

                modelGroups[i] = new ModelPart(key, vertexBuffer, indexBuffer, bounds);
            }


        }

        void LoadSMD(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (StreamReader file = new StreamReader(fs))
                {
                    while (!file.EndOfStream)
                    {
                        switch (file.ReadLine())
                        {
                            case "version 1":

                                break;
                            case "nodes":
                                ParseSMDNodes(file);
                                break;
                            case "skeleton":
                                ParseSMDSkeleton(file, filename);
                                break;
                            case "triangles":
                                ParseSMDTriangles(file);
                                break;
                        }
                    }
                }
            }
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            //try
            {
                foreach (XmlAttribute attrib in node.Attributes)
                {
                    string[] attribs = attrib.Name.ToLower().Split('_');
                    switch (attribs[0])
                    {
                        case "mesh":
                            LoadSMD(attrib.Value);
                            break;
                        case "anim":
                            LoadSMD(attrib.Value);
                            break;
                        case "name":
                            name = attrib.Value;
                            break;
                    }
                }

            }
            //catch { }
        }

        void IResource.Destroy()
        {

        }
    }
}
