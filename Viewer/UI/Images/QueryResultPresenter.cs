using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;

namespace Viewer.UI.Images
{
    public class QueryResultPresenter
    {
        private IQueryResultView _view;
        private IAttributeStorage _storage;
        private IThumbnailGenerator _thumbnailGenerator;

        private Size _itemSize = new Size(100, 100);
        
        public QueryResultPresenter(IQueryResultView view, IAttributeStorage storage, IThumbnailGenerator thumbnailGenerator)
        {
            _thumbnailGenerator = thumbnailGenerator;
            _storage = storage;
            _view = view;
            _view.CloseView += OnCloseView;
            _view.HandleShortcuts += OnHandleShortcuts;
        }
        
        /// <summary>
        /// Load given directory as a query result
        /// </summary>
        /// <param name="fullPath">Full path to a directory</param>
        public void LoadDirectory(string fullPath)
        {
            // update view
            _view.ItemSize = _itemSize;
            _view.Items = (
                from file in Directory.EnumerateFiles(fullPath)
                let attrs = _storage.Load(file)
                select new ResultItem(GetName(attrs), GetThumbnail(attrs))
            ).ToList();

            _storage.Flush();
        }
        
        private void OnHandleShortcuts(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                _view.AddToSelection(Enumerable.Range(0, _view.Items.Count));
            }
        }

        private void OnCloseView(object sender, EventArgs eventArgs)
        {
            _view = null;
        }

        private Image GetThumbnail(AttributeCollection item)
        {
            var image = ((ImageAttribute)item["thumbnail"]).Value;
            try
            {
                return _thumbnailGenerator.GetThumbnail(image, _itemSize);
            }
            finally
            {
                image.Dispose();
            }
        }

        private string GetName(AttributeCollection item)
        {
            return Path.GetFileNameWithoutExtension(item.Path);
        }
    }
}
