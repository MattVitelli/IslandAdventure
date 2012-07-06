using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Gaia.Resources;
using Gaia.Rendering;
using Gaia.Core;
using Gaia.Input;

namespace Gaia.SceneGraph.GameEntities
{
    public class RoundManager : Entity
    {
        const float ROUND_TIME = 30;            //Time in seconds that a round lasts
        const float DELAY_NEXT_ROUND_TIME = 5;  //Time in seconds between rounds
        const float END_GAME_TIME = 10;         //Time in seconds before game ends

        SortedList<string, Queue<Tank>> Teams = new SortedList<string, Queue<Tank>>();

        bool gameEnded = false;
        bool isPaused = false;
        bool advancingRound = false;
        int currTeam = 0;
        Tank currTank = null;

        float timeRemaining = ROUND_TIME;

        string playerTeamName = string.Empty;

        enum RoundGUIMode
        {
            None = 0x0,
            DisplayTime = 0x1,
            DisplayIdleGui = 0x2,
            DisplayMainGui = 0x4,
        };

        int guiFlag = (int)RoundGUIMode.DisplayTime + (int)RoundGUIMode.DisplayMainGui;

        const float SHOW_GUI_IDLE_TIME = 1.5f;
        const float SHOW_GUI_MIN_ALPHA_TIME = 0.75f;
        float elapsedIdleTime = 0;

        const float SHOW_WEAPONS_IDLE_TIME = 0.5f;
        float elasedWeaponsVisibleTime = 0;
        bool weaponScreenVisible = false;

        string[] compassDirs = { "E", "N", "W", "S"};
        float[] compassTCs = { 0, 0.25f, 0.5f, 0.75f };
        const int compassTextureWidth = 512;
        const int compassTextureHeight = 2;
        const int compassTickDelta = 15;
        Texture2D compassTexture;

        bool anyButtonClicked = false;

        public void AddTeam(string teamName)
        {
            if (Teams.ContainsKey(teamName))
                return;

            Teams.Add(teamName, new Queue<Tank>());
        }

        public void AddTank(string teamName, Tank tank)
        {
            if (!Teams.ContainsKey(teamName))
                AddTeam(teamName);
            Teams[teamName].Enqueue(tank);
            
            if(currTank == null)
                currTank = tank;
        }

        public void SetPlayerTeamName(string name)
        {
            playerTeamName = name;
        }

        public override void OnAdd(Scene scene)
        {
            CreateCompassTexture();
            base.OnAdd(scene);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (isPaused && !gameEnded)
                return;

            timeRemaining -= Time.GameTime.ElapsedTime;
            if (timeRemaining <= 0)
            {
                if (!gameEnded)
                {
                    if (!advancingRound)
                        BeginAdvancingRound();
                    else
                        AdvanceRound();
                }

                //Otherwise, return to main menu
            }

            if (currTank != null)
            {
                if (InputManager.Inst.IsKeyDownOnce(GameKey.ShowWeapons))
                {
                    weaponScreenVisible = !weaponScreenVisible;
                    elasedWeaponsVisibleTime = 0;
                }
                /*
                if (currTank.GetVelocity().Length() < 0.2f)
                {
                    elapsedIdleTime += Time.GameTime.ElapsedTime;
                    guiFlag |= (int)RoundGUIMode.DisplayIdleGui;
                    bool controlTank = (InputManager.Inst.IsLeftMouseDown() && !anyButtonClicked);
                    InputManager.Inst.StickyInput = controlTank;
                    currTank.SetControllable(controlTank);
                }
                else*/
                {
                    elapsedIdleTime = 0;
                    guiFlag = guiFlag & ~(int)RoundGUIMode.DisplayIdleGui;
                    InputManager.Inst.StickyInput = true;
                    currTank.SetControllable(true);
                }
            }
        }

        void PruneDeadTanks()
        {
            for (int i = 0; i < Teams.Count; i++)
            {
                int tankCount = Teams.Values[i].Count;
                for (int j = 0; j < tankCount; j++)
                {
                    Tank currTank = Teams.Values[i].Dequeue();
                    if (!currTank.IsDead())
                        Teams.Values[i].Enqueue(currTank);
                }
            }
        }

