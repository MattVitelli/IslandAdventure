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
    public class HuntScreen : UIScreen
    {
        UIList mapList;
        UIList weaponList;
        UIList dinosaurList;
        UIList inventoryList;
        UIButton costLabel;
        UIButton huntButton;

        Vector4 goldColor = new Vector4(0.8863f, 0.8551f, 0.7304f, 1.0f);
        Vector2 listScale = new Vector2(0.20f, 0.45f);
        const float listHeight = -0.25f;

        public HuntScreen()
            : base()
        {
            CreateMapList();
            CreateWeaponList();
            CreateDinosaurList();
            CreateInventoryList();
            CreateCostLabel();
            CreateHuntButton();
        }

        void CreateMapList()
        {
            mapList = new UIList();
            mapList.ItemColor = goldColor;
            mapList.Position = new Vector2(-0.75f, listHeight);
            mapList.Scale = listScale;
            string[] testStrings = new string[] 
            { 
                "Hello", "A Carton of Cigarettes", "Oh boy", "I hope this works", "25 or 6 to 4", "Rawr", "Christ", 
                "El Captain", "John Wayne", "Walter White", "Stanford Cow", "Dr. Evil", "Tom Hanks", "Lindsey Lohan",
                "Raptor Jesus", "Timothy Leary", "Bauldur", "Frank Lloyd Wright", "Pink Floyd", "The Beatles", "JoCo",
                "Is this twenty yet??",
            };
            mapList.Items.AddRange(testStrings);
            mapList.SetColor(new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            this.controls.Add(mapList);
        }

        void CreateWeaponList()
        {
            weaponList = new UIList();
            weaponList.ItemColor = goldColor;
            weaponList.Position = new Vector2(0.25f, listHeight);
            weaponList.Scale = listScale;
            string[] testStrings = new string[] 
            { 
                "Tactical Knife", "Pistol", "Shotgun", "SMG", "Rifle", "Sniper Rifle"
            };
            weaponList.Items.AddRange(testStrings);
            weaponList.SetColor(new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            this.controls.Add(weaponList);
        }

        void CreateDinosaurList()
        {
            dinosaurList = new UIList();
            dinosaurList.ItemColor = goldColor;
            dinosaurList.Position = new Vector2(-0.25f, listHeight);
            dinosaurList.Scale = listScale;
            string[] testStrings = new string[] 
            { 
                "Allosaurus", "Velociraptor", "T-Rex", "Triceratops",
            };
            dinosaurList.Items.AddRange(testStrings);
            dinosaurList.SetColor(new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            this.controls.Add(dinosaurList);
        }

        void CreateInventoryList()
        {
            inventoryList = new UIList();
            inventoryList.ItemColor = goldColor;
            inventoryList.Position = new Vector2(0.75f, listHeight);
            inventoryList.Scale = listScale;
            string[] testStrings = new string[] 
            { 
                "GPS", "Double Ammo", "Medicine Pack", "Scent Stopper", "Flashlight"
            };
            inventoryList.Items.AddRange(testStrings);
            inventoryList.SetColor(new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            this.controls.Add(inventoryList);
        }

        void CreateCostLabel()
        {
            costLabel = new UIButton(null, Vector4.One, "$16000");
            costLabel.Position = new Vector2(0, 0.85f);
            costLabel.Scale = new Vector2(0.15f, 0.15f);
            costLabel.SetTextColor(goldColor);
            this.controls.Add(costLabel);
        }

        void CreateHuntButton()
        {
            huntButton = new UIButton(null, goldColor, "Hunt!");
            huntButton.Position = new Vector2(0.8f, -0.85f);
            huntButton.Scale = new Vector2(0.2f, 0.15f);
            this.controls.Add(huntButton);
        }

        public override void OnUpdate(float timeDT)
        {
            InputManager.Inst.StickyInput = false;
            base.OnUpdate(timeDT);
        }

        public override void OnRender()
        {
            base.OnRender();
        }
    }
}
