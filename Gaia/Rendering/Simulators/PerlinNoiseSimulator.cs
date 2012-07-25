using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gaia.Rendering;
using Gaia.Resources;
using Gaia.Core;

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
            noise2DShader = new Shader();
            noise2DShader.CompileFromFiles("Shaders/Procedural/PerlinNoise2D.hlsl", "Shaders/PostProcess/GenericV.hlsl");
        }

        Texture2D ComputeRandomTexture(int width, int height)
        {
            float[] randomData = new float[width * height];
            for (int i = 0; i < randomData.Length; i++)
                randomData[i] = (float)RandomHelper.RandomGen.NextDouble() * 2.0f - 1.0f;
            Texture2D texture = new Texture2D(GFX.Device, width, height, 1, TextureUsage.None, SurfaceFormat.Single);
            texture.SetData<float>(randomData);
            return texture;
        }

        public Texture2D Generate2DNoise(NoiseParameters noiseParams, int width, int height, int mipCount)
        {
            Vector2 invRes = Vector2.One / new Vector2(width, height);
            noise2DShader.SetupShader();
            GFX.Device.SetVertexShaderConstant(0, invRes);
            GFX.Device.SetPixelShaderConstant(0, invRes);
            GFX.Device.SetPixelShaderConstant(1, new Vector4(noiseParams.Amplitude, noiseParams.Frequency, noiseParams.Persistance, noiseParams.Octaves));
            GFX.Device.Textures[0] = ComputeRandomTexture(width / 2, height / 2);

            RenderTarget2D rtNoise = new RenderTarget2D(GFX.Device, width, height, 1, SurfaceFormat.Color);
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = GFX.Inst.dsBufferLarge;
            GFX.Device.SetRenderTarget(0, rtNoise);
            GFXPrimitives.Quad.Render();
            GFX.Device.SetRenderTarget(0, null);
            GFX.Device.DepthStencilBuffer = dsOld;

            Color[] colorData = new Color[width * height];
            rtNoise.GetTexture().GetData<Color>(colorData);
            Texture2D noiseTexture = new Texture2D(GFX.Device, width, height, mipCount, TextureUsage.None, SurfaceFormat.Color);
            noiseTexture.SetData<Color>(colorData);
            noiseTexture.GenerateMipMaps(TextureFilter.GaussianQuad);
            return noiseTexture;
        }
    }
}
