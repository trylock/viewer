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
    public class QueryResultController
    {
        /// <summary>
        /// Size of the thumbnail control (i.e. size of the thumbnail + file name label)
        /// </summary>
        public Size ThumbnailSize { get; set; } = new Size(200, 140);
        
        /// <summary>
        /// Currently loaded attributes
        /// </summary>
        public IReadOnlyList<AttributeCollection> Result { get; private set; }

        public QueryResultController()
        {
            var factory = new AttributeStorageFactory();
            var storage = factory.Create();
            var result = new List<AttributeCollection>();
            foreach (var file in Directory.EnumerateFiles("C:/tmp"))
            {
                result.Add(storage.Load(file));
            }
            storage.Flush();

            Result = result;
        }

        /// <summary>
        /// Get name of given item
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Item name</returns>
        public string GetName(AttributeCollection item)
        {
            return Path.GetFileNameWithoutExtension(item.Path);
        }
    }
}
