using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.Xna.Framework;

using Gaia.Rendering;
using Gaia.Core;

namespace Gaia.Resources
{
    public class Material : IResource, IComparable
    {
        Shader shader;
        string name;

        public string Name { get { return name; } }

        const int textureCounts = 16;

        TextureResource[] textures = new TextureResource[textureCounts];

        bool Transparent;
        bool Refract;
        bool Reflect;
        bool Transmissive;

        bool isFirstPerson = false;

        public bool IsTranslucent { get { return (Transmissive || Refract || Transparent); } }
        public bool IsEmissive;
        public string EmissiveMaterial;
        public bool IsFirstPerson { get { return isFirstPerson; } }
        public bool IsFoliage = false;

        float kReflect;
        float kRefract;
        float kIOR;
        float kTrans;
        public Vector3 kAmbient = Vector3.One;
        public Vector3 kDiffuse = Vector3.One;
        public Vector3 kSpecular = Vector3.One;
        float kSpecularPower = 15;
        float kSpecularCoeff = 1;
        float kRimCoeff = 1;

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            Material otherMaterial = obj as Material;
            if (otherMaterial != null)
                return string.Compare(this.name, otherMaterial.name);
            else
                throw new ArgumentException("Object is not a materials");
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
                    case "reflect":
                        Reflect = bool.Parse(attrib.Value);
                        break;
                    case "refract":
                        Refract = bool.Parse(attrib.Value);
                        break;
                    case "transmissive":
                        Transmissive = bool.Parse(attrib.Value);
                        break;
                    case "transparent":
                        Transparent = bool.Parse(attrib.Value);
                        break;
                    case "emissive":
                        IsEmissive = bool.Parse(attrib.Value);
                        break;
                    case "emissivematerial":
                        EmissiveMaterial = attrib.Value;
                        break;

                    case "foliage":
                        IsFoliage = bool.Parse(attrib.Value);
                        break;

                    case "kreflect":
                        kReflect = float.Parse(attrib.Value);
                        break;
                    case "krefract":
                        kRefract = float.Parse(attrib.Value);
                        break;
                    case "kior":
                        kIOR = float.Parse(attrib.Value);
                        break;
                    case "ktrans":
                        kTrans = float.Parse(attrib.Value);
                        break;

                    case "kambient":
                        kAmbient = ParseUtils.ParseVector3(attrib.Value);
                        break;
                    case "kdiffuse":
                        kDiffuse = ParseUtils.ParseVector3(attrib.Value);
                        break;
                    case "kspecular":
                        kSpecular = ParseUtils.ParseVector3(attrib.Value);
                        break;
                    case "kspecpower":
                        kSpecularPower = float.Parse(attrib.Value)/255.0f;
                        break;
                    case "krimcoeff":
                        kRimCoeff = float.Parse(attrib.Value);
                        break;
                    case "kspeccoeff":
                        kSpecularCoeff = float.Parse(attrib.Value);
                        break;

                    case "isfirstperson":
                        isFirstPerson = bool.Parse(attrib.Value);
                        break;

                    case "texture":
                        int index = int.Parse(attribs[1]);
                        if(index >= 0 && index < textureCounts)
                            textures[index] = ResourceManager.Inst.GetTexture(attrib.Value);
                        break;

                    case "shader":
                        shader = ResourceManager.Inst.GetShader(attrib.Value);
                        break;

                    case "name":
                        name = attrib.Value;
                        break;
                }
            }
        }

        Vector4 GetSpecularPower()
        {
            return new Vector4(kSpecularPower, kSpecularCoeff, kRimCoeff, 0);
        }

        void SetupMaterialParameters()
        {
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_AMBIENT, kAmbient);
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_DIFFUSE, kDiffuse);
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_SPECULAR, kSpecular);
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_SPECPOWER, GetSpecularPower());
        }

        public void SetupTextures()
        {
            for (int i = 0; i < textures.Length; i++)
            {
                if (textures[i] != null)
                {
                    GFX.Device.Textures[i] = textures[i].GetTexture();
                    GFX.Inst.SetTextureFilter(i, Microsoft.Xna.Framework.Graphics.TextureFilter.Anisotropic);
                    GFX.Inst.SetTextureAddressMode(i, Microsoft.Xna.Framework.Graphics.TextureAddressMode.Wrap);
                    GFX.Device.SamplerStates[i].MaxMipLevel = textures[i].GetTexture().LevelOfDetail;
                }
            }
        }

        public void SetTexture(int index, TextureResource texture)
        {
            if (index >= 0 && index < textures.Length)
                textures[index] = texture;
        }

        public void SetShader(Shader newShader)
        {
            this.shader = newShader;
        }

        public void SetupMaterial()
        {
            shader.SetupShader();

            SetupMaterialParameters();

            SetupTextures();
        }
    }
}
