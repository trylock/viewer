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
    public class TaskLoader : Component, ITaskLoader
    {
        public IProgressController CreateLoader(string name, CancellationTokenSource cancellation)
        {
            var view = new TaskLoaderView( cancellation);
            view.Show(Application.Panel, DockState.DockBottom);
            view.OperationName = name;
            view.Text = name;
            return view.Progress;
        }
    }
}
