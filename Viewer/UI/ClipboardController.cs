using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.IO;

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
            AddFilesToClipboard(new StringCollection { path }, DragDropEffects.Copy);
        }

        /// <summary>
        /// Cut a file to clipboard
        /// </summary>
        /// <param name="path">Path to a file or a folder</param>
        public void CutFile(string path)
        {
            AddFilesToClipboard(new StringCollection { path }, DragDropEffects.Move);
        }

        /// <summary>
        /// If there are files in the sytem clipboard, they will be copied/moved 
        /// to <paramref name="targetFolderPath"/>.
        /// </summary>
        /// <param name="targetFolderPath">
        ///     Path to a folder where the files in clipboard will be moved/copies
        /// </param>
        public void PasteFiles(string targetFolderPath)
        {
            if (!Clipboard.ContainsFileDropList())
                return;

            // check whether we should copy or move
            var effect = DragDropEffects.Copy;
            var effectData = (MemoryStream)Clipboard.GetData("Preferred DropEffect");
            if (effectData != null)
            {
                var reader = new BinaryReader(effectData);
                effect = (DragDropEffects)reader.ReadInt32();
            }
            
            // copy/move all files in the clipboard
            var files = Clipboard.GetFileDropList();
            foreach (var source in files)
            {
                var sepIndex = source.LastIndexOfAny(DirectoryController.DirectorySeparators);
                var target = Path.Combine(targetFolderPath, source.Substring(sepIndex + 1));

                if ((effect & DragDropEffects.Move) != 0)
                {
                    if (File.Exists(source))
                    {
                        File.Move(source, target);
                    }
                    else
                    {
                        Directory.Move(source, target);
                    }
                }
                else if ((effect & DragDropEffects.Copy) != 0)
                {
                    if (File.Exists(source))
                    {
                        File.Copy(source, target);
                    }
                    else
                    {
                        DirectoryUtils.Copy(source, target);
                    }
                }
            }
        }

        private void AddFilesToClipboard(StringCollection fileList, DragDropEffects effect)
        {
            Clipboard.Clear();
            
            var data = new DataObject();
            data.SetFileDropList(fileList);
            data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)effect)));
            Clipboard.SetDataObject(data, true);
        }
    }
}
