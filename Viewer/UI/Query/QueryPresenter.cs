using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Query;

namespace Viewer.UI.Query
{
    [Export]
    public class QueryPresenter : Presenter<IQueryView>
    {
        private readonly IApplicationState _appEvents;
        private readonly IQueryCompiler _queryCompiler;
        private readonly IErrorListener _queryErrorListener;

        protected override ExportLifetimeContext<IQueryView> ViewLifetime { get; }

        [ImportingConstructor]
        public QueryPresenter(ExportFactory<IQueryView> viewFactory, IApplicationState appEvents, IQueryCompiler queryCompiler, IErrorListener queryErrorListener)
        {
            ViewLifetime = viewFactory.CreateExport();
            _appEvents = appEvents;
            _queryCompiler = queryCompiler;
            _queryErrorListener = queryErrorListener;

            SubscribeTo(View, "View");
        }

        private void View_RunQuery(object sender, EventArgs e)
        {
            var query = _queryCompiler.Compile(new StringReader(View.Query), _queryErrorListener);
            if (query != null)
            {
                _appEvents.ExecuteQuery(query);
            }
        }
    }
}
