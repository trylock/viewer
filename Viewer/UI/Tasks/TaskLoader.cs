using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Tasks
{
    [Export(typeof(ITaskLoader))]
    public class TaskLoader : ITaskLoader
    {
        private readonly ViewerForm _appForm;

        [ImportingConstructor]
        public TaskLoader(ViewerForm form)
        {
            _appForm = form;
        }

        public IProgress<ILoadingProgress> CreateLoader(string name, int totalTaskCount, CancellationTokenSource cancellation)
        {
            var view = new TaskLoaderView(totalTaskCount, cancellation);
            view.Show(_appForm.Panel, DockState.DockBottom);
            return view.Progress;
        }
    }
}
