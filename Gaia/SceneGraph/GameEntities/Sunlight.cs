using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

using Gaia.Core;
namespace Gaia.SceneGraph.GameEntities
{
    public class Sunlight : Light
    {
        float theta = 0;
        float phi = MathHelper.Pi/2.0f;

        const float PhiCycle = MathHelper.Pi / 60.0f;
        const float ThetaCycle = MathHelper.TwoPi / 60.0f;
        
        Vector3 dawnColor = new Vector3(1.1f,0.6545f,0.613f);
        Vector3 noonColor = Vector3.One;
        Vector3 nightColor = new Vector3(0.5849f, 0.6975f, 1.0f);

        public Sunlight()
            : base(LightType.Directional, Vector3.One, Vector3.Up, true)
        {

        }

        public override void OnUpdate()
        {
            //theta += Time.GameTime.ElapsedTime * ThetaCycle;
            //phi += Time.GameTime.ElapsedTime * PhiCycle;
            if (theta >= MathHelper.TwoPi)
                theta -= MathHelper.TwoPi;
            if (phi >= MathHelper.Pi)
                phi -= MathHelper.Pi;

            Vector3 pos = Vector3.Normalize(new Vector3((float)Math.Sin(theta), (float)Math.Sin(phi), (float)Math.Cos(theta)));
            this.Transformation.SetPosition(pos);
            Color = (pos.Y >= 0.0f) ? Vector3.Lerp(dawnColor, noonColor, pos.Y) : Vector3.Lerp(nightColor, dawnColor, 1.0f + pos.Y);
            base.OnUpdate();
        }
    }
}
