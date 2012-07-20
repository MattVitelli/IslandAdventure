using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Microsoft.Xna.Framework;
using Gaia.Core;

namespace Gaia.Resources
{
    public struct ModelBoneAnimationFrame
    {
        public float time;
        public Vector3 Rotation;
        public Vector3 Position;
        public string boneName;
    }

    public class AnimationSequence : IResource
    {
        const float blendOutTime = 0.2f;

        float timeStart;
        float timeEnd = -1;
        float fps;
        string name;
        Mesh mesh;
        float endTime;
        public float EndTime
        {
            get { return endTime; }
        }

        public bool IsCyclic = false;

        bool recenterRoots = false;

        SortedList<string, ModelBoneAnimationFrame[]> animationFrames;// = new SortedList<string,ModelBoneAnimationFrame[]>();

        public string Name { get { return name; } }

        void IResource.Destroy()
        {

        }

        void IResource.LoadFromXML(XmlNode node)
        {
            string filename = string.Empty;
            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.Name.ToLower())
                {
                    case "timestart":
                        timeStart = float.Parse(attrib.Value);
                        break;
                    case "timeend":
                        timeEnd = float.Parse(attrib.Value);
                        break;
                    case "fps":
                        fps = float.Parse(attrib.Value);
                        break;
                    case "mesh":
                        mesh = ResourceManager.Inst.GetMesh(attrib.Value);
                        break;
                    case "filename":
                        filename = attrib.Value;
                        break;
                    case "name":
                        name = attrib.Value;
                        break;
                    case "iscyclic":
                        IsCyclic = bool.Parse(attrib.Value);
                        break;
                    case "recenter":
                        recenterRoots = bool.Parse(attrib.Value);
                        break;
                }
            }
            if (filename != string.Empty)
            {
                timeStart = 0;
                ReadSMD(filename);
                RescaleAnimation(animationFrames, timeStart, fps);
            }
            endTime = (timeEnd - timeStart) / fps;
            if (!IsCyclic)
                endTime += blendOutTime;
        }

        void ReadNodes(Tokenizer input, SortedList<int, string> nodesToNames)
        {
            string text = input.GetNextToken();
            while (text != "nodes")
                text = input.GetNextToken();

            while (input.Peek() != "end")
            {
                int id = int.Parse(input.GetNextToken());
                string name = input.GetNextToken();
                int temp = 0;
                while (!int.TryParse(input.Peek(), out temp))
                    name += " " + input.GetNextToken();
                name = name.Substring(1, name.Length - 2);
                nodesToNames.Add(id, name);
                input.GetNextToken(); // the parent index (not important)
            }
            input.GetNextToken(); //read over the end
        }

        void ReadSkeleton(Tokenizer input, SortedList<int, List<ModelBoneAnimationFrame>> frames)
        {
            string text = input.GetNextToken();
            while (text != "skeleton")
                text = input.GetNextToken();
            int currTime = 0;
            timeStart = currTime;
            while (input.Peek() != "end")
            {
                text = input.GetNextToken();
                if (text == "time")
                {
                    currTime = int.Parse(input.GetNextToken());
                    if (currTime > timeEnd && timeEnd > -1)
                    {
                        currTime = (int)timeEnd;
                        break;
                    }
                }
                else
                {
                    int boneID = int.Parse(text);
                    Vector3 posDisp;
                    posDisp.X = float.Parse(input.GetNextToken());
                    posDisp.Y = float.Parse(input.GetNextToken());
                    posDisp.Z = float.Parse(input.GetNextToken());
                    Vector3 rotDisp;
                    rotDisp.X = float.Parse(input.GetNextToken());
                    rotDisp.Y = float.Parse(input.GetNextToken());
                    rotDisp.Z = float.Parse(input.GetNextToken());

                    ModelBoneAnimationFrame frame = new ModelBoneAnimationFrame();
                    frame.Position = posDisp;
                    frame.Rotation = rotDisp;
                    frame.time = currTime;

                    if (!frames.ContainsKey(boneID))
                        frames.Add(boneID, new List<ModelBoneAnimationFrame>());

                    frames[boneID].Add(frame);
                }
            }
            timeEnd = currTime;
            fps = 24;
        }

        void ReadSMD(string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Open))
            {
                using (StreamReader reader = new StreamReader(fs))
                {
                    Tokenizer tokenizer = new Tokenizer(reader);
                    SortedList<int, string> nodesToNames = new SortedList<int,string>();
                    ReadNodes(tokenizer, nodesToNames);

                    SortedList<int, List<ModelBoneAnimationFrame>> frames = new SortedList<int,List<ModelBoneAnimationFrame>>();
                   
                    ReadSkeleton(tokenizer, frames);

                    SortedList<string, AnimationNode> nodes;
                    SortedList<string, char> rootNodes = new SortedList<string, char>();
                    AnimationNode[] roots = mesh.GetRootNodes(out nodes);
                    for (int i = 0; i < roots.Length; i++)
                    {
                        rootNodes.Add(roots[i].Name, '1');
                    }

                    animationFrames = new SortedList<string, ModelBoneAnimationFrame[]>();
                    for (int i = 0; i < frames.Keys.Count; i++)
                    {
                        int currKey = frames.Keys[i];
                        string currName = nodesToNames[currKey];
                        animationFrames.Add(currName, frames[currKey].ToArray());
                        for (int j = 0; j < animationFrames[currName].Length; j++)
                        {
                            animationFrames[currName][j].boneName = currName;
                            if (rootNodes.ContainsKey(currName))
                            {
                                if (recenterRoots)
                                    animationFrames[currName][j].Position = nodes[currName].Translation;
                            }
                            animationFrames[currName][j].Position -= nodes[currName].Translation;
                            animationFrames[currName][j].Rotation -= nodes[currName].Rotation;
                            Vector3 rot = animationFrames[currName][j].Rotation;
                            animationFrames[currName][j].Rotation = new Vector3(MathHelper.WrapAngle(rot.X), MathHelper.WrapAngle(rot.Y), MathHelper.WrapAngle(rot.Z));
                            
                        }
                    }
                }
            }
        }

        void RescaleAnimation(SortedList<string, ModelBoneAnimationFrame[]> frames, float startTime, float fps)
        {
            for (int i = 0; i < frames.Count; i++)
            {
                string currKey = frames.Keys[i];
                for (int j = 0; j < frames[currKey].Length; j++)
                {
                    frames[currKey][j].time = (frames[currKey][j].time - startTime) / fps;
                }
            }
        }

        public string[] GetAnimationKeys()
        {
            string[] keys = new string[animationFrames.Keys.Count];
            animationFrames.Keys.CopyTo(keys, 0);
            return keys;
        }

        public void GetKeyFrameParameter(string name, out Vector3 pos, out Vector3 rot, float time)
        {
            int frameIndex = 0;
            int numFrames = animationFrames[name].Length;
            while (frameIndex < numFrames && animationFrames[name][frameIndex].time < time)
                frameIndex++;

            if (frameIndex == 0)
            {
                pos = animationFrames[name][0].Position;
                rot = animationFrames[name][0].Rotation;
            }
            else if (frameIndex == animationFrames[name].Length)
            {
                int idx = numFrames - 1;
                pos = animationFrames[name][idx].Position;
                rot = animationFrames[name][idx].Rotation;
            }
            else
            {
                int prevFrameIndex = frameIndex - 1;

                ModelBoneAnimationFrame right = animationFrames[name][frameIndex];
                ModelBoneAnimationFrame left = animationFrames[name][prevFrameIndex];
                float timeDelta = right.time - left.time;
                float interpolator = MathHelper.Clamp((float)Math.Sqrt((time - left.time) / timeDelta), 0, 1);

                pos = Vector3.Lerp(left.Position, right.Position, interpolator);
                rot = Vector3.Lerp(left.Rotation, right.Rotation, interpolator);
            }
        }
    }
}
