using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml;

using Gaia.Rendering;

namespace Gaia.Resources
{
    public enum TextureResourceType
    {
        Texture2D = 0,
        Texture3D,
        TextureCube,
        VertexTexture
    };

    public class TextureResource : IResource
    {
        string name;
        public string Name { get { return name; } }

        Texture2D texture2D = null;
        Texture3D texture3D = null;
        TextureCube textureCube = null;
        TextureResourceType textureType = TextureResourceType.Texture2D;

        public void SetTexture(TextureResourceType type, Texture texture)
        {
            textureType = type;
            switch (textureType)
            {
                case TextureResourceType.Texture3D:
                    texture3D = (Texture3D)texture;
                    break;
                case TextureResourceType.TextureCube:
                    textureCube = (TextureCube)texture;
                    break;
                default:
                    texture2D = (Texture2D)texture;
                    break;
            }
        }

        public TextureResourceType GetTextureType() { return textureType; }

        public Texture GetTexture()
        {
            switch (textureType)
            {
                case TextureResourceType.Texture3D:
                    return texture3D;
                case TextureResourceType.TextureCube:
                    return textureCube;
                default:
                    return texture2D;
            }
        }

        void IResource.Destroy()
        {
            if (texture2D != null)
                texture2D.Dispose();
            texture2D = null;
            if (texture3D != null)
                texture3D.Dispose();
            texture3D = null;
            if (textureCube != null)
                textureCube.Dispose();
            textureCube = null;
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            try
            {
                string filename = "";
                foreach (XmlAttribute attrib in node.Attributes)
                {
                    switch (attrib.Name.ToLower())
                    {
                        case "type":
                            textureType = FindResourceType(attrib.Value.ToLower());
                            break;
                        case "filename":
                            filename = attrib.Value;
                            name = filename;
                            break;
                    }
                }
                LoadTextureFromFile(filename);
            }
            catch { }
        }

        TextureResourceType FindResourceType(string value)
        {
            switch (value)
            {
                case "texture2d":
                    return TextureResourceType.Texture2D;
                case "texture3d":
                    return TextureResourceType.Texture3D;
                case "texturecube":
                    return TextureResourceType.TextureCube;
                case "vertextexture":
                    return TextureResourceType.VertexTexture;
                default:
                    return TextureResourceType.Texture2D;
            }
        }

        void LoadTextureFromFile(string filename)
        {
            switch (textureType)
            {
                case TextureResourceType.Texture3D:
                    texture3D = Texture3D.FromFile(GFX.Device, filename);
                    break;
                case TextureResourceType.TextureCube:
                    textureCube = TextureCube.FromFile(GFX.Device, filename);
                    break;
                default:
                    texture2D = Texture2D.FromFile(GFX.Device, filename);
                    break;
            }
        }
    }
}
