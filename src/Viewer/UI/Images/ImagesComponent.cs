using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Query;
using Viewer.Query.Properties;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export(typeof(IComponent))]
    public class ImagesComponent : IComponent
    {
        private readonly IQueryEvents _state;
        private readonly IQueryFactory _queryFactory;
        private readonly IQueryCompiler _queryCompiler;
        private readonly ExportFactory<ImagesPresenter> _imagesFactory;

        private ExportLifetimeContext<ImagesPresenter> _images;

        [ImportingConstructor]
        public ImagesComponent(
            IQueryEvents state, 
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
            app.AddLayoutDeserializeCallback(Deserialize);
        }

        private IDockContent Deserialize(string persistString)
        {
            if (persistString.StartsWith(typeof(ImagesGridView).FullName))
            {
                var parts = persistString.Split(';');
                if (parts.Length == 1)
                {
                    var images = GetImages();
                    images.LoadQueryAsync(_queryFactory.CreateQuery());
                    return images.View;
                }
                else if (parts.Length == 2)
                {
                    var queryText = parts[1];
                    var query = _queryCompiler.Compile(new StringReader(queryText), new NullErrorListener());
                    if (query == null)
                    {
                        query = _queryFactory.CreateQuery();
                    }

                    var images = GetImages();
                    images.LoadQueryAsync(query);
                    return images.View;
                }
            }
            return null;
        }

        private ImagesPresenter GetImages()
        {
            if (_images == null)
            {
                _images = _imagesFactory.CreateExport();
                _images.Value.View.CloseView += (sender, args) =>
                {
                    _images.Dispose();
                    _images = null;
                };
            }
            else
            {
                _images.Value.View.EnsureVisible();
            }

            return _images.Value;
        }

        private IDockContent ShowImages(IQuery query)
        {
            var images = GetImages();
            images.LoadQueryAsync(query);
            images.ShowView("Images", DockState.Document);
            return images.View;
        }
    }
}
