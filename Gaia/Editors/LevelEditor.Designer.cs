namespace Gaia.Editors
{
    partial class LevelEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newLevelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openLevelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveLevelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveLevelAsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.terrainToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.conformToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.selectedObjectToTerrainToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.terrainToSelectedObjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.undoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.redoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBoxScene = new System.Windows.Forms.GroupBox();
            this.groupBoxCreate = new System.Windows.Forms.GroupBox();
            this.groupBoxEntity = new System.Windows.Forms.GroupBox();
            this.listViewCreate = new System.Windows.Forms.ListView();
            this.listViewScene = new System.Windows.Forms.ListView();
            this.propertyGridScene = new System.Windows.Forms.PropertyGrid();
            this.menuStrip1.SuspendLayout();
            this.groupBoxScene.SuspendLayout();
            this.groupBoxCreate.SuspendLayout();
            this.groupBoxEntity.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.editToolStripMenuItem,
            this.terrainToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(614, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newLevelToolStripMenuItem,
            this.openLevelToolStripMenuItem,
            this.saveLevelToolStripMenuItem,
            this.saveLevelAsToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 24);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // newLevelToolStripMenuItem
            // 
            this.newLevelToolStripMenuItem.Name = "newLevelToolStripMenuItem";
            this.newLevelToolStripMenuItem.Size = new System.Drawing.Size(257, 24);
            this.newLevelToolStripMenuItem.Text = "New Level";
            this.newLevelToolStripMenuItem.Click += new System.EventHandler(this.newLevelToolStripMenuItem_Click);
            // 
            // openLevelToolStripMenuItem
            // 
            this.openLevelToolStripMenuItem.Name = "openLevelToolStripMenuItem";
            this.openLevelToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
            this.openLevelToolStripMenuItem.Size = new System.Drawing.Size(257, 24);
            this.openLevelToolStripMenuItem.Text = "Open Level";
            this.openLevelToolStripMenuItem.Click += new System.EventHandler(this.openLevelToolStripMenuItem_Click);
            // 
            // saveLevelToolStripMenuItem
            // 
            this.saveLevelToolStripMenuItem.Name = "saveLevelToolStripMenuItem";
            this.saveLevelToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.S)));
            this.saveLevelToolStripMenuItem.Size = new System.Drawing.Size(257, 24);
            this.saveLevelToolStripMenuItem.Text = "Save Level";
            this.saveLevelToolStripMenuItem.Click += new System.EventHandler(this.saveLevelToolStripMenuItem_Click);
            // 
            // saveLevelAsToolStripMenuItem
            // 
            this.saveLevelAsToolStripMenuItem.Name = "saveLevelAsToolStripMenuItem";
            this.saveLevelAsToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift)
                        | System.Windows.Forms.Keys.S)));
            this.saveLevelAsToolStripMenuItem.Size = new System.Drawing.Size(257, 24);
            this.saveLevelAsToolStripMenuItem.Text = "Save Level As";
            this.saveLevelAsToolStripMenuItem.Click += new System.EventHandler(this.saveLevelAsToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(167, 24);
            this.exitToolStripMenuItem.Text = "Exit";
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.undoToolStripMenuItem,
            this.redoToolStripMenuItem});
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(47, 24);
            this.editToolStripMenuItem.Text = "Edit";
            // 
            // terrainToolStripMenuItem
            // 
            this.terrainToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.conformToolStripMenuItem});
            this.terrainToolStripMenuItem.Name = "terrainToolStripMenuItem";
            this.terrainToolStripMenuItem.Size = new System.Drawing.Size(67, 24);
            this.terrainToolStripMenuItem.Text = "Terrain";
            // 
            // conformToolStripMenuItem
            // 
            this.conformToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.terrainToSelectedObjectToolStripMenuItem,
            this.selectedObjectToTerrainToolStripMenuItem});
            this.conformToolStripMenuItem.Name = "conformToolStripMenuItem";
            this.conformToolStripMenuItem.Size = new System.Drawing.Size(152, 24);
            this.conformToolStripMenuItem.Text = "Conform";
            // 
            // selectedObjectToTerrainToolStripMenuItem
            // 
            this.selectedObjectToTerrainToolStripMenuItem.Name = "selectedObjectToTerrainToolStripMenuItem";
            this.selectedObjectToTerrainToolStripMenuItem.Size = new System.Drawing.Size(251, 24);
            this.selectedObjectToTerrainToolStripMenuItem.Text = "Selected Object to Terrain";
            // 
            // terrainToSelectedObjectToolStripMenuItem
            // 
            this.terrainToSelectedObjectToolStripMenuItem.Name = "terrainToSelectedObjectToolStripMenuItem";
            this.terrainToSelectedObjectToolStripMenuItem.Size = new System.Drawing.Size(251, 24);
            this.terrainToSelectedObjectToolStripMenuItem.Text = "Terrain to Selected Object";
            // 
            // undoToolStripMenuItem
            // 
            this.undoToolStripMenuItem.Name = "undoToolStripMenuItem";
            this.undoToolStripMenuItem.Size = new System.Drawing.Size(152, 24);
            this.undoToolStripMenuItem.Text = "Undo";
            this.undoToolStripMenuItem.Click += new System.EventHandler(this.undoToolStripMenuItem_Click);
            // 
            // redoToolStripMenuItem
            // 
            this.redoToolStripMenuItem.Name = "redoToolStripMenuItem";
            this.redoToolStripMenuItem.Size = new System.Drawing.Size(152, 24);
            this.redoToolStripMenuItem.Text = "Redo";
            this.redoToolStripMenuItem.Click += new System.EventHandler(this.redoToolStripMenuItem_Click);
            // 
            // groupBoxScene
            // 
            this.groupBoxScene.Controls.Add(this.listViewScene);
            this.groupBoxScene.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBoxScene.Location = new System.Drawing.Point(0, 526);
            this.groupBoxScene.Name = "groupBoxScene";
            this.groupBoxScene.Size = new System.Drawing.Size(614, 213);
            this.groupBoxScene.TabIndex = 1;
            this.groupBoxScene.TabStop = false;
            this.groupBoxScene.Text = "Scene Hierarchy";
            // 
            // groupBoxCreate
            // 
            this.groupBoxCreate.Controls.Add(this.listViewCreate);
            this.groupBoxCreate.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBoxCreate.Location = new System.Drawing.Point(0, 28);
            this.groupBoxCreate.Name = "groupBoxCreate";
            this.groupBoxCreate.Size = new System.Drawing.Size(204, 498);
            this.groupBoxCreate.TabIndex = 2;
            this.groupBoxCreate.TabStop = false;
            this.groupBoxCreate.Text = "Creation Menu";
            // 
            // groupBoxEntity
            // 
            this.groupBoxEntity.Controls.Add(this.propertyGridScene);
            this.groupBoxEntity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxEntity.Location = new System.Drawing.Point(204, 28);
            this.groupBoxEntity.Name = "groupBoxEntity";
            this.groupBoxEntity.Size = new System.Drawing.Size(410, 498);
            this.groupBoxEntity.TabIndex = 3;
            this.groupBoxEntity.TabStop = false;
            this.groupBoxEntity.Text = "Parameters";
            // 
            // listViewCreate
            // 
            this.listViewCreate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewCreate.Location = new System.Drawing.Point(3, 18);
            this.listViewCreate.Name = "listViewCreate";
            this.listViewCreate.Size = new System.Drawing.Size(198, 477);
            this.listViewCreate.TabIndex = 0;
            this.listViewCreate.UseCompatibleStateImageBehavior = false;
            this.listViewCreate.View = System.Windows.Forms.View.List;
            // 
            // listViewScene
            // 
            this.listViewScene.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewScene.Location = new System.Drawing.Point(3, 18);
            this.listViewScene.Name = "listViewScene";
            this.listViewScene.Size = new System.Drawing.Size(608, 192);
            this.listViewScene.TabIndex = 0;
            this.listViewScene.UseCompatibleStateImageBehavior = false;
            this.listViewScene.View = System.Windows.Forms.View.List;
            this.listViewScene.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.listViewScene_ItemSelectionChanged);
            // 
            // propertyGridScene
            // 
            this.propertyGridScene.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGridScene.Location = new System.Drawing.Point(3, 18);
            this.propertyGridScene.Name = "propertyGridScene";
            this.propertyGridScene.PropertySort = System.Windows.Forms.PropertySort.NoSort;
            this.propertyGridScene.Size = new System.Drawing.Size(404, 477);
            this.propertyGridScene.TabIndex = 0;
            this.propertyGridScene.ToolbarVisible = false;
            // 
            // LevelEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(614, 739);
            this.ControlBox = false;
            this.Controls.Add(this.groupBoxEntity);
            this.Controls.Add(this.groupBoxCreate);
            this.Controls.Add(this.groupBoxScene);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "LevelEditor";
            this.Text = "Level Editor";
            this.Deactivate += new System.EventHandler(this.LevelEditor_Deactivate);
            this.Activated += new System.EventHandler(this.LevelEditor_Activated);
            this.Enter += new System.EventHandler(this.LevelEditor_Enter);
            this.Leave += new System.EventHandler(this.LevelEditor_Leave);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.groupBoxScene.ResumeLayout(false);
            this.groupBoxCreate.ResumeLayout(false);
            this.groupBoxEntity.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newLevelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openLevelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveLevelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveLevelAsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem terrainToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem conformToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem selectedObjectToTerrainToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem terrainToSelectedObjectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem undoToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem redoToolStripMenuItem;
        private System.Windows.Forms.GroupBox groupBoxScene;
        private System.Windows.Forms.GroupBox groupBoxCreate;
        private System.Windows.Forms.GroupBox groupBoxEntity;
        private System.Windows.Forms.ListView listViewCreate;
        private System.Windows.Forms.ListView listViewScene;
        private System.Windows.Forms.PropertyGrid propertyGridScene;
    }
}