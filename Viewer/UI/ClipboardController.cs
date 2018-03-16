using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI
{
    public class ClipboardController
    {
        /// <summary>
        /// Copy a file to clipboard
        /// </summary>
        /// <param name="path">Path to a file or a folder</param>
        public void CopyFile(string path)
        {
            AddFilesToClipboard(new[] { path }, DragDropEffects.Copy);
        }

        /// <summary>
        /// Cut a file to clipboard
        /// </summary>
        /// <param name="path">Path to a file or a folder</param>
        public void CutFile(string path)
        {
            AddFilesToClipboard(new[] { path }, DragDropEffects.Move);
        }

        private void AddFilesToClipboard(IEnumerable<string> paths, DragDropEffects effect)
        {
            Clipboard.Clear();

            var fileList = new StringCollection();
            foreach (var path in paths)
            {
                fileList.Add(path);
            }

            var data = new DataObject();
            data.SetFileDropList(fileList);
            data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)effect)));
            Clipboard.SetDataObject(data, true);
        }
    }
}
