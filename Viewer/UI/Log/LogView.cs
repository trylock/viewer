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
using Viewer.Properties;

namespace Viewer.UI.Log
{
    [Export(typeof(ILogView))]
    public partial class LogView : WindowView, ILogView
    {
        private static Image[] _logTypeIcon =
        {
            Resources.ErrorIcon,
            Resources.WarningIcon,
            Resources.MessageIcon
        };
        
        public LogView()
        {
            InitializeComponent();
        }

        #region View

        public event EventHandler<RetryEventArgs> Retry;

        public IEnumerable<LogEntry> Entries { get; set; } = Enumerable.Empty<LogEntry>();

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
                    Value = entry.Message
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
                        Retry?.Invoke(this, new RetryEventArgs(localEntry));
                        LogEntryGridView.Rows.Remove(row);
                    }
                };
                row.Cells.Add(retryCell);

                LogEntryGridView.Rows.Add(row);
            }
            LogEntryGridView.Update();
        }

        #endregion
    }
}
