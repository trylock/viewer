using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Viewer.UI.Images
{
    internal partial class HistoryView : UserControl, IHistoryView
    {
        public HistoryView()
        {
            InitializeComponent();
        }

        #region IHistoryView

        public bool CanGoForwardInHistory { get; set; }
        public bool CanGoBackInHistory { get; set; }

        public event EventHandler GoBackInHistory;
        public event EventHandler GoForwardInHistory;
        public event EventHandler UserSelectedItem;
        public event EventHandler<HistoryItemEventArgs> ItemAdded;

        public IReadOnlyList<QueryHistoryItem> Items
        {
            get => HistoryComboBox.Items.OfType<QueryHistoryItem>().ToList();
            set
            {
                HistoryComboBox.Items.Clear();
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
    }
}
