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
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer
{
    [Export]
    public partial class ViewerForm : Form
    {
        public DockPanel Panel { get; }
        public EventHandler Shutdown;

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

        public void AddMenuItem(IReadOnlyList<string> path, Action action, Image icon)
        {
            var items = ViewerMenu.Items;
            foreach (var name in path)
            {
                // find menu item with this name
                var menuItem = items
                    .OfType<ToolStripMenuItem>()
                    .FirstOrDefault(item => StringComparer.CurrentCultureIgnoreCase.Compare(item.Text, name) == 0);

                // if it does not exist, create it
                if (menuItem == null)
                {
                    menuItem = new ToolStripMenuItem(name);
                    if (name == path[path.Count - 1])
                    {
                        menuItem.Image = icon;
                        menuItem.Click += (sender, e) => action();
                    }
                    items.Add(menuItem);
                }

                items = menuItem.DropDownItems;
            }
        }

        private void ViewerForm_Resize(object sender, EventArgs e)
        {
            Panel.Width = ClientSize.Width;
            Panel.Height = ClientSize.Height - ViewerMenu.Height;
            Panel.Location = new Point(0, ViewerMenu.Height);
        }

        private void ViewerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Shutdown?.Invoke(sender, e);
        }
    }
}
