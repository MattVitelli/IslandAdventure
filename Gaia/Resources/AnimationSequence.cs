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
        public Vector3 Displacement;
        public string boneName;
    }

    public enum AnimationType
    {
        Rotation,
        Position,
    }

    public class AnimationSequence : IResource
    {
        float timeStart;
        float timeEnd;
        float fps;
        string name;
        Mesh mesh;
        float endTime;
        public float EndTime
        {
            get { return endTime; }
        }

        public bool IsCyclic = false;

        SortedList<string, ModelBoneAnimationFrame[]> positionFrames;// = new SortedList<string,ModelBoneAnimationFrame[]>();
        SortedList<string, ModelBoneAnimationFrame[]> rotationFrames;// = new SortedList<string,ModelBoneAnimationFrame[]>();

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
                }
            }
            if (filename != string.Empty)
            {
                timeStart = 0;
                ReadSMD(filename);
            }
            else
            {
                mesh.ExtractNodesBetweenFrames(timeStart, timeEnd, out positionFrames, out rotationFrames);
            }
            RescaleAnimation(positionFrames, timeStart, fps);
            RescaleAnimation(rotationFrames, timeStart, fps);
            endTime = (timeEnd - timeStart) / fps;
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
                name = name.Substring(1, name.Length - 2);
                nodesToNames.Add(id, name);
                input.GetNextToken(); // the parent index (not important)
            }
            input.GetNextToken(); //read over the end
        }

        void ReadSkeleton(Tokenizer input, SortedList<int, List<ModelBoneAnimationFrame>> posFrames, SortedList<int, List<ModelBoneAnimationFrame>> rotFrames)
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

                    ModelBoneAnimationFrame posFrame = new ModelBoneAnimationFrame();
                    posFrame.Displacement = posDisp;
                    posFrame.time = currTime;

                    ModelBoneAnimationFrame rotFrame = new ModelBoneAnimationFrame();
                    rotFrame.Displacement = rotDisp;
                    rotFrame.time = currTime;

                    if (!posFrames.ContainsKey(boneID))
                        posFrames.Add(boneID, new List<ModelBoneAnimationFrame>());
                    if (!rotFrames.ContainsKey(boneID))
                        rotFrames.Add(boneID, new List<ModelBoneAnimationFrame>());

                    posFrames[boneID].Add(posFrame);
                    rotFrames[boneID].Add(rotFrame);
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

                    SortedList<int, List<ModelBoneAnimationFrame>> posFrames = new SortedList<int,List<ModelBoneAnimationFrame>>();
                    SortedList<int, List<ModelBoneAnimationFrame>> rotFrames = new SortedList<int,List<ModelBoneAnimationFrame>>();
                    ReadSkeleton(tokenizer, posFrames, rotFrames);

                    SortedList<string, AnimationNode> nodes;
                    mesh.GetRootNodes(out nodes);

                    positionFrames = new SortedList<string, ModelBoneAnimationFrame[]>();
                    for (int i = 0; i < posFrames.Keys.Count; i++)
                    {
                        int currKey = posFrames.Keys[i];
                        string currName = nodesToNames[currKey];
                        positionFrames.Add(currName, posFrames[currKey].ToArray());
                        for (int j = 0; j < positionFrames[currName].Length; j++)
                        {
                            positionFrames[currName][j].boneName = currName;
                            positionFrames[currName][j].Displacement -= nodes[currName].Translation;
                        }
                    }

                    rotationFrames = new SortedList<string, ModelBoneAnimationFrame[]>();
                    for (int i = 0; i < rotFrames.Keys.Count; i++)
                    {
                        int currKey = rotFrames.Keys[i];
                        string currName = nodesToNames[currKey];
                        rotationFrames.Add(currName, rotFrames[currKey].ToArray());
                        for (int j = 0; j < rotationFrames[currName].Length; j++)
                        {
                            rotationFrames[currName][j].boneName = currName;
                            rotationFrames[currName][j].Displacement -= nodes[currName].Rotation;
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

        public string[] GetAnimationKeys(AnimationType type)
        {
            string[] keys;
            if(type == AnimationType.Position)
            {
                keys = new string[positionFrames.Keys.Count];
                positionFrames.Keys.CopyTo(keys, 0);
            }
            else
            {
                keys = new string[rotationFrames.Keys.Count];
                rotationFrames.Keys.CopyTo(keys, 0);
            }
            return keys;
        }

        public int ComputeFrameIndex(string name, AnimationType type, float time)
        {
            SortedList<string, ModelBoneAnimationFrame[]> frameList = (type == AnimationType.Position) ? positionFrames : rotationFrames;
            int frameIndex = 0;
            int numFrames = frameList[name].Length;
            while (frameIndex < numFrames && frameList[name][frameIndex].time < time)
                frameIndex++;
            return frameIndex;
        }

        public Vector3 GetKeyFrameParameter(string name, AnimationType type, float time, ref int frameIndex)
        {
            SortedList<string, ModelBoneAnimationFrame[]> frameList = (type == AnimationType.Position) ? positionFrames : rotationFrames;
            int numFrames = frameList[name].Length;
            Vector3 parameter;
            if (!IsCyclic)
            {
                if (frameIndex == 0)
                {
                    parameter = frameList[name][0].Displacement;
                    if (time >= frameList[name][frameIndex].time)
                        frameIndex++;
                }
                else if (frameIndex == numFrames)
                    parameter = frameList[name][numFrames - 1].Displacement;
                else
                {
                    int prevFrameIndex = frameIndex - 1;

                    ModelBoneAnimationFrame right = frameList[name][frameIndex];
                    ModelBoneAnimationFrame left = frameList[name][prevFrameIndex];
                    float timeDelta = Math.Max(0.01f, right.time - left.time);
                    float interpolator = (time - left.time) / timeDelta;
                    if (interpolator >= 1.0f)
                        frameIndex++;
                    parameter = Vector3.Lerp(left.Displacement, right.Displacement, Math.Min(1.0f,interpolator));
                    
                }
            }
            else
            {
                int prevFrameIndex = frameIndex - 1;
                if (prevFrameIndex < 0)
                {
                    prevFrameIndex = frameList[name].Length - 1;
                }
                ModelBoneAnimationFrame right = frameList[name][frameIndex];
                ModelBoneAnimationFrame left = frameList[name][prevFrameIndex];
                float leftTime = (prevFrameIndex == (frameList[name].Length - 1)) ? 0.0f : left.time;

                float timeDelta = Math.Max(0.01f, Math.Abs(right.time - leftTime));
                float interpolator = Math.Abs(time - leftTime) / timeDelta;

                parameter = Vector3.Lerp(left.Displacement, right.Displacement, interpolator);
                if (interpolator >= 1.0f)
                {
                    frameIndex++;
                    if (frameIndex >= frameList[name].Length)
                        frameIndex = 0;

                    parameter = Vector3.Lerp(left.Displacement, right.Displacement, Math.Min(1.0f,interpolator));
                }
            }

            return parameter;
        }
    }
}
