using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.Properties;
using Viewer.UI;
using Viewer.UI.Images;
using Viewer.UI.FileExplorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer
{
    public partial class ViewerForm : Form
    {
        private QueryResultPresenter _resultPresenter;
        private DirectoryTreePresenter _treePresenter;

        private DockPanel _dockPanel;

        public ViewerForm()
        {
            InitializeComponent();

            _dockPanel = new DockPanel();
            _dockPanel.Dock = DockStyle.Fill;
            Controls.Add(_dockPanel);

            // models
            var factory = new AttributeStorageFactory();
            var storage = factory.Create();
            
            // views
            var directoryTreeView = new DirectoryTreeControl();
            directoryTreeView.Text = Resources.ExplorerWindowName;
            directoryTreeView.Show(_dockPanel, DockState.DockLeft);

            var queryResultView = new ThumbnailGridControl();
            queryResultView.Text = Resources.QueryResultWindowName;
            queryResultView.Show(_dockPanel, DockState.Document);

            var progressForm = new ProgressViewForm();

            // presenters
            _treePresenter = new DirectoryTreePresenter(directoryTreeView, progressForm);
            _treePresenter.UpdateRootDirectories();
            
            _resultPresenter = new QueryResultPresenter(queryResultView, storage);
            _resultPresenter.LoadDirectory("C:/tmp");
        }
    }
}
