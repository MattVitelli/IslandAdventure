namespace Gaia.Editors
{
    partial class ModelSelector
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
            this.listViewModels = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // listViewModels
            // 
            this.listViewModels.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listViewModels.Location = new System.Drawing.Point(0, 0);
            this.listViewModels.Name = "listViewModels";
            this.listViewModels.Size = new System.Drawing.Size(386, 457);
            this.listViewModels.TabIndex = 0;
            this.listViewModels.UseCompatibleStateImageBehavior = false;
            this.listViewModels.SelectedIndexChanged += new System.EventHandler(this.listViewModels_SelectedIndexChanged);
            // 
            // ModelSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(386, 457);
            this.ControlBox = false;
            this.Controls.Add(this.listViewModels);
            this.Name = "ModelSelector";
            this.Text = "Select A Model";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listViewModels;
    }
}