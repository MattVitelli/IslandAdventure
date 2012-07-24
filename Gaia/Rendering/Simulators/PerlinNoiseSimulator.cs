using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gaia.Rendering;
using Gaia.Resources;

namespace Gaia.Rendering.Simulators
{
    public struct NoiseParameters
    {
        public int Octaves;
        public float Persistance;
        public float Amplitude;
        public float Frequency;

        public NoiseParameters(int octaves, float amplitude, float frequency, float persistance)
        {
            Octaves = octaves;
            Amplitude = amplitude;
            Frequency = frequency;
            Persistance = persistance;
        }
    }

    public class PerlinNoiseSimulator
    {
        Shader noise2DShader;
        public PerlinNoiseSimulator()
        {
            noise2DShader = ResourceManager.Inst.GetShader("PerlinNoise2DShader");
        }

        public Texture2D Generate2DNoise(NoiseParameters noiseParams, int width, int height, int mipCount)
        {
            Vector2 invRes = Vector2.One / new Vector2(width, height);
            noise2DShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(0, invRes);
            GFX.Device.SetPixelShaderConstant(0, invRes);

            RenderTarget2D rtNoise = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Color);
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;
            GFX.Device.SetRenderTarget(0, rtNoise);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.DepthStencilBuffer = dsOld;

            Color[] colorData = new Color[width * height];
            rtNoise.GetTexture().GetData<Color>(colorData);
            Texture2D noiseTexture = new Texture2D(GFX.Device, width, height, mipCount, TextureUsage.AutoGenerateMipMap, SurfaceFormat.Color);
            noiseTexture.SetData<Color>(colorData);
            noiseTexture.GenerateMipMaps(TextureFilter.GaussianQuad);
            return noiseTexture;
        }
    }
}
