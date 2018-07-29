using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Properties;

namespace Viewer.UI
{
    public interface ISystemExplorer
    {
        /// <summary>
        /// Open folder which contains file/folder at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Path to a file/folder</param>
        void OpenFile(string path);
    }

    [Export(typeof(ISystemExplorer))]
    public class SystemExplorer : ISystemExplorer
    {
        public void OpenFile(string path)
        {
            var fullPath = Path.GetFullPath(path);
            Process.Start(
                Resources.ExplorerProcessName,
                string.Format(Resources.ExplorerOpenFolderArguments, fullPath)
            );
        }
    }
}
