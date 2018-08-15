using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MemoryStream = System.IO.MemoryStream;

namespace Viewer.Core.UI
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a list of files in system clipboard.
    /// It also caries preferred drag/drop <see cref="P:Viewer.Core.UI.ClipboardFileDrop.Effect" />.
    /// </summary>
    public class ClipboardFileDrop : IEnumerable<string>
    {
        /// <summary>
        /// Preferred drag/drop effect (e.g. copy or move)
        /// </summary>
        public DragDropEffects Effect { get; }

        private readonly IEnumerable<string> _paths;

        /// <summary>
        /// Create empty clipboard file drop. <see cref="Effect"/> will be <see cref="DragDropEffects.Copy"/>
        /// </summary>
        public ClipboardFileDrop()
        {
            Effect = DragDropEffects.Copy;
            _paths = Enumerable.Empty<string>();
        }

        /// <summary>
        /// Create a new clipboard file drop with <paramref name="filePaths"/> as files and
        /// <paramref name="effect"/> as a preferred drop effect.
        /// </summary>
        /// <param name="filePaths">Files added to the clipboard</param>
        /// <param name="effect">Preferred drag/drop effect</param>
        public ClipboardFileDrop(IEnumerable<string> filePaths, DragDropEffects effect)
        {
            _paths = filePaths ?? throw new ArgumentNullException(nameof(filePaths));
            Effect = effect;
        }
        
        public IEnumerator<string> GetEnumerator()
        {
            return _paths.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public interface IClipboardService
    {
        /// <summary>
        /// Get files in the clipboard
        /// </summary>
        /// <returns>List of files in the clipboard with preferred drop effect</returns>
        ClipboardFileDrop GetFiles();

        /// <summary>
        /// Replace content of system clipboard with <paramref name="files"/> as a FileDrop
        /// and string (list of file paths).
        /// </summary>
        /// <param name="files">Paths to files which will be added to the clipboard with preferred drag/drop effect</param>
        void SetFiles(ClipboardFileDrop files);
    }
    
    [Export(typeof(IClipboardService))]
    public class ClipboardService : IClipboardService
    {
        public ClipboardFileDrop GetFiles()
        {
            var data = Clipboard.GetDataObject();
            if (data == null)
            {
                return new ClipboardFileDrop();
            }
            var files = data.GetData(DataFormats.FileDrop, true) as string[];
            var effect = DragDropEffects.Copy;
            var effectData = data.GetData("Preferred DropEffect");
            if (effectData is MemoryStream effectStream)
            {
                var reader = new BinaryReader(effectStream);
                effect = (DragDropEffects) reader.ReadInt32();
            }
            return new ClipboardFileDrop(files, effect);
        }

        public void SetFiles(ClipboardFileDrop files)
        {
            var fileList = files.ToArray();
            var data = new DataObject(DataFormats.FileDrop, fileList);
            data.SetText(string.Join(", ", fileList));
            data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)files.Effect)));

            Clipboard.Clear();
            Clipboard.SetDataObject(data);
        }
    }
}
