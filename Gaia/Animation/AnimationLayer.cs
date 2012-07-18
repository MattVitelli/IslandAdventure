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
        SortedList<string, int> rotationFrameIndices = new SortedList<string, int>();
        SortedList<string, int> positionFrameIndices = new SortedList<string, int>();
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
            string[] positionKeys = animation.GetAnimationKeys(AnimationType.Position);
            string[] rotationKeys = animation.GetAnimationKeys(AnimationType.Rotation);
            for (int i = 0; i < positionKeys.Length; i++)
                positionFrameIndices.Add(positionKeys[i], 0);
            for (int i = 0; i < rotationKeys.Length; i++)
                rotationFrameIndices.Add(rotationKeys[i], 0);
        }

        void ComputeFrameIndices()
        {
            for (int i = 0; i < positionFrameIndices.Count; i++)
            {
                string currKey = positionFrameIndices.Keys[i];
                positionFrameIndices[currKey] = animation.ComputeFrameIndex(currKey, AnimationType.Position, elapsedTime);
            }

            for (int i = 0; i < rotationFrameIndices.Count; i++)
            {
                string currKey = rotationFrameIndices.Keys[i];
                rotationFrameIndices[currKey] = animation.ComputeFrameIndex(currKey, AnimationType.Rotation, elapsedTime);
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
            for(int i = 0; i < positionFrameIndices.Count; i++)
            {
                string currKey = positionFrameIndices.Keys[i];
                int frameIndex = positionFrameIndices[currKey];
                Vector3 delta = animation.GetKeyFrameParameter(currKey, AnimationType.Position, elapsedTime, ref frameIndex) * Weight;
                positionFrameIndices[currKey] = frameIndex;
                nodes[currKey].Translation += delta;
            }

            for (int i = 0; i < rotationFrameIndices.Count; i++)
            {
                string currKey = rotationFrameIndices.Keys[i];
                int frameIndex = rotationFrameIndices[currKey];
                Vector3 delta = animation.GetKeyFrameParameter(currKey, AnimationType.Rotation, elapsedTime, ref frameIndex) * Weight;
                rotationFrameIndices[currKey] = frameIndex;
                nodes[currKey].Rotation += delta;
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
