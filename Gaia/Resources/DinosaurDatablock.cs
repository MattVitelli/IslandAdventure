using System;
using System.Collections.Generic;
using System.Xml;
using Gaia.Core;
namespace Gaia.Resources
{
    public enum DinosaurAnimations
    {
        Idle = 0,
        Run,
        Walk,
        Roar,
        Attack,
        Melee,
        ChargeStart,
        ChargeLoop,
        ChargeFail,
        ChargeSuccess,
        Shove,
        Count,
    };

    public enum DinosaurAnimationsSimple
    {
        Idle = 0,
        Run,
        Walk,
        Roar,
        Attack,
        Melee,
        Jump,
        TurnLeft,
        TurnRight,
        Count,
    };

    public enum DinosaurAnimationStyle
    {
        Simple,
        Complex,
        Raptor
    }

    public class DinosaurDatablock : IResource
    {
        string name;

        public string Name { get { return name; } }

        public float Health = 10;

        public float Damage = 1.0f;

        public float Aggressiveness = 1.0f;

        public float Fear = 0.0f;

        public float Hearing = 1.0f;

        public float Sight = 1.0f;

        public TextureResource HuntImage;

        public List<string>[] Animations = new List<string>[Math.Max((int)DinosaurAnimations.Count, (int)DinosaurAnimationsSimple.Count)];

        public DinosaurAnimationStyle Style = DinosaurAnimationStyle.Simple;

        public string MeshName;

        public int Team;

        public string GetAnimation(DinosaurAnimations anim)
        {
            int index = (int)anim;
            if(Animations[index].Count == 0)
                return string.Empty;
            if (Animations[index].Count == 1)
                return Animations[index][0];

            int randIndex = RandomHelper.RandomGen.Next(0, Animations[index].Count);
            return Animations[index][randIndex];
        }

        void IResource.Destroy()
        {

        }

        void IResource.LoadFromXML(XmlNode node)
        {
            foreach (XmlAttribute attrib in node.Attributes)
            {
                string[] attribs = attrib.Name.ToLower().Split('_');
                switch (attribs[0])
                {
                    case "name":
                        name = attrib.Value;
                        break;
                    case "health":
                        Health = float.Parse(attrib.Value);
                        break;
                    case "damage":
                        Damage = float.Parse(attrib.Value);
                        break;
                    case "aggressive":
                        Aggressiveness = float.Parse(attrib.Value);
                        break;
                    case "fear":
                        Fear = float.Parse(attrib.Value);
                        break;
                    case "hearing":
                        Hearing = float.Parse(attrib.Value);
                        break;
                    case "sight":
                        Sight = float.Parse(attrib.Value);
                        break;
                    case "huntimage":
                        HuntImage = ResourceManager.Inst.GetTexture(attrib.Value);
                        break;
                    case "mesh":
                        MeshName = attrib.Value;
                        break;
                    case "team":
                        Team = int.Parse(attrib.Value);
                        break;
                    case "animationtype":
                        switch (attrib.Value.ToLower())
                        {
                            case "simple":
                                Style = DinosaurAnimationStyle.Simple;
                                break;
                            case "complex":
                                Style = DinosaurAnimationStyle.Complex;
                                break;
                            case "raptor":
                                Style = DinosaurAnimationStyle.Raptor;
                                break;
                        }
                        break;
                    case "animation":
                        int index = int.Parse(attribs[1]);
                        Animations[index].Add(attrib.Value);
                        break;
                }
            }
        }
    }
}
