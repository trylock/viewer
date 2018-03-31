using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;

namespace Viewer.UI.Images
{
    [Flags]
    public enum ResultItemState
    {
        None = 0x0,
        Active = 0x1,
        Selected = 0x2,
    }

    public class ResultItemView : IDisposable
    {
        /// <summary>
        /// Name of the file which should be shown to the user
        /// </summary>
        public string Name => Path.GetFileNameWithoutExtension(FullPath);

        /// <summary>
        /// Path to a file
        /// </summary>
        public string FullPath => _data.Path;

        /// <summary>
        /// Current state of the item
        /// </summary>
        public ResultItemState State { get; set; } = ResultItemState.None;

        /// <summary>
        /// Image representation of the file
        /// </summary>
        public Image Thumbnail { get; }

        private AttributeCollection _data;

        public ResultItemView(AttributeCollection data, Image thumbnail)
        {
            _data = data;
            Thumbnail = thumbnail;
        }

        public void Dispose()
        {
            Thumbnail?.Dispose();
        }
    }

    public interface IImagesView
    {
        event MouseEventHandler HandleMouseDown;
        event MouseEventHandler HandleMouseUp;
        event MouseEventHandler HandleMouseMove;
        event EventHandler Resize;
        event KeyEventHandler HandleKeyDown;
        event KeyEventHandler HandleKeyUp;

        Size ItemSize { get; set; }
        Size ItemPadding { get; set; }

        void UpdateSize();
        void LoadItems(IEnumerable<ResultItemView> items);
        void UpdateItems(IEnumerable<int> itemIndices);
        void UpdateItem(int index);
        void SetState(int index, ResultItemState state);
        void ShowSelection(Rectangle bounds);
        void HideSelection();
        void BeginDragDrop(IDataObject data, DragDropEffects effect);

        IEnumerable<int> GetItemsIn(Rectangle bounds);
        int GetItemAt(Point location);
    }
}
