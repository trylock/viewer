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

        public IProgressView Create()
        {
            var view = new ProgressView();
            _parentView.Controls.Add(view);
            return view;
        }
    }
}
