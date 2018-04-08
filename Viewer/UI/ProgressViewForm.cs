using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
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

            CancelProgress += (sender, e) => _cancellation.Cancel();
        }

        #region View interface

        public event EventHandler CancelProgress;

        public CancellationToken CancellationToken => _cancellation.Token;
        
        private CancellationTokenSource _cancellation = new CancellationTokenSource();
        private ReaderWriterLockSlim _closeLock = new ReaderWriterLockSlim();
        private bool _isFinished = false;

        public void Show(string name, int maximum, WorkDelegate work)
        {
            Text = name;
            Progress.Value = 0;
            Progress.Maximum = maximum;
            Progress.Step = 1;
            _work = work;
            Show();
        }

        public void StartWork(string name)
        {
            _closeLock.EnterReadLock();
            try
            {
                if (_isFinished)
                {
                    return;
                }

                BeginInvoke(new Action(() =>
                {
                    CurrentTaskNameLabel.Text = name;
                }));
            }
            finally
            {
                _closeLock.ExitReadLock();
            }
        }

        public void FinishWork()
        {
            _closeLock.EnterReadLock();
            try
            {
                if (_isFinished)
                {
                    return;
                }

                BeginInvoke(new Action(() =>
                {
                    Progress.PerformStep();
                    if (Progress.Value >= Progress.Maximum)
                    {
                        CloseView();
                    }
                }));
            }
            finally
            {
                _closeLock.ExitReadLock();
            }
        }

        public void CloseView(bool canceled = false)
        {
            _closeLock.EnterWriteLock();
            try
            {
                _isFinished = true;
                if (canceled)
                {
                    CancelProgress?.Invoke(this, EventArgs.Empty);
                }

                Hide();
            }
            finally
            {
                _closeLock.ExitWriteLock();
            }
        }

        #endregion

        private void CancelProgressButton_Click(object sender, EventArgs e)
        {
            CloseView(true);
        }

        private void ProgressViewForm_Shown(object sender, EventArgs e)
        {
            _work?.Invoke(this);
        }
        
        private void ProgressViewForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                CloseView(true);
                e.Cancel = true;
            }
        }
    }
}
