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
        private readonly IApplicationState _applicationEvents;
        private readonly IFileSystemErrorView _dialogErrorView;

        private ExportLifetimeContext<QueryPresenter> _query;

        [ImportingConstructor]
        public QueryComponent(
            ExportFactory<QueryPresenter> queryFactory, 
            IFileSystemErrorView dialogErrorView, 
            IApplicationState applicationEvents)
        {
            _queryFactory = queryFactory;
            _dialogErrorView = dialogErrorView;

            _applicationEvents = applicationEvents;
            _applicationEvents.FileOpened += OnFileOpened;
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddViewAction("Query", ShowQueryInput);

            ShowQueryInput();
        }

        private async void OnFileOpened(object sender, OpenFileEventArgs e)
        {
            var extension = Path.GetExtension(e.Path);
            if (StringComparer.InvariantCultureIgnoreCase.Compare(extension, ".vql") != 0)
            {
                // only open files which contain a query
                return;
            }
            
            var name = Path.GetFileName(e.Path);
            try
            {
                var query = await Task.Run(() => File.ReadAllText(e.Path));
                var queryInput = CreateQueryInput(name, query);
                queryInput.Value.FullPath = e.Path;
            }
            catch (FileNotFoundException)
            {
                _dialogErrorView.FileNotFound(e.Path);
            }
            catch (UnauthorizedAccessException)
            {
                _dialogErrorView.UnauthorizedAccess(e.Path);
            }
            catch (SecurityException)
            {
                _dialogErrorView.UnauthorizedAccess(e.Path);
            }
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
