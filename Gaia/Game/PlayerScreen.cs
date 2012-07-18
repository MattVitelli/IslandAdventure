using System;
using System.Collections.Generic;

using Gaia.UI;
using Gaia.SceneGraph;
using Gaia.Core;
using Gaia.Resources;
using Gaia.Input;

using Microsoft.Xna.Framework;

using JigLibX.Collision;
using JigLibX.Geometry;

namespace Gaia.Game
{
    public class PlayerScreen : UIScreen
    {
        UICompass compass;
        UIButton scoreLabel;
        UIButton journalStatus;
        UIButton interactButton;
        UIButton interactStatus;
        UIButton crosshair;
        UIList list;

        Scene scene;
        bool addedMarker = false;

        const float interactDist = 3.5f;
        const float journalFadeInTime = 1.5f;
        const float journalFadeOutTime = 2.0f;
        float journalFadeTime = 0;
        bool journalEntryAdded = false;

        InteractSkinPredicate pred = new InteractSkinPredicate();

        Transform playerTransform;

        public PlayerScreen(Scene scene) : base()
        {
            this.scene = scene;
            compass = new UICompass();
            compass.Scale = new Vector2(0.35f, 0.05f);
            compass.Position = new Vector2(0, -0.85f);

            crosshair = new UIButton(ResourceManager.Inst.GetTexture("UI/Game/crossHair.dds"), Vector4.One, string.Empty);
            crosshair.Position = Vector2.Zero;
            crosshair.Scale = Vector2.One * 0.07f;

            scoreLabel = new UIButton(null, Vector4.One, "Score:");
            scoreLabel.Position = new Vector2(0.7f, 0.85f);
           
            journalStatus = new UIButton(null, Vector4.One, "Journal Updated!");
            journalStatus.Position = new Vector2(-0.7f, 0.85f);
            journalStatus.SetVisible(false);

            interactStatus = new UIButton(null, Vector4.One, "Examine Object");
            interactStatus.Position = new Vector2(-0.7f, -0.85f);
            interactStatus.SetVisible(false);

            interactButton = new UIButton(ResourceManager.Inst.GetTexture("UI/Game/interact.dds"), Vector4.One, string.Empty);
            interactButton.Position = crosshair.Position;
            interactButton.Scale = crosshair.Scale * 1.15f;
            interactButton.SetVisible(false);

            list = new UIList();
            list.ItemColor = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);
            list.Position = new Vector2(0.5f, 0);
            list.Scale = new Vector2(0.25f, 0.5f);
            string[] testStrings = new string[] 
            { 
                "Hello", "A Carton of Cigarettes", "Oh boy", "I hope this works", "25 or 6 to 4", "Rawr", "Christ", 
                "El Captain", "John Wayne", "Walter White", "Stanford Cow", "Dr. Evil", "Tom Hanks", "Lindsey Lohan",
                "Raptor Jesus", "Timothy Leary", "Bauldur", "Frank Lloyd Wright", "Pink Floyd", "The Beatles", "JoCo",
                "Is this twenty yet??",
            };
            list.Items.AddRange(testStrings);
            list.SetColor(new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            //this.controls.Add(list);

            this.controls.Add(crosshair);
            this.controls.Add(journalStatus);
            this.controls.Add(compass);
            this.controls.Add(interactStatus);
            this.controls.Add(interactButton);
            this.controls.Add(scoreLabel);
        }

        void PerformInteraction()
        {
            Vector3 ray = Vector3.Zero;
            float dist;
            CollisionSkin skin = null;
            Vector3 pos, normal;
 
            Vector3 origin = playerTransform.GetPosition();
            Vector3 lookDir = playerTransform.GetTransform().Forward;
            Segment seg = new Segment(origin, lookDir * interactDist);
             
            scene.GetPhysicsEngine().CollisionSystem.SegmentIntersect(out dist, out skin, out pos, out normal, seg, pred);
            if (skin != null)
            {
                InteractBody body = (InteractBody)skin.Owner;
                InteractNode node = body.Node;
                //Do collision detection at a later date!
                //skin.Owner
                interactStatus.SetText(node.GetInteractText());

                crosshair.SetVisible(false);
                interactButton.SetVisible(true);
                interactStatus.SetVisible(true);

                if (InputManager.Inst.IsKeyDownOnce(GameKey.Interact))
                {
                    node.OnInteract();
                }
            }
            else
            {
                crosshair.SetVisible(true);
                interactButton.SetVisible(false);
                interactStatus.SetVisible(false);
            }
        }

        void DisplayJournalStatus(float timeDT)
        {
            if (journalFadeTime < (journalFadeInTime + journalFadeOutTime))
            {
                journalFadeTime += timeDT;

                float alpha = (journalFadeTime <= journalFadeInTime) ? (journalFadeTime / journalFadeInTime) : (1.0f - (journalFadeTime - journalFadeInTime) / journalFadeOutTime);

                journalStatus.SetVisible(true);
                journalStatus.SetTextColor(new Vector4(1, 1, 1, alpha));
            }
            else
            {
                journalStatus.SetVisible(false);
                journalEntryAdded = false;
            }
        }

        public override void OnUpdate(float timeDT)
        {
            Entity camera = scene.FindEntity("MainCamera");
            if (camera != null)
            {
                playerTransform = camera.Transformation;
                compass.SetTransformation(playerTransform);
                PerformInteraction();
            }

            Entity testEnt = scene.FindEntity("scene_geom2");
            if (testEnt != null && !addedMarker)
            {
                compass.AddMarker(testEnt.Transformation);
                addedMarker = true;
            }

            if (Input.InputManager.Inst.IsKeyDown(Gaia.Input.GameKey.Interact))
            {
                journalFadeTime = 0;
                journalEntryAdded = true;
            }

            if(journalEntryAdded)
                DisplayJournalStatus(timeDT);
            
            base.OnUpdate(timeDT);
        }
    }
}
