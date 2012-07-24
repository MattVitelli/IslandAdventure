using System;
using System.Collections.Generic;
using System.Xml;

namespace Gaia.Resources
{
    public class WeaponDatablock : IResource
    {
        string name;

        public string Name { get { return name; } }

        public float Damage = 10;

        public int Ammo = 8;

        public bool IsAutomatic = false;

        public bool IsMelee = false;

        public float MuzzleVelocity = 150;

        public int Price;

        void IResource.Destroy()
        {

        }

        void IResource.LoadFromXML(XmlNode node)
        {
            foreach (XmlAttribute attrib in node.Attributes)
            {
                switch (attrib.Name.ToLower())
                {
                    case "name":
                        name = attrib.Value;
                        break;
                    case "ammo":
                        Ammo = int.Parse(attrib.Value);
                        break;
                    case "damage":
                        Damage = float.Parse(attrib.Value);
                        break;
                    case "isautomatic":
                        IsAutomatic = bool.Parse(attrib.Value);
                        break;
                    case "muzzlevelocity":
                        MuzzleVelocity = float.Parse(attrib.Value);
                        break;
                    case "price":
                        Price = int.Parse(attrib.Value);
                        break;
                }
            }
        }
    }
}
