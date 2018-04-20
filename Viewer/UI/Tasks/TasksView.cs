using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Tasks
{
    public partial class TasksView : WindowView
    {
        public TasksView(string name)
        {
            InitializeComponent();

            Text = name;
        }

        private void TasksView_Resize(object sender, EventArgs e)
        {
            SuspendLayout();
            try
            {
                foreach (Control control in Controls)
                {
                    control.Width = ClientSize.Width;
                }
            }
            finally
            {
                ResumeLayout(false);
            }
        }
    }
}
