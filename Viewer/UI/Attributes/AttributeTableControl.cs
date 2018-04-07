using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.Properties;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    public partial class AttributeTableControl : WindowView, IAttributeView
    {
        private const int TypeColumnIndex = 2;
        
        public AttributeTableControl()
        {
            InitializeComponent();

            // add types column
            var typeColumn = GridView.Columns[TypeColumnIndex] as DataGridViewComboBoxColumn;
            typeColumn.DataSource = Enum.GetValues(typeof(AttributeType));
            typeColumn.ValueType = typeof(AttributeType);
        }

        #region View interface

        public event EventHandler SaveAttributes;
        public event EventHandler<AttributeChangedEventArgs> AttributeChanged;
        public event EventHandler<AttributeDeletedEventArgs> AttributeDeleted;

        public bool EditingEnabled
        {
            get => GridView.Enabled;
            set
            {
                GridView.Enabled = value;
                NoSelectionLabel.Visible = !value;
            }
        }

        public List<AttributeView> Attributes { get; set; } = new List<AttributeView>();

        private bool _suspendUpdateEvent = false;

        public void UpdateAttributes()
        {
            _suspendUpdateEvent = true;
            SuspendLayout();
            try
            {
                GridView.Rows.Clear();
                foreach (var attr in Attributes)
                {
                    var row = CreateAttributeView(attr);
                    GridView.Rows.Add(row);
                }
            }
            finally
            {
                ResumeLayout();
                _suspendUpdateEvent = false;
            }
        }

        public void UpdateAttribute(int index)
        {
            if (index < 0 || index >= Attributes.Count)
                return;

            var row = CreateAttributeView(Attributes[index]);
            _suspendUpdateEvent = true;
            try
            {
                if (index < GridView.Rows.Count)
                    GridView.Rows.RemoveAt(index);
                GridView.Rows.Insert(index, row);
            }
            finally
            {
                _suspendUpdateEvent = false;
            }
        }

        public void AttributeNameIsNotUnique(string name)
        {
            MessageBox.Show(
                string.Format(Resources.DuplicateAttributeName_Message, name),
                Resources.DuplicateAttributeName_Label,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public void AttributeNameIsEmpty()
        {
            MessageBox.Show(
                Resources.AttributeNameEmpty_Message, 
                Resources.AttributeNameEmpty_Label, 
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        #endregion

        private class RowAttributeVisitor : IAttributeVisitor
        {
            private DataGridViewRow _row;

            public RowAttributeVisitor(DataGridViewRow row)
            {
                _row = row;
            }

            public void Visit(IntAttribute attr)
            {
                _row.Cells.Add(new DataGridViewTextBoxCell
                {
                    ValueType = typeof(int),
                    Value = attr.Value
                });
                AddTypeColumn(AttributeType.Int);
            }

            public void Visit(DoubleAttribute attr)
            {
                _row.Cells.Add(new DataGridViewTextBoxCell
                {
                    ValueType = typeof(double),
                    Value = attr.Value
                });
                AddTypeColumn(AttributeType.Double);
            }

            public void Visit(StringAttribute attr)
            {
                _row.Cells.Add(new DataGridViewTextBoxCell
                {
                    ValueType = typeof(string),
                    Value = attr.Value
                });
                AddTypeColumn(AttributeType.String);
            }

            public void Visit(DateTimeAttribute attr)
            {
                _row.Cells.Add(new DateTimeCell { Value = attr.Value });
                AddTypeColumn(AttributeType.DateTime);
            }

            public void Visit(ImageAttribute attr)
            {
                throw new NotImplementedException();
            }

            private void AddTypeColumn(AttributeType type)
            {
                _row.Cells.Add(new DataGridViewComboBoxCell
                {
                    DataSource = Enum.GetValues(typeof(AttributeType)),
                    ValueType = typeof(AttributeType),
                    Value = type,
                });
            }
        }

        private DataGridViewRow CreateAttributeView(AttributeView attr)
        {
            var row = new DataGridViewRow { Tag = attr };
            row.Cells.Add(new DataGridViewTextBoxCell { ValueType = typeof(string), Value = attr.Data.Name });

            if (attr.IsMixed)
            {
                var mixedValueCell = new DataGridViewTextBoxCell();
                mixedValueCell.Value = "mixed value";
                mixedValueCell.ValueType = typeof(string);
                mixedValueCell.Style.ForeColor = Color.Gray;

                row.Cells.Add(mixedValueCell);
            }
            else
            {
                attr.Data.Accept(new RowAttributeVisitor(row));
            }

            return row;
        }
        
        private Attribute TryParseRow(DataGridViewRow row)
        {
            var name = row.Cells[0].Value as string;
            var value = row.Cells[1].Value;
            var type = row.Cells[2].Value as AttributeType?;
            
            switch (type)
            {
                case AttributeType.Int:
                    return new IntAttribute(name, value as int? ?? 0);
                case AttributeType.Double:
                    return new DoubleAttribute(name, value as double? ?? 0.0);
                case AttributeType.String:
                    return new StringAttribute(name, value as string ?? "");
                case AttributeType.DateTime:
                    return new DateTimeAttribute(name, value as DateTime? ?? DateTime.Now);
                case null:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void GridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || _suspendUpdateEvent)
                return;

            var row = GridView.Rows[e.RowIndex];
            var newValue = TryParseRow(row);
            if (newValue == null)
                return;
            
            AttributeChanged?.Invoke(sender, new AttributeChangedEventArgs
            {
                Index = e.RowIndex,
                OldValue = Attributes[e.RowIndex],
                NewValue = new AttributeView { IsMixed = false, Data = newValue }
            });
        }

        private void GridView_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            e.Row.Cells[TypeColumnIndex].Value = AttributeType.String;
        }

        private void GridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.S)
            {
                SaveAttributes?.Invoke(sender, e);
            }
            else if (e.KeyCode == Keys.Delete)
            {
                var deleted = new List<int>();
                foreach (DataGridViewCell cell in GridView.SelectedCells)
                {
                    deleted.Add(cell.RowIndex);
                }

                AttributeDeleted?.Invoke(sender, new AttributeDeletedEventArgs
                {
                    Deleted = deleted
                });
            }
        }
    }
}
