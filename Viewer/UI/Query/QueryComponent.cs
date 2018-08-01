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
            app.AddViewAction("Query", () => _editor.OpenNew());
        }

        public IDockContent Deserialize(string persistString)
        {
            if (persistString.StartsWith(typeof(QueryView).FullName))
            {
                var parts = persistString.Split(';');
                var content = parts.Length >= 2 ? parts[1] : "";
                var path = parts.Length >= 3 ? parts[2] : "";

                if (content.Length ==  0 && path.Length == 0)
                {
                    return _editor.OpenNew();
                }
                else if (path.Length == 0)
                {
                    var window = _editor.OpenNew();
                    window.Query = content;
                    return window;
                }
                else 
                {
                    return _editor.Open(path);
                }
            }
            return null;
        }
    }
}
