using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.Properties;
using Viewer.Query;
using Viewer.UI;
using Viewer.UI.Attributes;
using Viewer.UI.Images;
using Viewer.UI.Explorer;
using Viewer.UI.Log;
using Viewer.UI.Presentation;
using Viewer.UI.Query;
using Viewer.UI.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer
{
    [Export]
    public partial class ViewerForm : Form
    {
        public DockPanel Panel { get; }
        
        public ViewerForm()
        {
            Panel = new DockPanel
            {
                Theme = new VS2015LightTheme()
            };
            Panel.UpdateDockWindowZOrder(DockStyle.Right, true);
            Panel.UpdateDockWindowZOrder(DockStyle.Left, true);
            Controls.Add(Panel);
            
            InitializeComponent();

            ViewerForm_Resize(this, EventArgs.Empty);
        }

        public void AddViewAction(string name, Action action)
        {
            var item = new ToolStripMenuItem { Text = name };
            item.Click += (sender, args) => action();
            ViewMenuItem.DropDownItems.Add(item);
        }

        private void ViewerForm_Resize(object sender, EventArgs e)
        {
            Panel.Width = ClientSize.Width;
            Panel.Height = ClientSize.Height - ViewerMenu.Height;
            Panel.Location = new Point(0, ViewerMenu.Height);
        }

        private void ViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            using (var state = new FileStream(Resources.LayoutFilePath, FileMode.Create, FileAccess.Write))
            {
                Panel.SaveAsXml(state, Encoding.UTF8);
            }
        }
    }
}
