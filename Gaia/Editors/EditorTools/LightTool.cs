using System;
using System.Collections.Generic;

using Gaia.SceneGraph.GameEntities;
using Microsoft.Xna.Framework;

namespace Gaia.Editors.EditorTools
{
    
    public class LightTool : EntityTool
    {
        Light light;

        public override void  Initialize(LevelEditor editor, string name)
        {
 	         base.Initialize(editor, name);
             light = (Light)target;
        }

        public Vector3 Position
        {
            get { return light.Transformation.GetPosition(); }
            set { light.Transformation.SetPosition(Vector3.Normalize(value)); }
        }

        public bool CastsShadows
        {
            get { return light.CastsShadows; }
            set { light.CastsShadows = value; }
        }

        public Vector3 Color
        {
            get { return light.Color; }
            set { light.Color = value; }
        }

        public Vector4 Parameters
        {
            get { return light.Parameters; }
            set { light.Parameters = value; }
        }

        public LightType Type
        {
            get { return light.Type; }
            set { light.Type = value; }
        }
    }
    
}
