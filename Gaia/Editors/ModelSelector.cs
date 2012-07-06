using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Gaia.Resources;

namespace Gaia.Editors
{
    public partial class ModelSelector : Form
    {
        public ModelSelector(Type type)
        {
            InitializeComponent();
            listViewModels.Clear();
            if(type == typeof(Shader))
            {

            }
        }

        IResource selectedItem;

        public IResource SelectedItem
        {
            get { return selectedItem; }
            set { selectedItem = value; }
        }

        private void listViewModels_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewModels.SelectedItems.Count > 0)
            {
                selectedItem = (IResource)listViewModels.SelectedItems[0].Tag;
            }
        }
    }
}
