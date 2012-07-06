using System;
using System.Collections.Generic;
using System.Xml;
using IrrKlang;

namespace Gaia.Resources
{
    public class SoundEffect : IResource
    {
        ISoundSource sound;
        string name = "";
        float minDist = 1;
        float maxDist = 1;
        float volume = 1;

        public ISoundSource Sound;

        public string Name { get { return name; } }


        public float MinDistance
        {
            set
            {
                minDist = value;
                if (sound != null)
                {
                    sound.DefaultMinDistance = minDist;
                }
            }
            get { return minDist; }
        }

        public float MaxDistance
        {
            set
            {
                maxDist = value;
                if (sound != null)
                {
                    sound.DefaultMaxDistance = maxDist;
                }
            }
            get { return maxDist; }
        }

        public float Volume
        {
            set
            {
                volume = value;
                if (sound != null)
                {
                    sound.DefaultVolume = volume;
                }
            }
            get { return volume; }
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            //try
            {
                float vol = 1.25f;
                float mndist = 1;
                float mxdist = 50;
                foreach (XmlAttribute attrib in node.Attributes)
                {
                    switch (attrib.Name)
                    {
                        case "name":
                            name = attrib.Value;
                            break;
                        case "filename":
                            sound = SoundEngine.Device.AddSoundSourceFromFile(attrib.Value);
                            break;
                        case "volume":
                            float.TryParse(attrib.Value, out vol);
                            break;
                        case "mindistance":
                            float.TryParse(attrib.Value, out mndist);
                            break;
                        case "maxdistance":
                            float.TryParse(attrib.Value, out mxdist);
                            break;
                    }
                    Volume = vol;
                    MaxDistance = mxdist;
                    MinDistance = mndist;
                }
            }
            /*
            catch
            {
                sound.Dispose();
            }*/
        }

        void IResource.Destroy()
        {

        }
    }
}
