using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI
{
    public class QueryResultPresenter
    {
        private IQueryResultView _view;
        private IAttributeStorage _storage;

        public QueryResultPresenter(IQueryResultView view, IAttributeStorage storage)
        {
            _view = view;
            _storage = storage;
        }

        /// <summary>
        /// Load given directory as a query result
        /// </summary>
        /// <param name="fullPath">Full path to a directory</param>
        public void LoadDirectory(string fullPath)
        {
            // load files
            var result = new List<AttributeCollection>();
            foreach (var file in Directory.EnumerateFiles(fullPath))
            {
                result.Add(_storage.Load(file));
            }
            _storage.Flush();

            // update view
            _view.ItemSize = new Size(100, 100);
            _view.Items = (
                from r in result
                select new ResultItem(GetName(r), GetThumbnail(r))
            ).ToList();
        }

        private Image GetThumbnail(AttributeCollection item)
        {
            return ((ImageAttribute)item["thumbnail"]).Value;
        }

        private string GetName(AttributeCollection item)
        {
            return Path.GetFileNameWithoutExtension(item.Path);
        }
    }
}
