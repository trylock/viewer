using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Properties;
using Viewer.Query;
using Viewer.Query.Properties;
using Viewer.UI.QueryEditor;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export(typeof(IComponent))]
    public class ImagesComponent : IComponent
    {
        private readonly IEditor _editor;
        private readonly IQueryEvents _state;
        private readonly IQueryFactory _queryFactory;
        private readonly IQueryCompiler _queryCompiler;
        private readonly ExportFactory<ImagesPresenter> _imagesFactory;

        private ExportLifetimeContext<ImagesPresenter> _images;

        private IToolBarItem _backTool;
        private IToolBarItem _forwardTool;
        private IToolBarItem _showCodeTool;
        private IToolBarItem _refreshTool;
        
        private IStatusBarSlider _thumbnailSize;

        [ImportingConstructor]
        public ImagesComponent(
            IEditor editor,
            IQueryEvents state, 
            IQueryFactory queryFactory, 
            IQueryCompiler queryCompiler,
            ExportFactory<ImagesPresenter> images)
        {
            _editor = editor;
            _imagesFactory = images;
            _queryFactory = queryFactory;
            _queryCompiler = queryCompiler;
            _state = state;
            _state.QueryExecuted += StateOnQueryExecuted;
        }

        private void StateOnQueryExecuted(object sender, QueryEventArgs e)
        {
            // show the thumbnail grid component
            ShowImages(e.Query);

            // update back and forward buttons
            if (_backTool == null || _forwardTool == null)
            {
                return;
            }
            _backTool.Enabled = _state.Previous != null;
            _backTool.ToolTipText = _state.Previous == null ? "Back" : $"Back to {_state.Previous.Text}";

            _forwardTool.Enabled = _state.Next != null;
            _forwardTool.ToolTipText = _state.Next == null ? "Forward" : $"Forward to {_state.Next.Text}";

            _showCodeTool.Enabled = _state.Current != null;
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddLayoutDeserializeCallback(Deserialize);

            // add staus bar items
            _thumbnailSize = app.CreateStatusBarSlider("", Resources.ThumbnailSize, ToolStripItemAlignment.Right);
            _thumbnailSize.ValueChanged += ThumbnailSizeOnValueChanged;

            // add tool bar items
            _backTool = app.CreateToolBarItem("navigation", "back", "Back", Resources.Back, _state.Back);
            _forwardTool = app.CreateToolBarItem("navigation", "forward", "Forward", Resources.Forward, _state.Forward);
            _showCodeTool = app.CreateToolBarItem("navigation", "code", "Show current query", Resources.ShowCodeIcon, ShowCurrentQueryCode);
            _refreshTool = app.CreateToolBarItem("navigation", "refresh", "Refresh", Resources.Refresh, RefreshCurrentQuery);

            _backTool.Enabled = _state.Previous != null;
            _forwardTool.Enabled = _state.Next != null;
        }

        /// <summary>
        /// Execute current query again.
        /// </summary>
        private void RefreshCurrentQuery()
        {
            if (_state.Current != null)
            {
                _state.ExecuteQuery(_state.Current);
            }
        }

        /// <summary>
        /// Show code of the current query in query editor.
        /// </summary>
        private void ShowCurrentQueryCode()
        {
            _editor.OpenNew(_state.Current?.Text, DockState.Document);
        }

        private void ThumbnailSizeOnValueChanged(object sender, EventArgs e)
        {
            _images?.Value.SetThumbnailSize(_thumbnailSize.Value);
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
                    // get the last query
                    var queryText = parts[1];
                    var query = _queryCompiler.Compile(new StringReader(queryText), new NullErrorListener());
                    if (query == null)
                    {
                        query = _queryFactory.CreateQuery();
                    }
                    
                    // create the images component
                    var images = GetImages();

                    // execute the query 
                    _state.ExecuteQuery(query);
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
