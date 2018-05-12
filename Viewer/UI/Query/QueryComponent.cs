using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Query
{
    [Export(typeof(IComponent))]
    public class QueryComponent : IComponent
    {
        private readonly ExportFactory<QueryPresenter> _queryFactory;

        private ExportLifetimeContext<QueryPresenter> _query;

        [ImportingConstructor]
        public QueryComponent(ExportFactory<QueryPresenter> queryFactory)
        {
            _queryFactory = queryFactory;
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddViewAction("Query", ShowQueryInput);

            ShowQueryInput();
        }

        private void ShowQueryInput()
        {
            if (_query == null)
            {
                _query = _queryFactory.CreateExport();
                _query.Value.View.CloseView += (sender, args) =>
                {
                    _query.Dispose();
                    _query = null;
                };
                _query.Value.ShowView("Query", DockState.Document);
            }
            else
            {
                _query.Value.View.EnsureVisible();
            }
        }
    }
}
