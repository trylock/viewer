using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Properties;
using Viewer.Query;
using Viewer.UI.Explorer;
using Viewer.UI.Presentation;
using Viewer.UI.QueryEditor;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export(typeof(IComponent))]
    public class ImagesComponent : Component
    {
        private readonly IEditor _editor;
        private readonly IExplorer _explorer;
        private readonly IPresentation _presentation;
        private readonly IFileSystemErrorView _dialogView;
        private readonly ISelection _selection;
        private readonly IEntityManager _entityManager;
        private readonly IClipboardService _clipboard;
        private readonly IQueryHistory _queryHistory;
        private readonly IQueryFactory _queryFactory;
        private readonly IQueryCompiler _queryCompiler;
        private readonly IQueryEvaluatorFactory _queryEvaluatorFactory;
        
        private ImagesPresenter _presenter;

        private bool _dontShowImagesWindow = false;
        private IStatusBarSlider _thumbnailSize;
        private IStatusBarItem _statusLabel;
        private IStatusBarItem _itemCountLabel;
        private IStatusBarItem _selectionCountLabel;

        [ImportingConstructor]
        public ImagesComponent(
            IEditor editor,
            IExplorer explorer,
            IPresentation presentation,
            IFileSystemErrorView dialogView,
            ISelection selection,
            IEntityManager entityManager,
            IClipboardService clipboard,
            IQueryHistory state,
            IQueryFactory queryFactory,
            IQueryCompiler queryCompiler,
            IQueryEvaluatorFactory queryEvaluatorFactory)
        {
            _editor = editor;
            _explorer = explorer;
            _presentation = presentation;
            _dialogView = dialogView;
            _selection = selection;
            _entityManager = entityManager;
            _clipboard = clipboard;
            _queryHistory = state;
            _queryFactory = queryFactory;
            _queryCompiler = queryCompiler;
            _queryEvaluatorFactory = queryEvaluatorFactory;
        }

        private void SelectionOnChanged(object sender, EventArgs e)
        {
            var selectionCount = _selection.Count();
            _selectionCountLabel.Text = selectionCount > 0
                ? string.Format(Resources.SelectedItemCount_Label, selectionCount)
                : "";
        }

        private void StateOnQueryExecuted(object sender, QueryEventArgs e)
        {
            var images = GetImages();
            images.LoadQueryAsync(e.Query);

            if (!_dontShowImagesWindow)
            {
                images.View.Show(Application.Panel, DockState.Document);
            }
        }

        public override void OnStartup(IViewerApplication app)
        {
            app.AddLayoutDeserializeCallback(Deserialize);

            // add staus bar items
            _statusLabel = app.CreateStatusBarItem("Done.", Resources.SearchStatus, ToolStripItemAlignment.Left);
            _selectionCountLabel = app.CreateStatusBarItem("", null, ToolStripItemAlignment.Right);
            _itemCountLabel = app.CreateStatusBarItem("", null, ToolStripItemAlignment.Right);
            _thumbnailSize = app.CreateStatusBarSlider("", Resources.ThumbnailSize, ToolStripItemAlignment.Right);
            _thumbnailSize.Value = Settings.Default.ThumbnailSize;

            // register event handlers
            _thumbnailSize.ValueChanged += ThumbnailSizeOnValueChanged;
            _selection.Changed += SelectionOnChanged;
            _queryHistory.QueryExecuted += StateOnQueryExecuted;
        }
        
        private void ThumbnailSizeOnValueChanged(object sender, EventArgs e)
        {
            _presenter?.SetThumbnailSize(_thumbnailSize.Value);
            Settings.Default.ThumbnailSize = _thumbnailSize.Value;
        }

        private IWindowView Deserialize(string persistString)
        {
            if (persistString.StartsWith(typeof(ImagesView).FullName))
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
                    var query = _queryCompiler.Compile(new StringReader(queryText), new NullQueryErrorListener());
                    if (query == null)
                    {
                        query = _queryFactory.CreateQuery();
                    }
                    
                    var images = GetImages();
                    _dontShowImagesWindow = true;
                    try
                    {
                        _queryHistory.ExecuteQuery(query);
                    }
                    finally
                    {
                        _dontShowImagesWindow = false;
                    }

                    return images.View;
                }
            }
            return null;
        }

        private ImagesPresenter GetImages()
        {
            if (_presenter == null)
            {
                _presenter = new ImagesPresenter(new ImagesView(), 
                    _editor,
                    _explorer,
                    _presentation,
                    _dialogView,
                    _selection,
                    _entityManager,
                    _clipboard,
                    _queryHistory,
                    _queryFactory,
                    _queryEvaluatorFactory);
                _presenter.SetThumbnailSize(_thumbnailSize.Value);
                _presenter.StatusLabel = _statusLabel;
                _presenter.ItemCountLabel = _itemCountLabel;
                _presenter.View.CloseView += (sender, args) =>
                {
                    _presenter.Dispose();
                    _presenter = null;
                };
            }
            else
            {
                _presenter.View.EnsureVisible();
            }

            return _presenter;
        }
    }
}
