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
using Viewer.Properties;

namespace Viewer.UI.Tasks
{
    public partial class ProgressView : UserControl, IProgressView
    {
        public ProgressView()
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

            work(this);
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

                    var currentProgress = (int)((Progress.Value / (double) Progress.Maximum) * 100);
                    ProgressLabel.Text = string.Format(Resources.Progress_Label, currentProgress);
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

        private class ProgressController<T> : IProgress<T>
        {
            private Func<T, bool> _finishedPredicate;
            private Func<T, string> _taskNameGetter;
            private IProgressView _view;

            public ProgressController(IProgressView view, Func<T, bool> finishedPredicate, Func<T, string> taskNameGetter)
            {
                _view = view;
                _finishedPredicate = finishedPredicate;
                _taskNameGetter = taskNameGetter;
            }

            public void Report(T value)
            {
                if (_finishedPredicate(value))
                {
                    _view.FinishWork();
                }

                var name = _taskNameGetter(value);
                if (name != null)
                {
                    _view.StartWork(name);
                }
            }
        }

        public IProgress<T> CreateProgress<T>(Func<T, bool> finishedPredicate, Func<T, string> taskNameGetter)
        {
            return new ProgressController<T>(this, finishedPredicate, taskNameGetter);
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

                BeginInvoke(new Action(() =>
                {
                    // remove the control from its parent
                    Parent = null;
                }));
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
    }
}
