using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Viewer.UI.Explorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Query
{
    [Export(typeof(IComponent))]
    public class QueryComponent : IComponent
    {
        private readonly IEditor _editor;

        [ImportingConstructor]
        public QueryComponent(IEditor editor)
        {
            _editor = editor;
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddViewAction("Query", _editor.OpenNew);

            _editor.OpenNew();
        }
    }
}
