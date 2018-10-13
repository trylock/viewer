using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core;
using Viewer.Core.UI;

namespace Viewer.UI.UserSettings
{
    internal partial class SettingsView : WindowView, ISettingsView
    {
        public SettingsView()
        {
            InitializeComponent();
        }

        #region ISettingsView 

        public event EventHandler ProgramsChanged;

        private List<ExternalApplication> _programs;
        public List<ExternalApplication> Programs
        {
            get => _programs;
            set
            {
                CaptureSelectedProgram();

                _programs = value;
                ProgramsGridView.DataSource = value;

                RestoreSelectedProgram();
            }
        }

        private ExternalApplication _lastSelectedProgram;
        private int _lastSelectedColumn;

        private void CaptureSelectedProgram()
        {
            if (ProgramsGridView.SelectedCells.Count > 0)
            {
                var cell = ProgramsGridView.SelectedCells[0];
                _lastSelectedProgram = (ExternalApplication) cell.OwningRow.DataBoundItem;
                _lastSelectedColumn = cell.ColumnIndex;
            }
        }

        private void RestoreSelectedProgram()
        {
            if (_lastSelectedProgram == null)
            {
                return;
            }

            var rowIndex = _programs.IndexOf(_lastSelectedProgram);
            if (rowIndex < 0)
            {
                return;
            }

            var columnIndex = _lastSelectedColumn;
            ProgramsGridView.CurrentCell = ProgramsGridView[columnIndex, rowIndex];
        }

        #endregion

        private void ProgramsGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            ProgramsChanged?.Invoke(sender, e);
        }

        private void ProgramsGridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                var removedApps = ProgramsGridView.SelectedCells
                    .OfType<DataGridViewCell>()
                    .Where(cell => cell.RowIndex + 1 < ProgramsGridView.RowCount)
                    .Select(cell => cell.OwningRow.DataBoundItem)
                    .Cast<ExternalApplication>();
                _programs.RemoveAll(app => removedApps.Contains(app));

                ProgramsGridView.DataSource = null;
                ProgramsGridView.DataSource = _programs;

                ProgramsChanged?.Invoke(sender, e);
            }
        }
    }
}