        void CheckEndConditions()
        {
            if (Teams.Count == 0)
                gameEnded = true;

            int numTeamsAlive = 0;
            for (int i = 0; i < Teams.Count; i++)
            {
                if (Teams.Values[i].Count > 0)
                    numTeamsAlive++;
            }
            gameEnded = (numTeamsAlive <= 1 && Teams.Count > 1);
            if (gameEnded)
                timeRemaining = END_GAME_TIME;
        }

        public void BeginAdvancingRound()
        {
            if (currTank != null)
            {
                currTank.SetEnabled(false);
                currTank.SetControllable(false);
            }

            PruneDeadTanks();

            CheckEndConditions();

            if(!gameEnded)
            {
                timeRemaining = DELAY_NEXT_ROUND_TIME;
                advancingRound = true;
            }
        }

        public void AdvanceRound()
        {
            if (!gameEnded)
            {
                currTeam = (currTeam + 1) % Teams.Count;
                currTank = Teams.Values[currTeam].Dequeue();
                if (currTeam == Teams.IndexOfKey(playerTeamName)) //Set control if team == player team
                {
                    currTank.SetControllable(true);
                }
                //Set the camera to follow the tank
                currTank.SetEnabled(true);
                
                Teams.Values[currTeam].Enqueue(currTank);

                advancingRound = false;
                timeRemaining = ROUND_TIME;
            }
        }

        void DrawGameTime()
        {
            Vector2 timerPos = new Vector2(0, 0.85f);
            float time = Math.Max(timeRemaining, 0);

            string timerText = timeRemaining.ToString();
            int index = timerText.IndexOf('.');
            if(index >=0 && (index + 3) < timerText.Length)
                timerText = timerText.Substring(0, index + 3);
            float lerpAmt = (float)Math.Cos(timeRemaining*5.0f);
            Vector4 timerColor = (timeRemaining <= 5.0f) ? Vector4.Lerp(Vector4.One, new Vector4(1, 0, 0, 1), lerpAmt) : Vector4.One;
            GUITextElement timerElement = new GUITextElement(timerPos, timerText, timerColor);
            GFX.Inst.GetGUI().AddElement(timerElement);
        }

        float GetIdleAlpha()
        {
            return MathHelper.Clamp(elapsedIdleTime - SHOW_GUI_MIN_ALPHA_TIME, 0, SHOW_GUI_IDLE_TIME) / SHOW_GUI_IDLE_TIME;
        }

        float GetWeaponsAlpha()
        {
            return MathHelper.Clamp(elasedWeaponsVisibleTime / SHOW_WEAPONS_IDLE_TIME, 0.0f, 1.0f);
        }

        void DrawPowerBar()
        {
            float alpha = GetIdleAlpha();
            Vector2 minSize = new Vector2(-0.9f, -0.5f);
            Vector2 maxSize = new Vector2(-0.75f, 0.25f);
            Vector4 outlineColor = new Vector4(0.15f, 0.15f, 0.15f, alpha*0.5f);
            GUIElement powerBarOutline = new GUIElement(minSize, maxSize, null, outlineColor);
            GFX.Inst.GetGUI().AddElement(powerBarOutline);
        }

        void DrawHealth()
        {
            float alpha = GetIdleAlpha();
            const int maxHearts = 8;
            const float minHeartSize = 0.0725f;
            const float maxHeartSize = 0.1f;
            const float paddingSize = maxHeartSize + 0.065f;
            Vector2 origin = new Vector2(0.85f, -1);
            int numHearts = (int)(currTank.GetHealthPercent() * maxHearts);
            TextureResource heartImage = ResourceManager.Inst.GetTexture("Textures/Details/heart.png");
            GUIElementManager guiMgr = GFX.Inst.GetGUI();
            for (int i = 1; i <= numHearts; i++)
            {
                Vector2 center = origin + new Vector2(0, paddingSize * i);
                float offset = 7.256633f*MathHelper.TwoPi*((float)i/(float)maxHearts);
                float lerpAmount = (float)Math.Sin(offset+Time.RenderTime.TotalTime*2.25f) * 0.5f + 0.5f;
                float currSize = MathHelper.Lerp(minHeartSize, maxHeartSize, lerpAmount);
                GUIElement heart = new GUIElement(center - Vector2.One * currSize, center + Vector2.One * currSize, heartImage.GetTexture(), Vector4.One * alpha);
                guiMgr.AddElement(heart);
            }
        }

