using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;

using Gaia.SceneGraph;
using Gaia.SceneGraph.GameEntities;
using Gaia.Editors.EditorTools;

namespace Gaia.Editors
{
    public partial class LevelEditor : Form
    {
        Stack<ICommand> UndoStack = new Stack<ICommand>();
        Stack<ICommand> RedoStack = new Stack<ICommand>();
        SortedList<string, Type> typeToolDatabase = new SortedList<string, Type>();
        Scene scene;
        string levelFileName = string.Empty;
        bool anyChange = false;

        const string levelFileFilter = "Level Files (*.lvl)|*.lvl;|All Files (*.*)|*.*";

        public LevelEditor(Scene scene)
        {
            InitializeComponent();
            InitializeCreationMenu();
            InitializeTypeToolDatabase();
            this.scene = scene;
            Reset();
        }

        Type[] GetEntityTypes()
        {
            Assembly currAssembly = Assembly.GetExecutingAssembly();
            Type[] types = currAssembly.GetTypes();
            List<Type> entityTypes = new List<Type>();
            Type entityType = typeof(Entity);
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].BaseType == entityType)
                    entityTypes.Add(types[i]);
            }

            return entityTypes.ToArray();
        }

        void InitializeCreationMenu()
        {
            Type[] entityTypes = GetEntityTypes();
            for(int i = 0 ; i < entityTypes.Length; i++)
            {
                ListViewItem item = new ListViewItem(entityTypes[i].Name);
                item.Tag = entityTypes[i];
                listViewCreate.Items.Add(item);
            }
        }

        void InitializeTypeToolDatabase()
        {
            typeToolDatabase.Add(typeof(Light).FullName, typeof(LightTool)); 
        }

        public Scene GetScene()
        {
            return scene;
        }

        public void InitializeSceneMenu()
        {
            listViewScene.Clear();
            for (int i = 0; i < scene.Entities.Count; i++)
            {
                string currKey = scene.Entities.Keys[i];
                ListViewItem item = new ListViewItem(currKey);
                item.Tag = scene.Entities[currKey];
                listViewScene.Items.Add(item);
            }
        }

        void Reset()
        {
            levelFileName = string.Empty;
            UndoStack.Clear();
            RedoStack.Clear();
            InitializeSceneMenu();
        }

        public void PerformCommand(ICommand command)
        {
            command.Execute();
            UndoStack.Push(command);
        }

        void Undo()
        {
            if (UndoStack.Count < 1)
                return;

            ICommand command = UndoStack.Pop();
            command.Unexecute();
            RedoStack.Push(command);
        }

        void Redo()
        {
            if (RedoStack.Count < 1)
                return;
            ICommand command = RedoStack.Pop();
            command.Execute();
            UndoStack.Push(command);
        }

        
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Undo();
        }

        private void redoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Redo();
        }

        private void newLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        void SaveLevel()
        {
            if (levelFileName == string.Empty)
                return;

            using (XmlTextWriter writer = new XmlTextWriter(levelFileName, Encoding.UTF8))
            {
                scene.SaveScene(writer);
            }
        }

        void ChangeLevelFileName(bool saving)
        {
            FileDialog dlg = null;
            if (saving)
                dlg = new SaveFileDialog();
            else
                dlg = new OpenFileDialog();
            dlg.Title = ((saving) ? "Save" : "Open") + " Level";
            dlg.Filter = levelFileFilter;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                levelFileName = dlg.FileName;
            }
        }

        private void saveLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (levelFileName == string.Empty)
                ChangeLevelFileName(true);

            SaveLevel();
        }       

        private void saveLevelAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangeLevelFileName(true);
            SaveLevel();
        }

        private void listViewScene_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (listViewScene.SelectedItems.Count > 0)
            {
                ListViewItem item = listViewScene.SelectedItems[0];
                Entity ent = (Entity)item.Tag;
                EntityTool tool;
                string typeName = ent.GetType().FullName;
                if(typeToolDatabase.ContainsKey(typeName))
                {
                    tool = (EntityTool)typeToolDatabase[typeName].GetConstructors()[0].Invoke(null);
                }
                else
                    tool = new EntityTool();
                tool.Initialize(this, item.Text);
                propertyGridScene.SelectedObject = tool;
            }
        }


        private void LevelEditor_Enter(object sender, EventArgs e)
        {
            Input.InputManager.Inst.StickyInput = false;
        }

        private void LevelEditor_Activated(object sender, EventArgs e)
        {
            Input.InputManager.Inst.StickyInput = false;
        }

        private void LevelEditor_Deactivate(object sender, EventArgs e)
        {
            Input.InputManager.Inst.StickyInput = true;
        }

        private void LevelEditor_Leave(object sender, EventArgs e)
        {
            Input.InputManager.Inst.StickyInput = true;
        }

        private void openLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            levelFileName = string.Empty;
            ChangeLevelFileName(false);
            if (levelFileName != string.Empty)
            {
                scene.LoadScene(levelFileName);
                InitializeSceneMenu();
            }
        }
    }
}
