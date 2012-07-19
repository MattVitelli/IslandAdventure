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

    public enum AnimationType
    {
        Rotation,
        Position,
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
                            animationFrames[currName][j].Position -= nodes[currName].Translation;
                            animationFrames[currName][j].Rotation -= nodes[currName].Rotation;
                            if (rootNodes.ContainsKey(currName) && recenterRoots)
                                animationFrames[currName][j].Position = Vector3.Zero;
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

        public int ComputeFrameIndex(string name, float time)
        {
            int frameIndex = 0;
            int numFrames = animationFrames[name].Length;
            while (frameIndex < numFrames && animationFrames[name][frameIndex].time < time)
                frameIndex++;
            return frameIndex;
        }

        public void GetKeyFrameParameter(string name, out Vector3 pos, out Vector3 rot, float time, ref int frameIndex)
        {
            int numFrames = animationFrames[name].Length;
            if (!IsCyclic)
            {
                if (frameIndex == 0)
                {
                    pos = animationFrames[name][0].Position;
                    rot = animationFrames[name][0].Rotation;
                    if (time >= animationFrames[name][frameIndex].time)
                        frameIndex++;
                }
                else if (frameIndex == numFrames)
                {
                    float interp = Math.Min(1.0f, ((time - animationFrames[name][numFrames - 1].time) / blendOutTime));
                    pos = Vector3.Lerp(animationFrames[name][numFrames - 1].Position, animationFrames[name][0].Position, interp);
                    rot = Vector3.Lerp(animationFrames[name][numFrames - 1].Rotation, animationFrames[name][0].Rotation, interp);
                }
                else
                {
                    int prevFrameIndex = frameIndex - 1;

                    ModelBoneAnimationFrame right = animationFrames[name][frameIndex];
                    ModelBoneAnimationFrame left = animationFrames[name][prevFrameIndex];
                    float timeDelta = Math.Max(0.01f, right.time - left.time);
                    float interpolator = (time - left.time) / timeDelta;
                    if (interpolator >= 1.0f)
                        frameIndex++;
                    pos = Vector3.Lerp(left.Position, right.Position, Math.Min(1.0f, interpolator));
                    rot = Vector3.Lerp(left.Rotation, right.Rotation, Math.Min(1.0f, interpolator));

                }
            }
            else
            {
                int prevFrameIndex = frameIndex - 1;
                if (prevFrameIndex < 0)
                {
                    prevFrameIndex = animationFrames[name].Length - 1;
                }
                ModelBoneAnimationFrame right = animationFrames[name][frameIndex];
                ModelBoneAnimationFrame left = animationFrames[name][prevFrameIndex];
                float leftTime = (prevFrameIndex == (animationFrames[name].Length - 1)) ? 0.0f : left.time;

                float timeDelta = Math.Max(0.01f, Math.Abs(right.time - leftTime));
                float interpolator = Math.Abs(time - leftTime) / timeDelta;

                pos = Vector3.Lerp(left.Position, right.Position, interpolator);
                rot = Vector3.Lerp(left.Rotation, right.Rotation, interpolator);
                if (interpolator >= 1.0f)
                {
                    frameIndex++;
                    if (frameIndex >= animationFrames[name].Length)
                        frameIndex = 0;

                    pos = Vector3.Lerp(left.Position, right.Position, Math.Min(1.0f,interpolator));
                    rot = Vector3.Lerp(left.Rotation, right.Rotation, Math.Min(1.0f,interpolator));
                }
            }
        }
    }
}
