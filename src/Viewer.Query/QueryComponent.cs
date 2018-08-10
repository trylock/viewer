using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.IO;

namespace Viewer.Query
{
    [Export(typeof(IComponent))]
    public class QueryComponent : IComponent
    {
        private readonly IQueryViewRepository _queryViews;
        private readonly IFileSystem _fileSystem;

        [ImportingConstructor]
        public QueryComponent(IQueryViewRepository queryViews, IFileSystem fileSystem)
        {
            _queryViews = queryViews;
            _fileSystem = fileSystem;
        }

        public void OnStartup(IViewerApplication app)
        {
            LoadQueryViews("./views");
        }

        private void LoadQueryViews(string directoryPath)
        {
            try
            {
                var fullDirectoryPath = Path.GetFullPath(directoryPath);
                foreach (var file in _fileSystem.EnumerateFiles(fullDirectoryPath, "*.vql"))
                {
                    LoadQueryView(file);
                }
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        private void LoadQueryView(string filePath)
        {
            try
            {
                var contents = _fileSystem.ReadAllText(filePath);
                var name = Path.GetFileNameWithoutExtension(filePath);
                _queryViews.Add(new QueryView(name, contents, filePath));
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (FileNotFoundException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (IOException)
            {
            }
        }
    }
}
