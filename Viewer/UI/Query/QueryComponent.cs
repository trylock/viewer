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
            if (persistString == typeof(QueryView).FullName)
            {
                return _editor.OpenNew();
            }
            else if (persistString.StartsWith(typeof(QueryView).FullName))
            {
                var parts = persistString.Split(';');
                if (parts.Length < 2)
                {
                    return null;
                }

                var path = parts[1];
                return _editor.OpenAsync(path).Result;
            }
            return null;
        }
    }
}
