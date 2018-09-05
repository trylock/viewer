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
using ScintillaNET;
using Viewer.Core.UI;
using Viewer.Properties;
using Viewer.Query;

namespace Viewer.UI.QueryEditor
{
    internal partial class QueryEditorView : WindowView, IQueryEditorView
    {
        public QueryEditorView()
        {
            InitializeComponent();

            ViewerForm.Theme.ApplyTo(EditorToolStrip);

            // initialize highlighting
            QueryTextBox.StyleResetDefault();
            QueryTextBox.Styles[Style.Default].Font = "Consolas";
            QueryTextBox.Styles[Style.Default].Size = 11;
            QueryTextBox.StyleClearAll();

            QueryTextBox.Styles[Style.LineNumber].ForeColor = Color.FromArgb(0x888888);
            QueryTextBox.Styles[Style.LineNumber].BackColor = Color.White;

            QueryTextBox.Styles[Style.Sql.String].ForeColor = Color.FromArgb(0xa31515);
            QueryTextBox.Styles[Style.Sql.Word].ForeColor = Color.FromArgb(0x0000ff);
            QueryTextBox.Styles[Style.Sql.Operator].ForeColor = Color.FromArgb(0x444444);
            QueryTextBox.Styles[Style.Sql.Number].ForeColor = Color.FromArgb(0x09885a);

            QueryTextBox.Lexer = Lexer.Sql;
            QueryTextBox.SetKeywords(0, "select where order by desc asc and or not union except intersect");

            QueryTextBox.Margins[0].Type = MarginType.Number;
            QueryTextBox.Margins[0].Width = 20;

            QueryTextBox.ClearCmdKey(Keys.Control | Keys.S);
            QueryTextBox.ClearCmdKey(Keys.Control | Keys.O);
        }

        #region Drop view
        
        public event EventHandler<DragEventArgs> OnDrop;

        #endregion

        #region View interface

        public event EventHandler RunQuery;
        public event EventHandler QueryChanged;
        public event EventHandler SaveQuery;
        public event EventHandler<OpenQueryEventArgs> OpenQuery;
        public event EventHandler<QueryViewEventArgs> OpenQueryView;

        private ICollection<QueryView> _views;
        public ICollection<QueryView> Views
        {
            get => _views;
            set
            {
                _views = value;
                QueryViewsDropDown.DropDownItems.Clear();
                foreach (var view in value)
                {
                    var viewCapture = view;
                    var item = new ToolStripMenuItem(view.Name);
                    item.Click += (sender, args) => 
                        OpenQueryView?.Invoke(sender, new QueryViewEventArgs(viewCapture));
                    QueryViewsDropDown.DropDownItems.Add(item);
                }
            }
        }

        public string FullPath { get; set; }

        public string Query
        {
            get => QueryTextBox.Text;
            set => QueryTextBox.Text = value;
        }

        public string PickFileForWrite()
        {
            var result = SaveDialog.ShowDialog();
            return result != DialogResult.OK ? null : SaveDialog.FileName;
        }

        #endregion

        private void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5 || (e.Control && e.KeyCode == Keys.Enter))
            {
                RunQuery?.Invoke(sender, e);
            }
            else if (e.Control && e.KeyCode == Keys.S)
            {
                SaveQuery?.Invoke(sender, e);
            }
            else if (e.Control && e.KeyCode == Keys.O)
            {
                OpenButton_Click(sender, e);
            }
        }

        private void QueryTextBox_TextChanged(object sender, EventArgs e)
        {
            QueryChanged?.Invoke(sender, e);
        }
        
        private void QueryTextBox_DragDrop(object sender, DragEventArgs e)
        {
            OnDrop?.Invoke(sender, e);
        }

        private void QueryTextBox_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            var result = OpenDialog.ShowDialog();
            if (result != DialogResult.OK)
            {
                return;
            }

            OpenQuery?.Invoke(sender, new OpenQueryEventArgs(OpenDialog.FileName));
        }
        
        private void SaveButton_Click(object sender, EventArgs e)
        {
            SaveQuery?.Invoke(sender, e);
        }

        private void RunButton_Click(object sender, EventArgs e)
        {
            RunQuery?.Invoke(sender, e);
        }

        protected override string GetPersistString()
        {
            return base.GetPersistString() + ";" + Query + ";" + (FullPath ?? "");
        }
    }
}
