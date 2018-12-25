using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Localization;
using Viewer.Properties;

namespace Viewer.UI.About
{
    internal partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();

            VersionLabel.Text = string.Format(Strings.Version_Label, Assembly.GetExecutingAssembly().GetName().Version);
        }

        private void Icon8Link_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://icons8.com");
        }
    }
}
