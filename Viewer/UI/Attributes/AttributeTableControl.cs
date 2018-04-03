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
using Attribute = Viewer.Data.Attribute;

namespace Viewer.UI.Attributes
{
    public partial class AttributeTableControl : WindowView, IAttributeView
    {
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
            }

            public void Visit(DoubleAttribute attr)
            {
                _row.Cells.Add(new DataGridViewTextBoxCell
                {
                    ValueType = typeof(double),
                    Value = attr.Value
                });
            }

            public void Visit(StringAttribute attr)
            {
                _row.Cells.Add(new DataGridViewTextBoxCell
                {
                    ValueType = typeof(string),
                    Value = attr.Value
                });
            }

            public void Visit(DateTimeAttribute attr)
            {
                _row.Cells.Add(new DateTimeCell{ Value = attr.Value });
            }

            public void Visit(ImageAttribute attr)
            {
                throw new NotImplementedException();
            }
        }

        public AttributeTableControl()
        {
            InitializeComponent();

            ShowAttribute(new IntAttribute("test", AttributeSource.Custom, 42));
            ShowAttribute(new DateTimeAttribute("test2", AttributeSource.Custom, new DateTime(2018, 4, 2, 0, 0, 0)));
        }

        public void ShowAttribute(Attribute attr)
        {
            var row = new DataGridViewRow();
            row.Cells.Add(new DataGridViewTextBoxCell{ ValueType = typeof(string), Value = attr.Name });
            attr.Accept(new RowAttributeVisitor(row));
            
            GridView.Rows.Add(row);
        }
    }
}
