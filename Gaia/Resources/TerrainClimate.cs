using System;
using System.Collections.Generic;
using System.Xml;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Rendering;

namespace Gaia.Resources
{
    public class TerrainClimate : IResource, IComparable
    {
        string name;

        public string Name { get { return name; } }

        public static int MAX_BLEND_ZONES = 4;
        public float[] blendZones = new float[MAX_BLEND_ZONES];
        public float[] gradientCoeffs = new float[MAX_BLEND_ZONES];
        public float[] curvatureCoeffs = new float[MAX_BLEND_ZONES];
        public float[] heightCoeffs = new float[MAX_BLEND_ZONES];
        public float[] baseScores = new float[MAX_BLEND_ZONES];

        public float[] ClutterDensity = new float[MAX_BLEND_ZONES];
        public Material[] ClutterMaterials = new Material[MAX_BLEND_ZONES];

        public TextureResource[] BaseTextures = new TextureResource[MAX_BLEND_ZONES];
        public TextureResource[] NormalTextures = new TextureResource[MAX_BLEND_ZONES];

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            TerrainClimate otherMaterial = obj as TerrainClimate;
            if (otherMaterial != null)
                return string.Compare(this.name, otherMaterial.name);
            else
                throw new ArgumentException("Object is not a TerrainClimate");
        }

        void IResource.Destroy()
        {
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            for (int i = 0; i < MAX_BLEND_ZONES; i++)
            {
                blendZones[i] = 0.0001f;
                ClutterDensity[i] = 0.35f;
            }

            foreach (XmlAttribute attrib in node.Attributes)
            {
                string[] attribs = attrib.Name.ToLower().Split('_');
                int index = -1;
                if(attribs.Length > 1)
                    index = int.Parse(attribs[1]);
                if (index >= 0 && index < MAX_BLEND_ZONES)
                {
                    switch (attribs[0])
                    {
                        case "basemap":
                            BaseTextures[index] = ResourceManager.Inst.GetTexture(attrib.Value);
                            break;
                        case "normalmap":
                            NormalTextures[index] = ResourceManager.Inst.GetTexture(attrib.Value);
                            break;
                        case "blendzone":
                            blendZones[index] = float.Parse(attrib.Value);
                            break;
                        case "gradient":
                            gradientCoeffs[index] = float.Parse(attrib.Value);
                            break;
                        case "curvature":
                            curvatureCoeffs[index] = float.Parse(attrib.Value);
                            break;
                        case "score":
                            baseScores[index] = float.Parse(attrib.Value);
                            break;
                        case "height":
                            heightCoeffs[index] = float.Parse(attrib.Value);
                            break;
                        case "clutterdensity":
                            ClutterDensity[index] = float.Parse(attrib.Value);
                            break;
                        case "material":
                            ClutterMaterials[index] = ResourceManager.Inst.GetMaterial(attrib.Value);
                            break;
                    }
                }

                if(attrib.Name.ToLower() == "name")
                    name = attrib.Value;
            }
        }
    }
}
