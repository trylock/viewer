using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI
{
    public interface IClipboardService
    {
        /// <summary>
        /// Get files in the clipboard
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetFiles();

        /// <summary>
        /// Remove contents of the clipboard and add files.
        /// </summary>
        /// <param name="files">Paths to files to add to the clipboard</param>
        void SetFiles(IEnumerable<string> files);

        /// <summary>
        /// Set preferred drop effect (e.g. copy/move)
        /// This won't have any effect if there is no data in the clipboard.
        /// </summary>
        /// <param name="effect">Preferred effect</param>
        void SetPreferredEffect(DragDropEffects effect);

        /// <summary>
        /// Get current preferred effect.
        /// </summary>
        /// <returns>
        ///     Preferred effect of drop of files in clipboard.
        ///     If the preferred effect is not found, it will be Copy.
        /// </returns>
        DragDropEffects GetPreferredEffect();
    }
    
    [Export(typeof(IClipboardService))]
    public class ClipboardService : IClipboardService
    {
        public IEnumerable<string> GetFiles()
        {
            var data = Clipboard.GetDataObject();
            if (data == null)
            {
                return Enumerable.Empty<string>();
            }
            return data.GetData(DataFormats.FileDrop, true) as string[];
        }

        public void SetFiles(IEnumerable<string> files)
        {
            var data = new DataObject(DataFormats.FileDrop, files.ToArray());

            Clipboard.Clear();
            Clipboard.SetDataObject(data);
        }

        public void SetPreferredEffect(DragDropEffects effect)
        {
            var data = Clipboard.GetDataObject();
            data?.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)effect)));
        }

        public DragDropEffects GetPreferredEffect()
        {
            var effect = DragDropEffects.Copy;
            var effectData = (MemoryStream)Clipboard.GetData("Preferred DropEffect");
            if (effectData != null)
            {
                var reader = new BinaryReader(effectData);
                effect = (DragDropEffects)reader.ReadInt32();
            }

            return effect;
        }
    }
}
