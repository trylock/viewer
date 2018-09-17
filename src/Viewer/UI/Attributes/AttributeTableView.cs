using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Properties;
using Viewer.UI.Forms;
using Viewer.UI.Suggestions;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    internal partial class AttributeTableView : WindowView, IAttributeView
    {
        /// <summary>
        /// Background color of an attribute which is not set in all entities in selection
        /// </summary>
        private readonly Color _globalBackColor = Color.AliceBlue;

        /// <summary>
        /// Background color of a read only attribute
        /// </summary>
        private readonly Color _readOnlyBackColor = Color.LightGray;
        
        private const int TypeColumnIndex = 2;

        public AttributeTableView()
        {
            InitializeComponent();

            // add types column
            var typeColumn = GridView.Columns[TypeColumnIndex] as DataGridViewComboBoxColumn;
            typeColumn.DataSource = Enum.GetValues(typeof(AttributeType));
            typeColumn.ValueType = typeof(AttributeType);
        }

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region View interface

        public event EventHandler SaveAttributes;
        public event EventHandler<AttributeChangedEventArgs> AttributeChanged;
        public event EventHandler<AttributeDeletedEventArgs> AttributeDeleted;
        public event EventHandler<SortEventArgs> SortAttributes;
        public event EventHandler<NameEventArgs> NameChanged;
        public event EventHandler<NameEventArgs> BeginValueEdit;

        public List<AttributeGroup> Attributes { get; set; } = new List<AttributeGroup>();

        private SuggestionTextBox SuggestionEditControl => GridView.EditingControl as SuggestionTextBox;

        /// <inheritdoc />
        /// <summary>
        /// If the current editing control supports suggestions (e.g., it is a SuggestionTextBox),
        /// this will forward all calls to its Suggestion control. Otherwise, this is just a no-op.
        /// </summary>
        public IEnumerable<SuggestionItem> Suggestions
        {
            get => SuggestionEditControl?.Suggestions.Items ?? Enumerable.Empty<SuggestionItem>();
            set
            {
                var suggestions = SuggestionEditControl?.Suggestions;
                if (suggestions == null)
                {
                    return;
                }
                suggestions.Items = value;
                suggestions.ShowAtCurrentControl();
            }
        }
        
        private AttributeViewType _viewType;
        public AttributeViewType ViewType
        {
            get => _viewType;
            set
            {
                _viewType = value;

                if (value == AttributeViewType.Exif)
                {
                    Icon = Resources.ExifComponentIcon;
                    GridView.Size = new Size(
                        ClientSize.Width,
                        ClientSize.Height - SearchTextBox.Height
                    );
                    SaveButton.Hide();
                }
                else
                {
                    Icon = Resources.AttributesComponentIcon;
                }

                Invalidate();
            }
        }

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

        #region Search View
        
        public event EventHandler Search;

        public string SearchQuery
        {
            get => SearchTextBox.Text;
            set => SearchTextBox.Text = value;
        }

        #endregion

        private class RowAttributeVisitor : IValueVisitor
        {
            private readonly DataGridViewRow _row;

            public RowAttributeVisitor(DataGridViewRow row)
            {
                _row = row;
            }

            public void Visit(IntValue attr)
            {
                _row.Cells.Add(new SuggestionTextBoxCell
                {
                    ValueType = typeof(int),
                    Value = attr.Value
                });
                AddTypeColumn(AttributeType.Int);
            }

            public void Visit(RealValue attr)
            {
                _row.Cells.Add(new SuggestionTextBoxCell
                {
                    ValueType = typeof(double),
                    Value = attr.Value
                });
                AddTypeColumn(AttributeType.Double);
            }

            public void Visit(StringValue attr)
            {
                var cell = new SuggestionTextBoxCell
                {
                    ValueType = typeof(string),
                    Value = attr.Value,
                    MaxInputLength = int.MaxValue,
                    Style =
                    {
                        WrapMode = DataGridViewTriState.True
                    },
                };
                _row.Cells.Add(cell);
                AddTypeColumn(AttributeType.String);
            }

            public void Visit(DateTimeValue attr)
            {
                _row.Cells.Add(new DateTimeCell { Value = attr.Value });
                AddTypeColumn(AttributeType.DateTime);
            }

            public void Visit(ImageValue attr)
            {
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

        private DataGridViewRow CreateAttributeView(AttributeGroup attr)
        {
            var row = new DataGridViewRow { Tag = attr };
            row.Cells.Add(new SuggestionTextBoxCell
            {
                ValueType = typeof(string),
                Value = attr.Value.Name
            });

            if (attr.HasMultipleValues)
            {
                var mixedValueCell = new DataGridViewTextBoxCell
                {
                    Value = "mixed value",
                    ValueType = typeof(string),
                    Style = {ForeColor = Color.Gray}
                };

                row.Cells.Add(mixedValueCell);
            }
            else
            {
                attr.Value.Value.Accept(new RowAttributeVisitor(row));

                if (!attr.IsInAllEntities)
                {
                    row.DefaultCellStyle.BackColor = _globalBackColor;
                }
            }

            // disable editing if the attribute is readonly
            if (attr.Value.Source == AttributeSource.Metadata)
            {
                row.ReadOnly = true;
                row.DefaultCellStyle.BackColor = _readOnlyBackColor;
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
                    return new Attribute(name, new IntValue(value as int? ?? 0), AttributeSource.Custom);
                case AttributeType.Double:
                    return new Attribute(name, new RealValue(value as double? ?? 0), AttributeSource.Custom);
                case AttributeType.String:
                    return new Attribute(name, new StringValue(value as string ?? ""), AttributeSource.Custom);
                case AttributeType.DateTime:
                    return new Attribute(name, new DateTimeValue(value as DateTime? ?? DateTime.Now), AttributeSource.Custom);
                case null:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
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
                NewValue = new AttributeGroup { HasMultipleValues = false, Value = newValue }
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
                SaveButton_Click(sender, e);
            }
            else if (e.Control && e.KeyCode == Keys.F)
            {
                SearchTextBox.Focus();
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

        private void GridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2) // type column
            {
                GridView.BeginEdit(false);
                var comboBox = GridView.EditingControl as DataGridViewComboBoxEditingControl;
                if (comboBox != null)
                {
                    comboBox.DroppedDown = true;
                }
            }
        }

        private void GridView_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            SortColumn column = SortColumn.Name;
            switch (e.ColumnIndex)
            {
                case 0:
                    column = SortColumn.Name;
                    break;
                case 1:
                    column = SortColumn.Value;
                    break;
                case 2:
                    column = SortColumn.Type;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(e));
            }

            SortAttributes?.Invoke(sender, new SortEventArgs
            {
                Column = column
            });
        }

        #region Show name/value suggestions

        private void GridView_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            var suggestionControl = SuggestionEditControl;
            if (suggestionControl == null)
            {
                return;
            }

            // grid view re-uses controls, make sure we won't subscribe an event handler twice
            suggestionControl.Suggestions.Accepted -= SuggestionsOnAccepted;
            suggestionControl.Suggestions.Accepted += SuggestionsOnAccepted;

            const int nameColumn = 0;
            const int valueColumn = 1;

            if (GridView.CurrentCell.ColumnIndex == nameColumn)
            {
                // don't select a value by default
                suggestionControl.Suggestions.DefaultSelectedIndex = -1;

                suggestionControl.TextChanged -= NameTextBox_TextChanged;
                suggestionControl.TextChanged += NameTextBox_TextChanged;
                suggestionControl.Disposed -= NameTextBox_Disposed;
                suggestionControl.Disposed += NameTextBox_Disposed;

                // show suggestions
                NameChanged?.Invoke(sender, new NameEventArgs
                {
                    Value = GridView.CurrentCell.Value as string
                });
            }
            else if (GridView.CurrentCell.ColumnIndex == valueColumn) 
            {
                // don't select a value by default
                suggestionControl.Suggestions.DefaultSelectedIndex = -1;

                // trigger the BeginValueEdit event so that listeners can load suggestions
                var name = GridView.CurrentRow?.Cells[0].Value as string;
                BeginValueEdit?.Invoke(sender, new NameEventArgs
                {
                    Value = name
                });
            }
        }

        private void NameTextBox_Disposed(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            textBox.TextChanged -= NameTextBox_TextChanged;
            textBox.Disposed -= NameTextBox_Disposed;
        }

        private bool _suppressNameChanged;

        private void NameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (GridView.CurrentCell.ColumnIndex != 0)
            {
                return;
            }

            var textBox = sender as TextBox;
            if (textBox == null || _suppressNameChanged)
            {
                return;
            }

            NameChanged?.Invoke(sender, new NameEventArgs
            {
                Value = textBox.Text
            });
        }

        private void SuggestionsOnAccepted(object sender, SuggestionEventArgs e)
        {
            if (GridView.CurrentCell?.ColumnIndex == 0)
            {
                NameSuggestionsOnAccepted(sender, e);
            }
            else if (GridView.CurrentCell?.ColumnIndex == 1)
            {
                ValueSuggestionsOnAccepted(sender, e);
            }
        }
        
        private void NameSuggestionsOnAccepted(object sender, SuggestionEventArgs e)
        {
            var cell = GridView.CurrentCell;
            if (cell == null || cell.ColumnIndex != 0)
            {
                return;
            }

            if (!cell.IsInEditMode)
            {
                return;
            }

            var textBox = GridView.EditingControl as TextBox;
            if (textBox == null)
            {
                return;
            }

            _suppressNameChanged = true;
            try
            {
                textBox.Text = e.Value.Text;
            }
            finally
            {
                _suppressNameChanged = false;
            }
        }

        private void ValueSuggestionsOnAccepted(object sender, SuggestionEventArgs e)
        {
            var row = GridView.CurrentRow;
            if (row == null)
            {
                return;
            }

            var value = e.Value.UserData as BaseValue;
            if (value == null)
            {
                return;
            }

            var oldGroup = row.Tag as AttributeGroup;
            var newGroup = new AttributeGroup
            {
                EntityCount = oldGroup.EntityCount,
                HasMultipleValues = false,
                IsInAllEntities = true,
                Value = new Attribute(oldGroup.Value.Name, value, oldGroup.Value.Source)
            };

            var rowIndex = row.Index;
            GridView.Rows.RemoveAt(rowIndex);
            GridView.Rows.Insert(rowIndex, CreateAttributeView(newGroup));
        }

        #endregion

        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveAttributes?.Invoke(sender, e);
        }
        
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                SearchTextBox.Text = ""; // reset the filter
            }
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            Search?.Invoke(sender, e);
        }

        protected override string GetPersistString()
        {
            return base.GetPersistString() + ";" + (ViewType == AttributeViewType.Exif ? "exif" : "attributes");
        }
    }
}
