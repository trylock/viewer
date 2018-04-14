using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI.Tasks
{

    public class ProgressViewFactory : IProgressViewFactory
    {
        private Control _parentView;

        public ProgressViewFactory(Control parent)
        {
            _parentView = parent;
        }

        public IProgressView<T> Create<T>(Func<T, bool> finishPredicate, Func<T, string> taskNameGetter)
        {
            var view = new ProgressView<T>(finishPredicate, taskNameGetter);
            _parentView.Controls.Add(view);
            return view;
        }
    }
}
