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
        private readonly ExportFactory<QueryPresenter> _queryFactory;

        private ExportLifetimeContext<QueryPresenter> _query;

        [ImportingConstructor]
        public QueryComponent(
            ExportFactory<QueryPresenter> queryFactory, 
            IFileSystemErrorView dialogErrorView, 
            IApplicationState applicationEvents)
        {
            _queryFactory = queryFactory;
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddViewAction("Query", ShowQueryInput);

            ShowQueryInput();
        }
        
        private ExportLifetimeContext<QueryPresenter> CreateQueryInput(string name, string query)
        {
            var queryInput = _queryFactory.CreateExport();
            queryInput.Value.ShowView(name, DockState.Document);
            queryInput.Value.View.Query = query;
            queryInput.Value.View.CloseView += (sender, args) => queryInput.Dispose();
            return queryInput;
        }

        private void ShowQueryInput()
        {
            if (_query == null)
            {
                _query = CreateQueryInput("Query", "");
                _query.Value.View.CloseView += (sender, args) => _query = null;
            }
            else
            {
                _query.Value.View.EnsureVisible();
            }
        }
    }
}
