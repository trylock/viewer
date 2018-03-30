using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
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

        private Size _itemSize = new Size(150, 100);
        private List<AttributeCollection> _items = new List<AttributeCollection>();
        
        public QueryResultPresenter(IQueryResultView view, IAttributeStorage storage, IThumbnailGenerator thumbnailGenerator)
        {
            _thumbnailGenerator = thumbnailGenerator;
            _storage = storage;
            _view = view;
            _view.CloseView += View_Closed;
            _view.ExecuteShortcuts += View_ExecuteShortcuts;
            _view.SelectionChanged += View_SelectionChanged;
        }

        /// <summary>
        /// Load given directory as a query result
        /// </summary>
        /// <param name="fullPath">Full path to a directory</param>
        public void LoadDirectory(string fullPath)
        {
            // dispose old query
            foreach (var item in _items)
            {
                item.Dispose();
            }
            _items.Clear();

            // load new data
            foreach (var file in Directory.EnumerateFiles(fullPath))
            {
                var attrs = _storage.Load(file);
                _items.Add(attrs);
            }
            
            _storage.Flush();

            // update view
            _view.SetItemSize(_itemSize);
            _view.LoadItems(_items.Select(attrs => new ResultItemView(
                GetName(attrs), 
                attrs.Path, 
                GetThumbnail(attrs))));
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
        
        private void View_Closed(object sender, EventArgs eventArgs)
        {
            foreach (var item in _items)
            {
                item.Dispose();
            }
            _view = null;
        }

        private void View_SelectionChanged(object sender, SelectionEventArgs e)
        {
        }
    }
}
