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
using Viewer.UI;
using Viewer.UI.Attributes;
using Viewer.UI.Images;
using Viewer.UI.Explorer;
using Viewer.UI.Log;
using Viewer.UI.Presentation;
using Viewer.UI.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer
{
    [Export]
    public partial class ViewerForm : Form
    {
        public DockPanel Panel { get; private set; }

        public ViewerForm()
        {
            InitializeComponent();

            Panel = new DockPanel
            {
                Theme = new VS2015LightTheme(),
                Dock = DockStyle.Fill
            };
            Panel.UpdateDockWindowZOrder(DockStyle.Right, true);
            Panel.UpdateDockWindowZOrder(DockStyle.Left, true);
            Controls.Add(Panel);
        }
    }
}
