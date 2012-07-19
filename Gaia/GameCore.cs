using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using System.IO;

using Gaia.Core;
using Gaia.Input;
using Gaia.Rendering;
using Gaia.Resources;
using Gaia.SceneGraph;
using Gaia.Editors;
using Gaia.UI;
using Gaia.Game;

namespace Gaia
{
    public class GameCore : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        Scene mainScene; //Our default level
        LevelEditor editor;
        PlayerScreen playerScreen;

        public GameCore()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            new GFX(this.GraphicsDevice, this.Content);
            new InputManager();
            new SoundEngine();
            new ResourceManager();
            ResourceManager.Inst.LoadResources();
            
            GFX.Inst.GetGUI().DefaultFont = Content.Load<SpriteFont>("SimpleFont");

            mainScene = new Scene();
            playerScreen = new PlayerScreen(mainScene);
            editor = new LevelEditor(mainScene);
            /*
            using (FileStream fs = new FileStream("Graphics.txt", FileMode.Create))
            {
                using (StreamWriter wr = new StreamWriter(fs))
                {
                    PrintPresentationParameters(wr, GFX.Device.PresentationParameters);
                    PrintRenderState(wr, GFX.Device.RenderState);
                    PrintDeviceCapabilities(wr, GFX.Device.GraphicsDeviceCapabilities);
                }
            }*/
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            SoundEngine.Inst.ResumeAudio();
            InputManager.Inst.StickyInput = true;
            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            SoundEngine.Inst.PauseAudio();
            InputManager.Inst.StickyInput = false;
            base.OnDeactivated(sender, args);
        }

        void PrintRenderState(StreamWriter wr, RenderState state)
         {
             wr.WriteLine("/n /n -----------Render State-----------");
             wr.WriteLine("AlphaBlendEnable : {0}", state.AlphaBlendEnable);
             wr.WriteLine("AlphaBlendOperation : {0}", state.AlphaBlendOperation);
             wr.WriteLine("AlphaDestinationBlend : {0}",state.AlphaDestinationBlend);
             wr.WriteLine("AlphaFunction : {0}", state.AlphaFunction);
             wr.WriteLine("AlphaSourceBlend : {0}", state.AlphaSourceBlend);
             wr.WriteLine("AlphaTestEnable : {0}", state.AlphaTestEnable);
             wr.WriteLine("BlendFactor : {0}", state.BlendFactor);
             wr.WriteLine("BlendFunction : {0}", state.BlendFunction);
             wr.WriteLine("ColorWriteChannels : {0}", state.ColorWriteChannels);
             wr.WriteLine("ColorWriteChannels1 : {0}", state.ColorWriteChannels1);
             wr.WriteLine("ColorWriteChannels2 : {0}", state.ColorWriteChannels2);
             wr.WriteLine("ColorWriteChannels3 : {0}", state.ColorWriteChannels3);
             wr.WriteLine("CounterClockwiseStencilDepthBufferFail :{0}", state.CounterClockwiseStencilDepthBufferFail);
             wr.WriteLine("CounterClockwiseStencilFail : {0}", state.CounterClockwiseStencilFail);
             wr.WriteLine("CounterClockwiseStencilFunction : {0}",state.CounterClockwiseStencilFunction);
             wr.WriteLine("CounterClockwiseStencilPass : {0}",state.CounterClockwiseStencilPass);
             wr.WriteLine("CullMode : {0}", state.CullMode);
             wr.WriteLine("DepthBias : {0}", state.DepthBias);
             wr.WriteLine("DepthBufferEnable : {0}", state.DepthBufferEnable);
             wr.WriteLine("DepthBufferFunction : {0}", state.DepthBufferFunction);
             wr.WriteLine("DepthBufferWriteEnable : {0}",state.DepthBufferWriteEnable);
             wr.WriteLine("DestinationBlend : {0}", state.DestinationBlend);
             wr.WriteLine("FillMode : {0}", state.FillMode);
             wr.WriteLine("FogColor : {0}", state.FogColor);
             wr.WriteLine("FogDensity : {0}", state.FogDensity);
             wr.WriteLine("FogEnable : {0}", state.FogEnable);
             wr.WriteLine("FogEnd : {0}", state.FogEnd);
             wr.WriteLine("FogStart : {0}", state.FogStart);
             wr.WriteLine("FogTableMode : {0}", state.FogTableMode);
             wr.WriteLine("FogVertexMode : {0}", state.FogVertexMode);
             wr.WriteLine("MultiSampleAntiAlias : {0}",state.MultiSampleAntiAlias);
             wr.WriteLine("MultiSampleMask : {0}", state.MultiSampleMask);
             wr.WriteLine("PointSize : {0}", state.PointSize);
             wr.WriteLine("PointSizeMax : {0}", state.PointSizeMax);
             wr.WriteLine("PointSizeMin : {0}", state.PointSizeMin);
             wr.WriteLine("PointSpriteEnable : {0}", state.PointSpriteEnable);
             wr.WriteLine("RangeFogEnable : {0}", state.RangeFogEnable);
             wr.WriteLine("ReferenceAlpha : {0}", state.ReferenceAlpha);
             wr.WriteLine("ReferenceStencil : {0}", state.ReferenceStencil);
             wr.WriteLine("ScissorTestEnable : {0}", state.ScissorTestEnable);
             wr.WriteLine("SeparateAlphaBlendEnabled : {0}",state.SeparateAlphaBlendEnabled);
             wr.WriteLine("SlopeScaleDepthBias : {0}", state.SlopeScaleDepthBias);
             wr.WriteLine("SourceBlend : {0}", state.SourceBlend);
             wr.WriteLine("StencilDepthBufferFail : {0}",state.StencilDepthBufferFail);
             wr.WriteLine("StencilEnable : {0}", state.StencilEnable);
             wr.WriteLine("StencilFail : {0}", state.StencilFail);
             wr.WriteLine("StencilFunction : {0}", state.StencilFunction);
             wr.WriteLine("StencilMask : {0}", state.StencilMask);
             wr.WriteLine("StencilPass : {0}", state.StencilPass);
             wr.WriteLine("StencilWriteMask : {0}", state.StencilWriteMask);
             wr.WriteLine("TwoSidedStencilMode : {0}", state.TwoSidedStencilMode);
             wr.WriteLine("Wrap0 : {0}", state.Wrap0);
             wr.WriteLine("Wrap1 : {0}", state.Wrap1);
             wr.WriteLine("Wrap10 : {0}", state.Wrap10);
             wr.WriteLine("Wrap11 : {0}", state.Wrap11);
         }

         void PrintPresentationParameters(StreamWriter wr, PresentationParameters state)
         {
             wr.WriteLine("/n /n -----------Presentation Parameters-----------");
             wr.WriteLine("AutoDepthStencilFormat : {0}", state.AutoDepthStencilFormat);
             wr.WriteLine("BackBufferCount : {0}", state.BackBufferCount);
             wr.WriteLine("BackBufferFormat : {0}", state.BackBufferFormat);
             wr.WriteLine("BackBufferHeight : {0}", state.BackBufferHeight);
             wr.WriteLine("BackBufferWidth : {0}", state.BackBufferWidth);
             wr.WriteLine("EnableAutoDepthStencil : {0}", state.EnableAutoDepthStencil);
             wr.WriteLine("MultiSampleQuality : {0}", state.MultiSampleQuality);
             wr.WriteLine("MultiSampleType : {0}", state.MultiSampleType);
             wr.WriteLine("PresentOptions : {0}", state.PresentOptions);
             wr.WriteLine("RenderTargetUsage : {0}", state.RenderTargetUsage);
             wr.WriteLine("SwapEffect : {0}", state.SwapEffect);
         }

         void PrintDeviceCapabilities(StreamWriter wr, GraphicsDeviceCapabilities state)
         {
             wr.WriteLine("/n /n -----------Device Capabilities-----------");
             wr.WriteLine("AdapterOrdinalInGroup : {0}", state.AdapterOrdinalInGroup);
             wr.WriteLine("DeviceType : {0}", state.DeviceType);
         }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {

        }

        void ClampMouse()
        {
            if (this.IsActive)
            {
                int mouseX = Mouse.GetState().X;
                int mouseY = Mouse.GetState().Y;
                bool setMousePos = false;
                int width = this.Window.ClientBounds.Width;
                int height = this.Window.ClientBounds.Height;
                if (mouseX < 0 || mouseX > width)
                {
                    mouseX = Math.Max(Math.Min(mouseX, width), 0);
                    setMousePos = true;
                }
                if (mouseY < 0 || mouseY > height)
                {
                    mouseY = Math.Max(Math.Min(mouseY, height), 0);
                    setMousePos = true;
                }

                if (setMousePos)
                    Mouse.SetPosition(mouseX, mouseY);
            }
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if(!editor.Visible)
                ClampMouse();
            Time.GameTime.Elapse(gameTime.ElapsedGameTime.Milliseconds);
            Time.GameTime.DT = (float)gameTime.ElapsedGameTime.Ticks / (float)TimeSpan.TicksPerSecond;
            //Update functions here
            InputManager.Inst.Update();
            if(InputManager.Inst.IsKeyDown(GameKey.Pause))
                this.Exit();

            if (InputManager.Inst.IsKeyDownOnce(GameKey.LaunchEditor))
            {
                if (!editor.Visible)
                    editor.Show();
                else
                    editor.Hide();
            }

            mainScene.Update();
            playerScreen.OnUpdate(Time.GameTime.DT);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            Time.RenderTime.Elapse(gameTime.ElapsedGameTime.Milliseconds);
            Time.RenderTime.DT = (float)gameTime.ElapsedGameTime.Ticks / (float)TimeSpan.TicksPerSecond;
            GFX.Inst.AdvanceSimulations((float)gameTime.ElapsedGameTime.Milliseconds / 1000.0f);
            GFX.Device.SetVertexShaderConstant(GFXShaderConstants.VC_TIME, Vector4.One*Time.RenderTime.TotalTime);
            GFX.Device.SetPixelShaderConstant(GFXShaderConstants.PC_TIME, Vector4.One * Time.RenderTime.TotalTime);
            mainScene.Render();
            playerScreen.OnRender();
            GFX.Inst.RenderGUI();

            base.Draw(gameTime);
        }
    }
}
