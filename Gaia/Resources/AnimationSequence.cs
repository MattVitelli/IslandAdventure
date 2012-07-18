using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

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

        SortedList<string, ModelBoneAnimationFrame[]> positionFrames;// = new SortedList<string,ModelBoneAnimationFrame[]>();
        SortedList<string, ModelBoneAnimationFrame[]> rotationFrames;// = new SortedList<string,ModelBoneAnimationFrame[]>();

        public string Name { get { return name; } }

        void IResource.Destroy()
        {

        }

        void IResource.LoadFromXML(XmlNode node)
        {
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
                    case "name":
                        name = attrib.Value;
                        break;
                }
            }
            mesh.ExtractNodesBetweenFrames(timeStart, timeEnd, out positionFrames, out rotationFrames);
            RescaleAnimation(positionFrames, timeStart, fps);
            RescaleAnimation(rotationFrames, timeStart, fps);
            endTime = (timeEnd - timeStart) / fps;
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
                float timeDelta = right.time - left.time;
                float interpolator = (time - left.time) / timeDelta;

                parameter = Vector3.Lerp(left.Displacement, right.Displacement, interpolator);
                if (interpolator > 1.0f)
                    frameIndex++;
            }

            return parameter;
        }
    }
}
