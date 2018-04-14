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
    public partial class ProgressView<T> : UserControl, IProgressView<T>
    {
        private class ProgressController : IProgress<T>
        {
            private Func<T, bool> _finishedPredicate;
            private Func<T, string> _taskNameGetter;
            private IProgressView<T> _view;

            public ProgressController(IProgressView<T> view, Func<T, bool> finishedPredicate, Func<T, string> taskNameGetter)
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
                    return;
                }

                var name = _taskNameGetter(value);
                if (name != null)
                {
                    _view.StartWork(name);
                }
            }
        }

        public ProgressView(Func<T, bool> finishPredicate, Func<T, string> taskNameGetter) 
        {
            InitializeComponent();

            Progress = new ProgressController(this, finishPredicate, taskNameGetter);

            CancelProgress += (sender, e) => _cancellation.Cancel();
        }

        #region View interface

        public event EventHandler CancelProgress;

        public CancellationToken CancellationToken => _cancellation.Token;

        public IProgress<T> Progress { get; }

        private CancellationTokenSource _cancellation = new CancellationTokenSource();
        private ReaderWriterLockSlim _closeLock = new ReaderWriterLockSlim();
        private bool _isFinished = false;
        
        public IProgressView<T> WithTitle(string title)
        {
            Text = title;
            return this;
        }

        public IProgressView<T> WithWork(int workUnitCount)
        {
            ProgressBar.Value = 0;
            ProgressBar.Maximum = workUnitCount;
            ProgressBar.Step = 1;
            return this;
        }

        public void Show(WorkDelegate<T> work)
        {
            work(this);
        }

        public void StartWork(string name)
        {
            CancellationToken.ThrowIfCancellationRequested();
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
            CancellationToken.ThrowIfCancellationRequested();
            _closeLock.EnterReadLock();
            try
            {
                if (_isFinished)
                {
                    return;
                }

                BeginInvoke(new Action(() =>
                {
                    ProgressBar.PerformStep();

                    var currentProgress = (int)((ProgressBar.Value / (double)ProgressBar.Maximum) * 100);
                    ProgressLabel.Text = string.Format(Resources.Progress_Label, currentProgress);
                    if (ProgressBar.Value >= ProgressBar.Maximum)
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
