﻿﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

using Gaia.Resources;
using Gaia.Voxels;
using Gaia.Input;
using Gaia.Rendering;
using Gaia.Rendering.RenderViews;
using Gaia.SceneGraph.GameEntities;
using Gaia.Core;


namespace Gaia.SceneGraph.GameEntities
{
    public class Tree : Entity
    {
        List<RenderElement> Voxels;
        List<Material> treeMaterials;
        BoundingBox boundingBox;
        int varyTreeNum;

        public void setNum(int treeNum)
        {
            varyTreeNum = treeNum;
        }

        void generateTree(Vector3 position)
        {
            Lsystem lSys = new Lsystem();
            lSys.setAxiom("F");
            lSys.setIterations(3);
            lSys.setSphereRadius(1.0f);
            lSys.setTurnValue(45);
            lSys.setForwardLength(7.0f);
            lSys.setWidth(2.5f);

            Lsystem.ReproductionRule r1;
            r1.from = 'F';
         //   r1.to = "FF-[\\F+F&F@]+[/F-F%F@]";

            // Regular tree:
       //     r1.to = "GF[%-F@]%G[&+F@][&\\F@]&&G[%/F@]F@";  // With randomization
            r1.to = "GGGF[-F@]%G[+F@][\\F@]&&G[/F@]F@";  // With some randomization 

          //    r1.to = "G[/GF][+F][-GF][\\F]G@";
           //  r1.to = "G-%[[F]+&F]+&G[+&GF]-%F";
            //r1.to = "G[+%F][-&F]GFF@";
            lSys.addRule(r1);

            Lsystem.ReproductionRule r2;
            r2.from = 'G';
            r2.to = "G";
            //r2.to = "G[+<TTTT]G";
            lSys.addRule(r2);

            if (varyTreeNum == 2)
            {
                lSys.setSphereRadius(2.0f);
                lSys.setWidth(3.5f);
            }

            Voxels = lSys.generateGeometry(position, varyTreeNum);
            boundingBox = lSys.getBoundingBox();
        }

        public override void OnAdd(Scene scene)
        {

            treeMaterials = new List<Material>();
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LeafMat0"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LeafMat8"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LeafMat2"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LeafMat3"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LeafMat4"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LeafMat5"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LeafMat6"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LeafMat7"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LTreeMat0")); // Tree trunk
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LTreeMat1"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LTreeMat2"));
            treeMaterials.Add(ResourceManager.Inst.GetMaterial("LTreeMat3"));

            Vector3 randPosition = Vector3.Zero;
            Vector3 randNormal = Vector3.Zero;
            RandomHelper.RandomGen.NextDouble();
            scene.MainTerrain.GenerateRandomTransform(RandomHelper.RandomGen, out randPosition, out randNormal);
            while (randPosition.Y < 5.0f || Vector3.Dot(randNormal, Vector3.Up) < 0.5f)
            {
                scene.MainTerrain.GenerateRandomTransform(RandomHelper.RandomGen, out randPosition, out randNormal);
            }
            generateTree(randPosition);
            base.OnAdd(scene);
            
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public override void OnRender(RenderView view)
        {
            BoundingFrustum frustum = view.GetFrustum();
            if (frustum.Contains(boundingBox) != ContainmentType.Disjoint)
            {
                int barkNum = varyTreeNum / 2;

                view.AddElement(treeMaterials[8 + barkNum], Voxels[0]);
                view.AddElement(treeMaterials[varyTreeNum], Voxels[1]);
            }
            base.OnRender(view);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
        }
    }
}