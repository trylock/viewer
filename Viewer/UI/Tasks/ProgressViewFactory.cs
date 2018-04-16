using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Tasks
{
    [Export(typeof(IProgressViewFactory))]
    public class ProgressViewFactory : IProgressViewFactory
    {
        private readonly TasksView _parentView;

        [ImportingConstructor]
        public ProgressViewFactory(TasksView view)
        {
            _parentView = view;
        }
        
        public IProgressView<T> Create<T>(Func<T, bool> finishPredicate, Func<T, string> taskNameGetter)
        {
            var view = new ProgressView<T>(finishPredicate, taskNameGetter);
            _parentView.Controls.Add(view);
            _parentView.Show();
            return view;
        }
    }
}
