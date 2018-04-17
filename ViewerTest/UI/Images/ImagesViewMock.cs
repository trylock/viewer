using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.UI.Images;
using WeifenLuo.WinFormsUI.Docking;

namespace ViewerTest.UI.Images
{
    /// <summary>
    /// Images view mock.
    /// This mock models the images view as a grid.
    /// Each position in even row AND even column is a cell.
    /// Each position in odd row OR odd column is an empty space.
    /// </summary>
    public class ImagesViewMock : IImagesView
    {
        public event EventHandler CloseView;
        public event EventHandler ViewGotFocus;
        public event MouseEventHandler HandleMouseDown;
        public event MouseEventHandler HandleMouseUp;
        public event MouseEventHandler HandleMouseMove;
        public event EventHandler Resize;
        public event KeyEventHandler HandleKeyDown;
        public event KeyEventHandler HandleKeyUp;
        public event EventHandler BeginEditItemName;
        public event EventHandler CancelEditItemName;
        public event EventHandler<RenameEventArgs> RenameItem;
        public event EventHandler CopyItems;
        public event EventHandler DeleteItems;
        public event EventHandler OpenItem;
        public event EventHandler ThumbnailSizeChanged;
        public int ThumbnailSize { get; set; }
        public int ThumbnailSizeMinimum { get; set; }
        public int ThumbnailSizeMaximum { get; set; }

        public Size ItemSize { get; set; } = new Size(1, 1);
        public Size ItemPadding { get; set; } = new Size(0, 0);
        public List<EntityView> Items { get; set; } = new List<EntityView>();
        public Rectangle CurrentSelection { get; private set; } = Rectangle.Empty;
        public Size ViewSize { get; } = new Size(8, 8);
        public bool IsActive { get; private set; } = false;
        public int EditIndex { get; private set; } = -1;
        public ISet<int> UpdatedItems { get; } = new HashSet<int>();

        // mock interface
        public ImagesViewMock(int width, int height)
        {
            ViewSize = new Size(width, height);
            for (int i = 0; i < ViewSize.Width / 2; ++i)
            {
                for (int j = 0; j < ViewSize.Height / 2; ++j)
                {
                    Items.Add(new EntityView(null, null));
                }
            }
        }

        public void ResetMock()
        {
            foreach (var item in Items)
            {
                item.State = ResultItemState.None;
            }
            UpdatedItems.Clear();
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

        public void TriggerBeginEditItemName()
        {
            BeginEditItemName?.Invoke(this, EventArgs.Empty);
        }

        public void TriggerCancelEditItemName()
        {
            CancelEditItemName?.Invoke(this, EventArgs.Empty);
        }

        public void TriggerCloseView()
        {
            CloseView?.Invoke(this, EventArgs.Empty);
        }

        public void TriggerRenameItem(string newName)
        {
            RenameItem?.Invoke(this, new RenameEventArgs(newName));
        }

        // mocked interface
        public void UpdateSize()
        {
        }

        public void UpdateSize(Size itemSize)
        {
            throw new NotImplementedException();
        }

        public void UpdateItems()
        {
            UpdatedItems.Clear();
            for (int i = 0; i < Items.Count; ++i)
            {
                UpdatedItems.Add(i);
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
            UpdatedItems.Add(index);
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

        public void ShowItemEditForm(int index)
        {
            EditIndex = index;
        }

        public void HideItemEditForm()
        {
            EditIndex = -1;
        }

        public void Show(DockPanel dockPanel, DockState dockState)
        {
            throw new NotImplementedException();
        }

        public void EnsureVisible()
        {
        }

        public IAsyncResult BeginInvoke(Delegate action)
        {
            throw new NotImplementedException();
        }

        public void BeginDragDrop(IDataObject data, DragDropEffects effect)
        {
            throw new NotImplementedException();
        }

        public void MakeActive()
        {
            IsActive = true;
        }

        public void Dispose()
        {
        }
    }
}
