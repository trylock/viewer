using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Query;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export(typeof(IComponent))]
    public class ImagesComponent : IComponent
    {
        private readonly IApplicationState _state;
        private readonly IQueryFactory _queryFactory;
        private readonly IQueryCompiler _queryCompiler;
        private readonly ExportFactory<ImagesPresenter> _imagesFactory;

        private ExportLifetimeContext<ImagesPresenter> _images;

        [ImportingConstructor]
        public ImagesComponent(
            IApplicationState state, 
            IQueryFactory queryFactory, 
            IQueryCompiler queryCompiler,
            ExportFactory<ImagesPresenter> images)
        {
            _imagesFactory = images;
            _queryFactory = queryFactory;
            _queryCompiler = queryCompiler;
            _state = state;
            _state.QueryExecuted += (sender, e) => ShowImages(e.Query);
        }

        public void OnStartup(IViewerApplication app)
        {
        }

        public IDockContent Deserialize(string persistString)
        {
            if (persistString.StartsWith(typeof(ImagesGridView).FullName))
            {
                var parts = persistString.Split(';');
                if (parts.Length == 1)
                {
                    return ShowImages(_queryFactory.CreateQuery());
                }
                else if (parts.Length == 2)
                {
                    var queryText = parts[1];
                    var query = _queryCompiler.Compile(new StringReader(queryText), new NullErrorListener());
                    if (query == null)
                    {
                        return ShowImages(_queryFactory.CreateQuery());
                    }
                    return ShowImages(query);
                }
            }
            return null;
        }

        private IDockContent ShowImages(IQuery query)
        {
            if (_images == null)
            {
                _images = _imagesFactory.CreateExport();
                _images.Value.View.CloseView += (sender, args) =>
                {
                    _images.Dispose();
                    _images = null;
                };
                _images.Value.ShowView("Images", DockState.Document);
                _images.Value.LoadQueryAsync(query);
            }
            else
            {
                _images.Value.View.EnsureVisible();
                _images.Value.LoadQueryAsync(query);
            }

            return _images.Value.View;
        }
    }
}
