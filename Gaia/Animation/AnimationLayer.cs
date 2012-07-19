using System;
using System.Collections.Generic;
using Gaia.Resources;
using Microsoft.Xna.Framework;
using Gaia.SceneGraph.GameEntities;
namespace Gaia.Animation
{
    public class AnimationLayer
    {
        public float Weight = 1.0f;
        AnimationSequence animation;
        SortedList<string, int> frameIndices = new SortedList<string, int>();
        float elapsedTime = 0;
        ViewModel model;
        string name;
        public AnimationLayer(string name, ViewModel model, float weight)
        {
            this.name = name;
            this.Weight = weight;
            this.animation = ResourceManager.Inst.GetAnimation(name);
            this.model = model;
            InitializeFrames();
        }

        void InitializeFrames()
        {
            string[] keys = animation.GetAnimationKeys();
            for (int i = 0; i < keys.Length; i++)
                frameIndices.Add(keys[i], 0);
        }

        void ComputeFrameIndices()
        {
            for (int i = 0; i < frameIndices.Count; i++)
            {
                string currKey = frameIndices.Keys[i];
                frameIndices[currKey] = animation.ComputeFrameIndex(currKey, elapsedTime);
            }
        }

        public void SetTime(float time)
        {
            elapsedTime = time;
            ComputeFrameIndices();
        }

        public void UpdateAnimation(float timeDT, SortedList<string, AnimationNode> nodes)
        {
            elapsedTime += timeDT;
            for(int i = 0; i < frameIndices.Count; i++)
            {
                string currKey = frameIndices.Keys[i];
                int frameIndex = frameIndices[currKey];
                Vector3 posDelta = Vector3.Zero;
                Vector3 rotDelta = Vector3.Zero;
                animation.GetKeyFrameParameter(currKey, out posDelta, out rotDelta, elapsedTime, ref frameIndex);
                frameIndices[currKey] = frameIndex;
                nodes[currKey].Translation += posDelta * Weight;
                nodes[currKey].Rotation += rotDelta * Weight;
            }

            if (elapsedTime > animation.EndTime)
            {
                if (animation.IsCyclic)
                {
                    elapsedTime = 0;
                    ComputeFrameIndices();
                }
                else
                {
                    model.RemoveAnimationLayer(this.name);
                }
            }
        }
    }
}
