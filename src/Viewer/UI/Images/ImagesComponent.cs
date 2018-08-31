﻿using System;
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
using Viewer.UI.QueryEditor;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export(typeof(IComponent))]
    public class ImagesComponent : IComponent
    {
        private readonly IQueryHistory _state;
        private readonly ISelection _selection;
        private readonly IQueryFactory _queryFactory;
        private readonly IQueryCompiler _queryCompiler;
        private readonly ExportFactory<ImagesPresenter> _imagesFactory;

        private ExportLifetimeContext<ImagesPresenter> _images;

        private bool _dontShowImagesWindow = false;
        private IStatusBarSlider _thumbnailSize;
        private IStatusBarItem _statusLabel;
        private IStatusBarItem _itemCountLabel;
        private IStatusBarItem _selectionCountLabel;

        [ImportingConstructor]
        public ImagesComponent(
            IQueryHistory state, 
            ISelection selection,
            IQueryFactory queryFactory, 
            IQueryCompiler queryCompiler,
            ExportFactory<ImagesPresenter> images)
        {
            _imagesFactory = images;
            _queryFactory = queryFactory;
            _queryCompiler = queryCompiler;
            _state = state;
            _selection = selection;
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
                images.ShowView("Images", DockState.Document);
            }
        }

        public void OnStartup(IViewerApplication app)
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
            _state.QueryExecuted += StateOnQueryExecuted;
        }
        
        private void ThumbnailSizeOnValueChanged(object sender, EventArgs e)
        {
            _images?.Value.SetThumbnailSize(_thumbnailSize.Value);
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
                        _state.ExecuteQuery(query);
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
            if (_images == null)
            {
                _images = _imagesFactory.CreateExport();
                _images.Value.SetThumbnailSize(_thumbnailSize.Value);
                _images.Value.StatusLabel = _statusLabel;
                _images.Value.ItemCountLabel = _itemCountLabel;
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
    }
}
