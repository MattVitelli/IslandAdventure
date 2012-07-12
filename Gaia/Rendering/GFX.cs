using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using Gaia.Rendering.Simulators;
namespace Gaia.Rendering
{
    public enum GFXTextureDataType
    {
        BYTE,
        COLOR,
        SINGLE,
        HALFSINGLE,
    };

    public enum RenderPass
    {
        Shadows = 0,
        Terrain,
        Scene,
        TransparentGBuffer,
        TransparentColor,
        Light,
        Particles,
        Sky,
        Translucent,
        Emissive,
        PostProcess,
        FirstPersonPrepass,
        FirstPerson,
        Foliage,
        Decal,
        UI,
        Count
    };

    public class GFX
    {
        public Vector2 Origin = Vector2.Zero;
        public Vector2 DisplayRes
        {
            get { return new Vector2(Device.PresentationParameters.BackBufferWidth, Device.PresentationParameters.BackBufferHeight); }
        }

        GraphicsDevice device;
        public DepthStencilBuffer dsBufferLarge;
        public static GFX Inst = null;

        public static GraphicsDevice Device
        {
            get { return Inst.device; }
        }

        GUIElementManager guiManager;

        public GUIElementManager GetGUI()
        {
            return guiManager;
        }

        public SurfaceFormat ByteSurfaceFormat = SurfaceFormat.Luminance8;
        public GFXTextureDataType ByteSurfaceDataType = GFXTextureDataType.BYTE;

        DepthStencilBuffer DSBufferScene;

        public ParticleSimulator particleSystem;

        ContentManager contentManager;

        public GFX(GraphicsDevice device, ContentManager contentManager)
        {
            Inst = this;
            GFXShaderConstants.AuthorShaderConstantFile();
            RegisterDevice(device);
            this.contentManager = contentManager;
            guiManager = new GUIElementManager(contentManager.Load<SpriteFont>("SimpleFont"));
        }
        ~GFX()
        {
        }

        void InitializeSimulations()
        {
            particleSystem = new ParticleSimulator();
        }

        public void AdvanceSimulations(float timeDT)
        {
            DepthStencilBuffer dsOld = GFX.Device.DepthStencilBuffer;
            GFX.Device.DepthStencilBuffer = dsBufferLarge;
            particleSystem.AdvanceSimulation(timeDT);
            GFX.Device.DepthStencilBuffer = dsOld;
        }

        public Matrix ComputeTextureMatrix(Vector2 resolution)
        {
            Vector2 offset = Vector2.One * 0.5f / resolution;
            Matrix mat = Matrix.Identity;

            mat.M11 = 0.5f;
            mat.M12 = 0;
            mat.M13 = 0;
            mat.M14 = 0.5f + offset.X;
            mat.M21 = 0;
            mat.M22 = -0.5f;
            mat.M23 = 0;
            mat.M24 = 0.5f + offset.Y;
            mat.M31 = 0;
            mat.M32 = 0;
            mat.M33 = 1;
            mat.M34 = 0;
            mat.M41 = 0;
            mat.M42 = 0;
            mat.M43 = 0;
            mat.M44 = 1;
            return mat;
        }

        public void RegisterDevice(GraphicsDevice device)
        {
            this.device = device;
            GFXVertexDeclarations.Initialize();
            GFXPrimitives.Initialize();
            InitializeSurfaceModes();
            InitializeSamplerStates();
            InitializeTextures();
            InitializeSimulations();
        }

        public void ResetState()
        {
            InitializeSamplerStates();
            Device.RenderState.CullMode = CullMode.CullCounterClockwiseFace;
            Device.RenderState.DepthBufferEnable = true;
            Device.RenderState.DepthBufferFunction = CompareFunction.LessEqual;
            Device.RenderState.DepthBufferWriteEnable = true;
            Device.RenderState.AlphaBlendEnable = false;
        }

        void InitializeSurfaceModes()
        {
            SurfaceFormat[] formatEnum = { SurfaceFormat.Luminance8, SurfaceFormat.HalfSingle, SurfaceFormat.Color, SurfaceFormat.Single };
            GFXTextureDataType[] formatDataType = { GFXTextureDataType.BYTE, GFXTextureDataType.HALFSINGLE, GFXTextureDataType.COLOR, GFXTextureDataType.SINGLE };

            int i = 0;
            while (i < formatEnum.Length && !GraphicsAdapter.DefaultAdapter.CheckDeviceFormat(DeviceType.Hardware, GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Format,
                    TextureUsage.None, QueryUsages.None, ResourceType.RenderTarget, formatEnum[i]))
            {
                i++;
            }
            ByteSurfaceFormat = formatEnum[i];
            ByteSurfaceDataType = formatDataType[i];
        }

        public void SetTextureFilter(int index, TextureFilter filter)
        {
            TextureFilter mipFilter = filter;
            if (filter == TextureFilter.Anisotropic)
            {
                GFX.Device.SamplerStates[index].MaxAnisotropy = 16;
                mipFilter = TextureFilter.Linear;
            }
            GFX.Device.SamplerStates[index].MagFilter = filter;
            GFX.Device.SamplerStates[index].MinFilter = filter;
            GFX.Device.SamplerStates[index].MipFilter = mipFilter;
            
        }

        public void SetTextureAddressMode(int index, TextureAddressMode mode)
        {
            GFX.Device.SamplerStates[index].AddressU = mode;
            GFX.Device.SamplerStates[index].AddressV = mode;
            GFX.Device.SamplerStates[index].AddressW = mode;
        }

        public void InitializeSamplerStates()
        {
            for (int i = 0; i < 8; i++)
            {
                GFX.Device.SamplerStates[i].AddressU = TextureAddressMode.Wrap;
                GFX.Device.SamplerStates[i].AddressV = TextureAddressMode.Wrap;
                GFX.Device.SamplerStates[i].MagFilter = TextureFilter.Anisotropic;
                GFX.Device.SamplerStates[i].MinFilter = TextureFilter.Anisotropic;
                GFX.Device.SamplerStates[i].MipFilter = TextureFilter.Anisotropic;
                GFX.Device.SamplerStates[i].MaxAnisotropy = 16;
            }
        }

        void InitializeTextures()
        {
            int width = (int)DisplayRes.X;
            int height = (int)DisplayRes.Y;

            DSBufferScene = new DepthStencilBuffer(GFX.Device, width, height, Device.DepthStencilBuffer.Format);

            dsBufferLarge = new DepthStencilBuffer(Device, 2048, 2048, Device.DepthStencilBuffer.Format);
        }

        public void RenderGUI()
        {
            guiManager.Render();
            
        }
    }
}
