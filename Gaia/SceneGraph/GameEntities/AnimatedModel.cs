using System;
using System.Collections.Generic;
using Gaia.Animation;
using Gaia.Core;
using Microsoft.Xna.Framework;

namespace Gaia.SceneGraph.GameEntities
{
    public class AnimatedModel : Model
    {
        protected SortedList<string, AnimationLayer> animationLayers = new SortedList<string, AnimationLayer>();
        protected SortedList<string, Vector3> defaultTranslations = new SortedList<string, Vector3>();
        protected SortedList<string, Vector3> defaultRotations = new SortedList<string, Vector3>();

        public AnimatedModel(string name)
            : base(name)
        {

        }

        public override void OnAdd(Scene scene)
        {
            base.OnAdd(scene);
            for (int i = 0; i < nodes.Count; i++)
            {
                string currKey = nodes.Keys[i];
                defaultTranslations.Add(currKey, nodes[currKey].TranslationDelta);
                defaultRotations.Add(currKey, nodes[currKey].RotationDelta);
            }
        }
        public void SetAnimationLayer(string name, float weight, bool isCyclic)
        {
            if (!animationLayers.ContainsKey(name))
                animationLayers.Add(name, new AnimationLayer(name, this, weight, isCyclic));
            else
            {
                animationLayers[name].IsCyclic = isCyclic;
                animationLayers[name].Weight = weight;
            }
        }

        public void SetAnimationLayer(string name, float weight)
        {
            SetAnimationLayer(name, weight, false);
        }

        public void RemoveAnimationLayer(string name)
        {
            if (animationLayers.ContainsKey(name))
                animationLayers.Remove(name);
        }

        protected void UpdateAnimation(float timeDT)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                string currKey = nodes.Keys[i];
                nodes[currKey].TranslationDelta = defaultTranslations[currKey];
                nodes[currKey].RotationDelta = defaultRotations[currKey];
            }

            for (int i = 0; i < animationLayers.Count; i++)
            {
                animationLayers.Values[i].UpdateAnimation(timeDT, this.nodes);
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            UpdateAnimation(Time.GameTime.ElapsedTime);
            Matrix transform = this.Transformation.GetTransform();
            for (int i = 0; i < rootNodes.Length; i++)
                rootNodes[i].ApplyTransform(ref transform);
        }

    }
}
