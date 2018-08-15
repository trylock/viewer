using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Core;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Tasks
{
    [Export(typeof(ITaskLoader))]
    public class TaskLoader : ITaskLoader
    {
        private readonly IViewerApplication _app;

        [ImportingConstructor]
        public TaskLoader(IViewerApplication app)
        {
            _app = app;
        }

        public IProgressController CreateLoader(string name, int totalTaskCount, CancellationTokenSource cancellation)
        {
            var view = new TaskLoaderView(totalTaskCount, cancellation);
            view.Show(_app.Panel, DockState.DockBottom);
            view.OperationName = name;
            view.Text = name;
            return view.Progress;
        }
    }
}
