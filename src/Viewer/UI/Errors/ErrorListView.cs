using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core.UI;
using Viewer.Properties;
using Viewer.UI.Errors;
using Viewer.UI.Forms;
using Viewer.UI.Images;

namespace Viewer.UI.Errors
{
    internal partial class ErrorListView : WindowView, IErrorListView
    {
        private static readonly Image[] _logTypeIcon =
        {
            Resources.ErrorIcon,
            Resources.WarningIcon,
            Resources.MessageIcon
        };
        
        public ErrorListView()
        {
            InitializeComponent();
        }

        #region View

        public event EventHandler<ErrorListEntryEventArgs> Retry;
        public event EventHandler<ErrorListEntryEventArgs> ActivateEntry;

        public IEnumerable<ErrorListEntry> Entries { get; set; } = 
            Enumerable.Empty<ErrorListEntry>();

        public void UpdateEntries()
        {
            LogEntryGridView.Rows.Clear();
            foreach (var entry in Entries)
            {
                var row = (DataGridViewRow)LogEntryGridView.RowTemplate.Clone();
                row.Cells.Add(new DataGridViewImageCell
                {
                    Value = _logTypeIcon[(int)entry.Type],
                });
                row.Cells.Add(new DataGridViewTextBoxCell
                {
                    Value = entry.Line
                });
                row.Cells.Add(new DataGridViewTextBoxCell
                {
                    Value = entry.Column
                });
                row.Cells.Add(new DataGridViewTextBoxCell
                {
                    Value = entry.Message,
                    Style =
                    {
                        Alignment = DataGridViewContentAlignment.MiddleLeft
                    }
                });
                row.Cells.Add(new DataGridViewTextBoxCell
                {
                    Value = entry.Group
                });

                // retry button
                var localEntry = entry;
                var retryCell = new ButtonCell
                {
                    Value = "Retry",
                    Enabled = entry.RetryOperation != null,
                };
                retryCell.Click += (sender, e) =>
                {
                    if (localEntry.RetryOperation != null)
                    {
                        Retry?.Invoke(this, new ErrorListEntryEventArgs(localEntry));
                        LogEntryGridView.Rows.Remove(row);
                    }
                };
                row.Cells.Add(retryCell);

                LogEntryGridView.Rows.Add(row);
            }
            LogEntryGridView.Update();
        }

        #endregion

        private void LogEntryGridView_DoubleClick(object sender, EventArgs e)
        {
            var rowIndex = LogEntryGridView.CurrentCell?.RowIndex ?? -1;
            if (rowIndex < 0)
            {
                return;
            }

            var entry = Entries.ElementAtOrDefault(rowIndex);
            if (entry == null)
            {
                return;
            }

            ActivateEntry?.Invoke(this, new ErrorListEntryEventArgs(entry));
        }
    }
}
