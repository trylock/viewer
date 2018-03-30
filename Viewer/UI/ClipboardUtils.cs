using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI
{
    public static class ClipboardUtils
    {
        /// <summary>
        /// Try to find a preferred drop effect (copy or move) for the data in clipboard.
        /// If we can't retrieve the preffered drop effect (e.g. this platform does not support it),
        /// copy effect is assumed and returned.
        /// </summary>
        /// <returns>Preferred drop effect for data in clipboard</returns>
        public static DragDropEffects GetPreferredEffect()
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
