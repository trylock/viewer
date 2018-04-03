using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Attributes
{
    public class AttributesPresenter
    {
        private IAttributeView _attrView;
        private ISelection _selection;

        public AttributesPresenter(IAttributeView attrView, ISelection selection)
        {
            _attrView = attrView;
            _selection = selection;
            _selection.Changed += Selection_Changed;
        }

        private void Selection_Changed(object sender, EventArgs eventArgs)
        {
        }
    }
}
