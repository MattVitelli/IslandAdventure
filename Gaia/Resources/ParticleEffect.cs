using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

using Gaia.Rendering;
using Gaia.Core;

namespace Gaia.Resources
{
    public class ParticleEffect : IResource
    {
        public Material material;

        public float fadeInCoeff;
        public float fadeOutCoeff;

        public float fadeInPercent;
        public float fadeOutPercent;

        public float randomInitSpeed;
        public float initialSpeed;
        public float initialSpeedVariance;
        public Vector3 initialDirection;

        public float lifetime;         // Lifetime of particles
        public float lifetimeVariance; // Varience in lifetime from 0 to n

        public float mass;
        public float massVariance;

        public float size;
        public float sizeVariance;

        public Vector4 offsetParameters;

        public float densityRatio;

        public Vector3 color = Vector3.One*0.5f;
        
        string name;

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
                    case "size":
                        size = float.Parse(attrib.Value);
                        break;

                    case "sizevariance":
                        sizeVariance = float.Parse(attrib.Value);
                        break;
                    
                    case "density":
                        densityRatio = float.Parse(attrib.Value);
                        break;

                    case "initialdirection":
                        initialDirection = ParseUtils.ParseVector3(attrib.Value);
                        break;

                    case "initialspeed":
                        initialSpeed = float.Parse(attrib.Value);
                        break;
                    
                    case "initialspeedvariance":
                        initialSpeedVariance = float.Parse(attrib.Value);
                        break;

                    case "fadeinpercent":
                        fadeInPercent = float.Parse(attrib.Value);
                        fadeInCoeff = 1.0f / fadeInPercent;
                        break;

                    case "fadeoutpercent":
                        fadeOutPercent = float.Parse(attrib.Value);
                        fadeOutCoeff = 1.0f / (1.0f - fadeOutPercent);
                        break;

                    case "randominitspeed":
                        randomInitSpeed = float.Parse(attrib.Value);
                        break;

                    case "lifetime":
                        lifetime = float.Parse(attrib.Value);
                        break;

                    case "lifetimevariance":
                        lifetimeVariance = float.Parse(attrib.Value);
                        break;

                    case "mass" :
                        mass = float.Parse(attrib.Value);
                        break;

                    case "massvariance":
                        massVariance = float.Parse(attrib.Value);
                        break;
                        
                    case "material":
                        material = ResourceManager.Inst.GetMaterial(attrib.Value);
                        break;

                    case "randomoffset":
                        offsetParameters = new Vector4(ParseUtils.ParseVector3(attrib.Value), offsetParameters.W);
                        break;

                    case "offsetmagnitude":
                        offsetParameters.W = float.Parse(attrib.Value);
                        break;

                    case "name":
                        name = attrib.Value;
                        break;

                    case "color":
                        color = ParseUtils.ParseVector3(attrib.Value);
                        break;
                }
            }
        }
    }
}