        void DrawGrenadeButton()
        {
            float alpha = GetIdleAlpha()*(1-GetWeaponsAlpha());

            const float hoverSizeOffset = 0.15f;
            Vector2 minSize = new Vector2(-0.8f, -0.8f);
            Vector2 maxSize = new Vector2(-0.5f, -0.5f);
            Vector2 mousePos = Input.InputManager.Inst.GetMousePositionHomogenous();
            if (minSize.X <= mousePos.X && mousePos.X <= maxSize.X
                && minSize.Y <= mousePos.Y && mousePos.Y <= maxSize.Y)
            {
                minSize -= Vector2.One * hoverSizeOffset;
                maxSize += Vector2.One * hoverSizeOffset;
                anyButtonClicked |= InputManager.Inst.IsLeftMouseDown();
                if (anyButtonClicked)
                    weaponScreenVisible = true;
            }
            TextureResource grenadeImage = ResourceManager.Inst.GetTexture("Textures/UI/grenade.dds");
            GUIElement grenadeButton = new GUIElement(minSize, maxSize, grenadeImage.GetTexture(), Vector4.One*alpha);
            GFX.Inst.GetGUI().AddElement(grenadeButton);
        }

        void DrawMapButton()
        {
            float alpha = GetIdleAlpha();

            const float hoverSizeOffset = 0.15f;
            Vector2 minSize = new Vector2(-0.1f, -0.8f);
            Vector2 maxSize = new Vector2(0.2f, -0.5f);
            Vector2 mousePos = Input.InputManager.Inst.GetMousePositionHomogenous();
            if (minSize.X <= mousePos.X && mousePos.X <= maxSize.X
                && minSize.Y <= mousePos.Y && mousePos.Y <= maxSize.Y)
            {
                minSize -= Vector2.One * hoverSizeOffset;
                maxSize += Vector2.One * hoverSizeOffset;
                anyButtonClicked |= InputManager.Inst.IsLeftMouseDown();
                elasedWeaponsVisibleTime = 0;
            }
            TextureResource grenadeImage = ResourceManager.Inst.GetTexture("Textures/UI/map.dds");
            GUIElement grenadeButton = new GUIElement(minSize, maxSize, grenadeImage.GetTexture(), Vector4.One * alpha);
            GFX.Inst.GetGUI().AddElement(grenadeButton);
        }

        void DrawInventoryFrame()
        {
            elasedWeaponsVisibleTime += Time.RenderTime.ElapsedTime;
            float alpha = GetWeaponsAlpha();
            Vector2 minSize = new Vector2(-1, -0.75f);
            Vector2 maxSize = new Vector2(-0.65f, 0.75f);
            maxSize.X = MathHelper.Lerp(-1, -0.65f, alpha);
            GUIElement frame = new GUIElement(minSize, maxSize, null, Vector3.One * 0.35f);
            GFX.Inst.GetGUI().AddElement(frame);
        }

        void DrawWeaponScreen()
        {
            DrawInventoryFrame();
        }

        void DrawCursor()
        {
            bool leftMouseDown = InputManager.Inst.IsLeftMouseDown();
            if (leftMouseDown && !anyButtonClicked)
                return;
            float alpha = GetIdleAlpha();
            Vector2 cursorPos = InputManager.Inst.GetMousePositionHomogenous();
            Vector2 cursorHalfSize = new Vector2(0.05f, 0.05f);
            TextureResource cursorImage = ResourceManager.Inst.GetTexture("Textures/UI/cursor.dds");
            GUIElement cursorButton = new GUIElement(cursorPos - cursorHalfSize, cursorPos + cursorHalfSize, cursorImage.GetTexture(), Vector4.One * alpha);
            GFX.Inst.GetGUI().AddElement(cursorButton);

        }

