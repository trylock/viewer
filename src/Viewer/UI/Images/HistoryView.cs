using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.UI.Forms;

namespace Viewer.UI.Images
{
    internal partial class HistoryView : UserControl, IHistoryView
    {
        public HistoryView()
        {
            InitializeComponent();

            var imageStyles = new StateStyles
            {
                Normal = new Styles
                {
                    StrokeWidth = 2,
                    StrokeColor = Color.FromArgb(unchecked((int)0xff69696C))
                },
                Disabled = new Styles
                {
                    StrokeWidth = 2,
                    StrokeColor = Color.FromArgb(unchecked((int)0xffBCBCC0))
                }
            };

            GoBackButton.Image = VectorIcons.GoBackIcon;
            GoBackButton.ImageStyles = imageStyles;

            GoForwardButton.Image = VectorIcons.GoForwardIcon;
            GoForwardButton.ImageStyles = imageStyles;

            GoUpButton.Image = VectorIcons.ArrowUpIcon;
            GoUpButton.ImageStyles = new StateStyles
            {
                Normal = new Styles
                {
                    StrokeColor = imageStyles.Normal.StrokeColor,
                    StrokeWidth = 2,
                    LineJoin = LineJoin.Bevel
                }
            };
        }

        #region IHistoryView

        public bool CanGoForwardInHistory
        {
            get => GoForwardButton.Enabled;
            set => GoForwardButton.Enabled = value;
        }

        public bool CanGoBackInHistory
        {
            get => GoBackButton.Enabled;
            set => GoBackButton.Enabled = value;
        }

        public event EventHandler GoBackInHistory;
        public event EventHandler GoForwardInHistory;
        public event EventHandler GoUp;
        public event EventHandler UserSelectedItem;
        public event EventHandler<HistoryItemEventArgs> ItemAdded;

        [IgnoreDataMember]
        public IReadOnlyList<QueryHistoryItem> Items
        {
            get => HistoryComboBox.Items.OfType<QueryHistoryItem>().ToList();
            set
            {
                HistoryComboBox.Items.Clear();
                if (value == null)
                {
                    return;
                }

                foreach (var item in value)
                {
                    HistoryComboBox.Items.Add(item);
                }
            }
        }

        public QueryHistoryItem SelectedItem
        {
            get => HistoryComboBox.SelectedItem as QueryHistoryItem;
            set
            {
                _isSelectedBySetter = true;
                try
                {
                    HistoryComboBox.SelectedItem = value;
                }
                finally
                {
                    _isSelectedBySetter = false;
                }
            }
        }

        #endregion

        public void GoBack()
        {
            GoBackInHistory?.Invoke(this, EventArgs.Empty);
        }

        public void GoForward()
        {
            GoForwardInHistory?.Invoke(this, EventArgs.Empty);
        }

        public void GoToParent()
        {
            GoUp?.Invoke(this, EventArgs.Empty);
        }

        private void HistoryComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ItemAdded?.Invoke(sender, new HistoryItemEventArgs(HistoryComboBox.Text));
            }
        }

        private bool _isSelectedBySetter;

        private void HistoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isSelectedBySetter)
            {
                return;
            }

            UserSelectedItem?.Invoke(sender, e);
        }

        private void GoBackButton_Click(object sender, EventArgs e)
        {
            GoBack();
        }

        private void GoForwardButton_Click(object sender, EventArgs e)
        {
            GoForward();
        }

        private void GoUpButton_Click(object sender, EventArgs e)
        {
            GoToParent();
        }

        private void HistoryView_Layout(object sender, LayoutEventArgs e)
        {
            HistoryComboBox.Width = Width - HistoryComboBox.Location.X - 5;
        }
    }
}
