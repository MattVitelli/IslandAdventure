using System;
using System.Collections.Generic;
using IrrKlang;

namespace Gaia.Resources
{
    class SoundEngine
    {
        ISoundEngine engine;

        static SoundEngine instance = null;

        public static ISoundEngine Device
        {
            get
            {
                return instance.engine;
            }
        }

        public static SoundEngine Inst
        {
            get
            {
                return instance;
            }
        }

        public SoundEngine()
        {
            instance = this;

            engine = new ISoundEngine();
        }

        public void PauseAudio()
        {
            engine.SetAllSoundsPaused(true);
        }

        public void ResumeAudio()
        {
            engine.SetAllSoundsPaused(false);
        }
    }
}
