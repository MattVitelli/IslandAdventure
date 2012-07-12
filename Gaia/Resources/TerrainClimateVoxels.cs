using System;
using System.Collections.Generic;
using System.Xml;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Rendering;

namespace Gaia.Resources
{
    public class TerrainClimateVoxels : IResource
    {
        string name;

        public string Name { get { return name; } }

        public static int MAX_BLEND_ZONES = 8;

        public float[] blendZones = new float[TerrainClimateVoxels.MAX_BLEND_ZONES];
        public float[] gradientCoeffs = new float[TerrainClimateVoxels.MAX_BLEND_ZONES];
        public float[] curvatureCoeffs = new float[TerrainClimateVoxels.MAX_BLEND_ZONES];
        public float[] heightCoeffs = new float[TerrainClimateVoxels.MAX_BLEND_ZONES];
        public float[] baseScores = new float[TerrainClimateVoxels.MAX_BLEND_ZONES];

        public TextureResource BaseMapAtlas;
        public TextureResource NormalMapAtlas;

        int width;
        int height;
        int depth;

        void PopulateAtlas(Texture2D[] textures, Texture3D output)
        {
            RenderTarget2D rt = new RenderTarget2D(GFX.Device, output.Width, output.Height, 1, SurfaceFormat.Color);

            int stride = output.Width * output.Height;

            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;
            Shader drawImageShader = ResourceManager.Inst.GetShader("Generic2D");
            drawImageShader.SetupShader();

            GFX.Device.SamplerStates[0].AddressU = TextureAddressMode.Clamp;
            GFX.Device.SamplerStates[0].AddressV = TextureAddressMode.Clamp;
            GFX.Device.SamplerStates[0].AddressW = TextureAddressMode.Clamp;

            GFX.Device.RenderState.DepthBufferEnable = false;
            GFX.Device.RenderState.DepthBufferWriteEnable = false;

            Color[] colorDataAtlas = new Color[stride * output.Depth];

            for (int i = 0; i < textures.Length; i++)
            {
                GFX.Device.Textures[0] = textures[i];
                GFX.Device.SetVertexShaderConstant(0, new Vector2(1.0f / textures[i].Width, 1.0f / textures[0].Height));

                GFX.Device.SetRenderTarget(0, rt);

                GFXPrimitives.Quad.Render();

                GFX.Device.SetRenderTarget(0, null);

                rt.GetTexture().GetData<Color>(colorDataAtlas, i*stride, stride);
            }
            GFX.Device.Textures[0] = null;

            GFX.Device.DepthStencilBuffer = dsOld;
            GFX.Device.RenderState.DepthBufferEnable = true;
            GFX.Device.RenderState.DepthBufferWriteEnable = true;

            output.SetData<Color>(colorDataAtlas);
        }

        void Create3DTextures(TextureResource[] baseMaps, TextureResource[] normalMaps)
        {

            Texture2D simpleWhite = new Texture2D(GFX.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            Texture2D simpleNormal = new Texture2D(GFX.Device, 1, 1, 1, TextureUsage.None, SurfaceFormat.Color);
            Color[] whiteColor = new Color[1] { Color.White };
            Color[] normalColor = new Color[1] { Color.Blue };
            simpleWhite.SetData<Color>(whiteColor);
            simpleNormal.SetData<Color>(normalColor);


            List<Texture2D> baseTextures = new List<Texture2D>(TerrainClimateVoxels.MAX_BLEND_ZONES);
            List<Texture2D> normalTextures = new List<Texture2D>(TerrainClimateVoxels.MAX_BLEND_ZONES);
            int maxBaseSize = 256;
            int maxNormalSize = 256;

            for (int i = 0; i < TerrainClimate.MAX_BLEND_ZONES; i++)
            {
                if(baseMaps[i] != null)
                {
                    Texture2D baseMap = (Texture2D)baseMaps[i].GetTexture();
                    maxBaseSize = Math.Max(maxBaseSize, baseMap.Width);
                    baseTextures.Add(baseMap);
                }
                else
                    baseTextures.Add(simpleWhite);
                if(normalMaps[i] != null)
                {
                    Texture2D normalMap = (Texture2D)normalMaps[i].GetTexture();
                    maxNormalSize = Math.Max(maxNormalSize, normalMap.Width);
                    normalTextures.Add(normalMap);
                }
                else
                    normalTextures.Add(simpleNormal);
            }

            Texture3D baseAtlas = new Texture3D(GFX.Device, maxBaseSize, maxBaseSize, TerrainClimateVoxels.MAX_BLEND_ZONES, 1, TextureUsage.None, SurfaceFormat.Color);
            Texture3D normalAtlas = new Texture3D(GFX.Device, maxNormalSize, maxNormalSize, TerrainClimateVoxels.MAX_BLEND_ZONES, 1, TextureUsage.None, SurfaceFormat.Color);

            width = maxBaseSize;
            height = maxBaseSize;
            depth = TerrainClimate.MAX_BLEND_ZONES;

            PopulateAtlas(baseTextures.ToArray(), baseAtlas);

            PopulateAtlas(normalTextures.ToArray(), normalAtlas);

            BaseMapAtlas = new TextureResource();
            BaseMapAtlas.SetTexture(TextureResourceType.Texture3D, baseAtlas);

            NormalMapAtlas = new TextureResource();
            NormalMapAtlas.SetTexture(TextureResourceType.Texture3D, normalAtlas);

            baseAtlas.Save("BaseMapAtlas.dds", ImageFileFormat.Dds);

            normalAtlas.Save("NormalMapAtlas.dds", ImageFileFormat.Dds);
        }

        void IResource.Destroy()
        {
        }

        void IResource.LoadFromXML(XmlNode node)
        {
            TextureResource[] baseTextures = new TextureResource[TerrainClimateVoxels.MAX_BLEND_ZONES];
            TextureResource[] normalTextures = new TextureResource[TerrainClimateVoxels.MAX_BLEND_ZONES];
            foreach (XmlAttribute attrib in node.Attributes)
            {
                string[] attribs = attrib.Name.ToLower().Split('_');
                int index = -1;
                if(attribs.Length > 1)
                    index = int.Parse(attribs[1]);
                if (index >= 0 && index < TerrainClimateVoxels.MAX_BLEND_ZONES)
                {
                    switch (attribs[0])
                    {
                        case "basemap":
                                baseTextures[index] = ResourceManager.Inst.GetTexture(attrib.Value);
                            break;
                        case "normalmap":
                            normalTextures[index] = ResourceManager.Inst.GetTexture(attrib.Value);
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
                    }
                }

                if(attrib.Name.ToLower() == "name")
                    name = attrib.Value;
            }

            Create3DTextures(baseTextures, normalTextures);
        }

        public Vector3 GetInverseResolution()
        {
            return Vector3.One / new Vector3(width, height, depth);
        }
    }
}
