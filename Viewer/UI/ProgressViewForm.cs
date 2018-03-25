using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI
{
    public partial class ProgressViewForm : Form, IProgressView
    {
        private WorkDelegate _work;

        public ProgressViewForm()
        {
            InitializeComponent();
        }

        #region View interface

        public event EventHandler CancelProgress;

        public void Show(string name, int maximum, WorkDelegate work)
        {
            Text = name;
            Progress.Value = 0;
            Progress.Maximum = maximum;
            Progress.Step = 1;
            _work = work;
            ShowDialog();
        }

        public void StartWork(string name)
        {
            CurrentTaskNameLabel.Text = name;
            Progress.PerformStep();
            Application.DoEvents();
        }

        public void Finish()
        {
            Hide();
        }

        #endregion

        private void CancelProgressButton_Click(object sender, EventArgs e)
        {
            CancelProgress?.Invoke(sender, e);
        }

        private void ProgressViewForm_Shown(object sender, EventArgs e)
        {
            _work?.Invoke();
        }
    }
}
