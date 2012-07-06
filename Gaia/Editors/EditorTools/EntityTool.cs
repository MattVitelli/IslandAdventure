using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

using Gaia.SceneGraph;

namespace Gaia.Editors.EditorTools
{
    public class EntityTool
    {
        protected Entity target;
        protected string name;
        protected Scene scene;
        protected LevelEditor editor;

        public virtual void Initialize(LevelEditor editor, string name) 
        {
            this.editor = editor;
            this.scene = editor.GetScene();
            this.name = name;
            this.target = scene.FindEntity(name);
        }

        public Vector3 Position
        {
            get { return target.Transformation.GetPosition(); }
            set { target.Transformation.SetPosition(value); }
        }

        public Vector3 Rotation
        {
            get { return target.Transformation.GetRotation(); }
            set { target.Transformation.SetRotation(value); }
        }

        public Vector3 Scale
        {
            get { return target.Transformation.GetScale(); }
            set { target.Transformation.SetScale(value); }
        }

        public string Name
        {
            get { return name; }
            set { name = scene.RenameEntity(name, value); editor.InitializeSceneMenu(); }
        }
    }
}
