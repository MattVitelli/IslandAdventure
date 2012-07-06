using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Resources;
namespace Gaia.SceneGraph.GameEntities
{
    public class Cloud : Entity
    {
        List<ParticleEmitter> emitters = new List<ParticleEmitter>();
        public Cloud()
        {
            Vector3 pos = this.Transformation.GetPosition();
            emitters.Add(new ParticleEmitter(ResourceManager.Inst.GetParticleEffect("Smoke2"), 100));
            emitters[emitters.Count - 1].Transformation.SetPosition(pos);
            emitters.Add(new ParticleEmitter(ResourceManager.Inst.GetParticleEffect("Smoke1"), 100));
            emitters[emitters.Count - 1].Transformation.SetPosition(pos+Vector3.Up * 20);
            emitters.Add(new ParticleEmitter(ResourceManager.Inst.GetParticleEffect("Smoke4"), 100));
            emitters[emitters.Count - 1].Transformation.SetPosition(pos + Vector3.Up * 40);
            emitters.Add(new ParticleEmitter(ResourceManager.Inst.GetParticleEffect("Smoke3"), 100));
            emitters[emitters.Count - 1].Transformation.SetPosition(pos + Vector3.Up * 60);
            emitters.Add(new ParticleEmitter(ResourceManager.Inst.GetParticleEffect("Smoke0"), 100));
            emitters[emitters.Count - 1].Transformation.SetPosition(pos + Vector3.Up * 70);
        }

        public override void OnAdd(Scene scene)
        {
            for (int i = 0; i < emitters.Count; i++)
            {
                emitters[i].OnAdd(scene);
            }
            base.OnAdd(scene);
        }

        public override void OnDestroy()
        {
            for (int i = 0; i < emitters.Count; i++)
            {
                emitters[i].OnDestroy();
            }
            base.OnDestroy();
        }

        public override void OnUpdate()
        {
            for (int i = 0; i < emitters.Count; i++)
            {
                emitters[i].OnUpdate();
            }
            base.OnUpdate();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            for (int i = 0; i < emitters.Count; i++)
            {
                emitters[i].OnRender(view);
            }
            base.OnRender(view);
        }
    }
}