        void DrawIdleGui()
        {
            DrawHealth();
            
            if (weaponScreenVisible)
            {
                DrawWeaponScreen();
            }
            else
            {
                elasedWeaponsVisibleTime -= Time.RenderTime.ElapsedTime;
                DrawGrenadeButton();

                DrawMapButton();
            }

            DrawCursor();
        }

        void CreateCompassTexture()
        {
            compassTexture = new Texture2D(GFX.Device, compassTextureWidth, compassTextureHeight, 1, TextureUsage.None, SurfaceFormat.Color);
            Color[] colorData = new Color[compassTextureWidth * compassTextureHeight];
            int lineIndex = (int)(((float)compassTextureWidth * (float)compassTickDelta) / 360.0f);
            int lineThickness = 4;
            for (int y = 0; y < compassTextureHeight; y++)
            {
                for (int x = 0; x < compassTextureWidth; x++)
                {
                    int index = x % lineIndex;
                    colorData[x+y*compassTextureWidth] = (index < lineThickness && x < (compassTextureWidth-lineThickness*2)) ? Color.Black : Color.TransparentBlack;
                }
            }
            compassTexture.SetData<Color>(colorData);
        }

        void DrawCompass()
        {
            Vector2 min = new Vector2(-0.25f, 0.6f);
            Vector2 max = new Vector2(0.25f, 0.7f);
            
            float pivot = (float)compassTickDelta / 45.0f;
            Vector3 dir = Vector3.Zero;// currTank.GetCannonDirection();
            float theta = 0.5f + 0.5f*(float)Math.Atan2(dir.Z, dir.X)/MathHelper.Pi;

            Vector2 minTC = new Vector2(theta - pivot, 0);
            Vector2 maxTC = new Vector2(theta + pivot, 1);

            GUIElementTC compass = new GUIElementTC(min, max, compassTexture, Vector4.One, minTC, maxTC);
            GFX.Inst.GetGUI().AddElement(compass);

            for (int i = 0; i < compassDirs.Length; i++)
            {
                float currDirTC = compassTCs[i];
                if(currDirTC < minTC.X)
                    currDirTC++;
                if (minTC.X < currDirTC && currDirTC < maxTC.X)
                {
                    float lerpAmount = (currDirTC - minTC.X) / (maxTC.X - minTC.X);
                    Vector2 textPos = Vector2.Lerp(min, max, lerpAmount);
                    textPos.Y = (min.Y + max.Y) * 0.5f;
                    GUITextElement cardinalDirection = new GUITextElement(textPos, compassDirs[i], Vector4.One);
                    GFX.Inst.GetGUI().AddElement(cardinalDirection);
                }
            }

            const float offsetY = 0.045f;
            float offsetX = offsetY * GFX.Inst.DisplayRes.Y / GFX.Inst.DisplayRes.X;
            Vector2[] corners = { new Vector2(min.X-offsetX, min.Y), new Vector2(min.X, max.Y),
                                  new Vector2(max.X, min.Y), new Vector2(max.X+offsetX, max.Y),
                                  new Vector2(min.X-offsetX, min.Y-offsetY), new Vector2(max.X+offsetX, min.Y),
                                  new Vector2(min.X-offsetX, max.Y), new Vector2(max.X+offsetX, max.Y+offsetY)
                                };
            int quadCount = corners.Length / 2;
            for (int i = 0; i < quadCount; i++)
            {
                int index = i * 2;
                GUIElement box = new GUIElement(corners[index], corners[index + 1], null, Vector3.One * 0.15f);
                GFX.Inst.GetGUI().AddElement(box);
            }
        }

        void DrawMainGui()
        {
            DrawCompass();
        }

        public override void OnRender(Gaia.Rendering.RenderViews.RenderView view)
        {
            if (view.GetRenderType() != Gaia.Rendering.RenderViews.RenderViewType.MAIN)
                return;

            anyButtonClicked = false;

            if ((guiFlag & (int)RoundGUIMode.DisplayTime) > 0)
                DrawGameTime();
            if ((guiFlag & (int)RoundGUIMode.DisplayIdleGui) > 0)
                DrawIdleGui();
            if ((guiFlag & (int)RoundGUIMode.DisplayMainGui) > 0)
                DrawMainGui();
            base.OnRender(view);
        }

    }
}
