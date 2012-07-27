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
        AnimationSequence activeAnimation;
        SortedList<string, float> timeIndices = new SortedList<string, float>();
        ViewModel model;
        string name;

        SortedList<string, ModelBoneAnimationFrame> prevFrames = new SortedList<string, ModelBoneAnimationFrame>();
        SortedList<string, Queue<ModelBoneAnimationFrame>> frames = new SortedList<string, Queue<ModelBoneAnimationFrame>>();
        /*
        public AnimationLayer(string name, ViewModel model, float weight)
        {
            this.name = name;
            this.Weight = weight;
            this.activeAnimation = ResourceManager.Inst.GetAnimation(name);
            this.model = model;
            InitializeFrames();
        }*/

        public AnimationLayer(ViewModel model)
        {
            this.model = model;
        }

        public void SetActiveAnimation(string name, bool firstInit)
        {
            this.activeAnimation = ResourceManager.Inst.GetAnimation(name);
            if(firstInit)
                InitializeFrames();
        }

        public void AddAnimation(string animationName)
        {
            AnimationSequence anim = ResourceManager.Inst.GetAnimation(animationName);
            for (int i = 0; i < frames.Keys.Count; i++)
            {
                string currKey = frames.Keys[i];
                ModelBoneAnimationFrame[] keyFrames = anim.GetKeyFrames(currKey);
                for (int j = 0; j < keyFrames.Length; j++)
                    frames[currKey].Enqueue(keyFrames[j]);
            }
        }

        //This function is used when we need to quickly (within the next frame) swap between animations. 
        //(Such as when a dinosaur's about to bite your face off)
        public void AddAnimation(string animationName, bool immediate)
        {
            if (immediate)
            {
                for (int i = 0; i < frames.Keys.Count; i++)
                {
                    string currKey = frames.Keys[i];
                    if (frames[currKey].Count > 1)
                    {
                        ModelBoneAnimationFrame currFrame = frames[currKey].Dequeue();
                        frames[currKey].Clear();
                        frames[currKey].Enqueue(currFrame); //Reenqueue the old frame
                    }
                }
            }
            AddAnimation(animationName); //Enqueue the new animation as you normally would
        }

        void InitializeFrames()
        {
            string[] keys = activeAnimation.GetAnimationKeys();
            ModelBoneAnimationFrame nullFrame = new ModelBoneAnimationFrame();
            nullFrame.Position = Vector3.Zero;
            nullFrame.Rotation = Vector3.Zero;
            for (int i = 0; i < keys.Length; i++)
            {
                timeIndices.Add(keys[i], 0);
                frames.Add(keys[i], new Queue<ModelBoneAnimationFrame>());
                prevFrames.Add(keys[i], nullFrame);
                ModelBoneAnimationFrame[] keyFrames = activeAnimation.GetKeyFrames(keys[i]);
                for (int j = 0; j < keyFrames.Length; j++)
                    frames[keys[i]].Enqueue(keyFrames[j]);
            }
        }

        public void GetKeyFrameParameter(string name, out Vector3 pos, out Vector3 rot, ref float time)
        {
            if (frames[name].Count == 0)
            {
                pos = Vector3.Zero;
                rot = Vector3.Zero;
                time = 0;
                return;
            }

            ModelBoneAnimationFrame currFrame = frames[name].Peek();
            ModelBoneAnimationFrame oldFrame = prevFrames[name];
            float interpolator = MathHelper.Clamp((float)Math.Sqrt(time / currFrame.time), 0, 1);

            if (interpolator == 1.0f)
            {
                prevFrames[name] = frames[name].Dequeue();
                time = 0;
            }

            pos = Vector3.Lerp(oldFrame.Position, currFrame.Position, interpolator);
            rot = Vector3.Lerp(oldFrame.Rotation, currFrame.Rotation, interpolator);
        }

        public void UpdateAnimation(float timeDT, SortedList<string, AnimationNode> nodes)
        {
            for(int i = 0; i < timeIndices.Count; i++)
            {
                string currKey = timeIndices.Keys[i];
                float time = timeIndices[currKey] + timeDT;
                Vector3 posDelta;
                Vector3 rotDelta;
                GetKeyFrameParameter(currKey, out posDelta, out rotDelta, ref time);
                timeIndices[currKey] = time;
                nodes[currKey].Translation += posDelta * Weight;
                nodes[currKey].Rotation += rotDelta * Weight;

                if (frames[currKey].Count == 0 && activeAnimation.IsCyclic)
                {
                    ModelBoneAnimationFrame[] keyFrames = activeAnimation.GetKeyFrames(currKey);
                    for(int j = 0; j < keyFrames.Length; j++)
                        frames[currKey].Enqueue(keyFrames[j]);
                }
            }
        }
    }
}
