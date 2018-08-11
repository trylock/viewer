using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core.UI;
using Viewer.Properties;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Tasks
{
    public partial class TaskLoaderView : WindowView
    {
        public IProgress<ILoadingProgress> Progress => _controller;

        /// <summary>
        /// Name of the task
        /// </summary>
        public string OperationName { get; set; }

        private readonly ProgressController _controller;
        private readonly CancellationTokenSource _cancellation;

        private class ProgressController : IProgress<ILoadingProgress>
        {
            /// <summary>
            /// Name of a task which is currently loading
            /// </summary>
            public string LoadingTaskName;

            /// <summary>
            /// Number of finished tasks
            /// </summary>
            public int FinishedCount;

            public void Report(ILoadingProgress value)
            {
                LoadingTaskName = value.Name;
                Interlocked.Increment(ref FinishedCount);
            }
        }

        public TaskLoaderView(int totalTaskCount, CancellationTokenSource cancellation)
        {
            InitializeComponent();
            
            _controller = new ProgressController();
            _cancellation = cancellation;

            TaskProgressBar.Minimum = 0;
            TaskProgressBar.Maximum = totalTaskCount;
        }

        private void PollTimer_Tick(object sender, EventArgs e)
        {
            // read current state
            var name = _controller.LoadingTaskName;
            var finishedCount = _controller.FinishedCount;
            if (finishedCount >= TaskProgressBar.Maximum)
            {
                Close();
            }

            // update the view
            var progress = (int) (TaskProgressBar.Value / (double) TaskProgressBar.Maximum * 100);
            TaskProgressBar.Value = finishedCount;
            TaskNameLabel.Text = name;
            ProgressLabel.Text = string.Format(Resources.Progress_Label, progress);

            // update the title
            Text = string.Format(Resources.Progess_Title, OperationName, progress);
        }

        private void TaskLoaderView_FormClosing(object sender, FormClosingEventArgs e)
        {
            _cancellation.Cancel();
        }

        private void CancelTaskButton_Click(object sender, EventArgs e)
        {
            _cancellation.Cancel();
            Close();
        }
    }
}
