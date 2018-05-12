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

namespace Viewer.UI.Query
{
    [Export(typeof(IQueryView))]
    public partial class QueryView : WindowView, IQueryView
    {
        public QueryView()
        {
            InitializeComponent();
        }

        #region View interface

        public event EventHandler RunQuery;

        public string Query
        {
            get => QueryTextBox.Text;
            set => QueryTextBox.Text = value;
        }

        #endregion

        private void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5)
            {
                RunQuery?.Invoke(sender, e);
            }
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            RunQuery?.Invoke(sender, e);
        }
    }
}
