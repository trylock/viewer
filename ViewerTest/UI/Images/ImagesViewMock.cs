using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.UI.Images;

namespace ViewerTest.UI.Images
{
    public class ItemMock
    {
        public bool IsUpdated { get; set; }
        public ResultItemState State { get; set; }
    }

    public class ImagesViewMock : IImagesView
    {
        public event EventHandler CloseView;
        public event MouseEventHandler HandleMouseDown;
        public event MouseEventHandler HandleMouseUp;
        public event MouseEventHandler HandleMouseMove;
        public event EventHandler Resize;
        public event KeyEventHandler HandleKeyDown;
        public event KeyEventHandler HandleKeyUp;

        public Size ItemSize { get; set; } = new Size(1, 1);
        public Size ItemPadding { get; set; } = new Size(0, 0);
        public List<ItemMock> Items { get; } = new List<ItemMock>();
        public Rectangle CurrentSelection { get; private set; } = Rectangle.Empty;
        public Size ViewSize { get; } = new Size(8, 8);
        public bool IsActive { get; private set; } = false;

        // mock interface
        public ImagesViewMock(int width, int height)
        {
            ViewSize = new Size(width, height);
            for (int i = 0; i < ViewSize.Width / 2; ++i)
            {
                for (int j = 0; j < ViewSize.Height / 2; ++j)
                {
                    Items.Add(new ItemMock());
                }
            }
        }

        public void ResetMock()
        {
            foreach (var item in Items)
            {
                item.State = ResultItemState.None;
                item.IsUpdated = false;
            }
        }

        public void TriggerMouseDown(MouseEventArgs args)
        {
            HandleMouseDown?.Invoke(this, args);
        }

        public void TriggerMouseUp(MouseEventArgs args)
        {
            HandleMouseUp?.Invoke(this, args);
        }

        public void TriggerMouseMove(MouseEventArgs args)
        {
            HandleMouseMove?.Invoke(this, args);
        }

        public void TriggerKeyDown(KeyEventArgs args)
        {
            HandleKeyDown?.Invoke(this, args);
        }

        // mocked interface
        public void UpdateSize()
        {
        }

        public void LoadItems(IEnumerable<ResultItemView> items)
        {
            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(new ItemMock());
            }
        }

        public void UpdateItems(IEnumerable<int> itemIndices)
        {
            foreach (var item in itemIndices)
            {
                UpdateItem(item);
            }
        }

        public void UpdateItem(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;
            Items[index].IsUpdated = true;
        }

        public void SetState(int index, ResultItemState state)
        {
            if (index < 0 || index >= Items.Count)
                return;
            Items[index].State = state;
        }

        public void ShowSelection(Rectangle bounds)
        {
            CurrentSelection = bounds;
        }

        public void HideSelection()
        {
            CurrentSelection = Rectangle.Empty;
        }

        public IEnumerable<int> GetItemsIn(Rectangle bounds)
        {
            if (bounds.Width == 0 && bounds.Height == 0)
                yield break;

            for (int x = bounds.X; x <= bounds.X + bounds.Width; ++x)
            {
                if (x % 2 != 0)
                    continue;
                for (int y = bounds.Y; y <= bounds.Y + bounds.Height; ++y)
                {
                    if (y % 2 != 0)
                        continue;
                    var locX = x / 2;
                    var locY = y / 2;
                    yield return locY * (ViewSize.Width / 2) + locX;
                }
            }
        }

        public int GetItemAt(Point location)
        {
            if (location.X < 0 || location.Y < 0 ||
                location.X >= ViewSize.Width || location.Y >= ViewSize.Height)
                return -1;
            if (location.X % 2 != 0 || location.Y % 2 != 0)
                return -1;
            return (location.Y / 2) * (ViewSize.Width / 2) + (location.X / 2);
        }

        public void BeginDragDrop(IDataObject data, DragDropEffects effect)
        {
            throw new NotImplementedException();
        }

        public void MakeActive()
        {
            IsActive = true;
        }
    }
}
