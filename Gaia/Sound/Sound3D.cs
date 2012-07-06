using System;
using System.Collections.Generic;
using IrrKlang;
using Microsoft.Xna.Framework;

using Gaia.Resources;
namespace Gaia.Sound
{
    public class Sound3D : Sound2D
    {
        Vector3 position = Vector3.Zero;
        Vector3 velocity = Vector3.Zero;

        public Vector3 Position
        {
            set
            {
                position = value;
                sound.Position = new Vector3D(position.X, position.Y, position.Z);
            }
            get { return position; }
        }

        public Vector3 Velocity
        {
            set
            {
                velocity = value;
                sound.Velocity = new Vector3D(velocity.X, velocity.Y, velocity.Z);
            }
            get { return velocity; }
        }

        public Sound3D(string soundName, Vector3 pos)
            : base()
        {
            position = pos;
            SoundEffect soundEffect = ResourceManager.Inst.GetSoundEffect(soundName);
            sound = SoundEngine.Device.Play3D(soundEffect.Sound, position.X, position.Y, position.Z, loop, paused, false);
        }
    }
}
