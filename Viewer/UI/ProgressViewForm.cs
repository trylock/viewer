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
        private bool _isFinished;

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
            _isFinished = true;
            Hide();
        }

        #endregion

        private void CancelProgressButton_Click(object sender, EventArgs e)
        {
            Hide();
        }

        private void ProgressViewForm_Shown(object sender, EventArgs e)
        {
            try
            {
                _work?.Invoke();
            }
            finally
            {
                _work = null;
            }
        }

        private void ProgressViewForm_VisibleChanged(object sender, EventArgs e)
        {
            if (!Visible && !_isFinished)
            {
                CancelProgress?.Invoke(sender, e);
            }

            if (!Visible)
            {
                CancelProgress = null;
                _work = null;
            }
        }

        private void ProgressViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
