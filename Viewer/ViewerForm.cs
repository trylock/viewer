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
using Viewer.UI.Explorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer
{
    public partial class ViewerForm : Form
    {
        private DockPanel _dockPanel;

        public ViewerForm()
        {
            InitializeComponent();

            _dockPanel = new DockPanel();
            _dockPanel.Theme = new VS2015LightTheme();
            _dockPanel.Dock = DockStyle.Fill;
            Controls.Add(_dockPanel);

            var factory = new AttributeStorageFactory();

            // models
            var storage = factory.Create();
            var selection = new Selection();
            var clipboard = new ClipboardService();
            var thumbnailGenerator = new ThumbnailGenerator();

            // UI
            var progressForm = new ProgressViewForm();
            {
                var directoryTreeView = new DirectoryTreeControl();
                directoryTreeView.Text = Resources.ExplorerWindowName;
                directoryTreeView.Show(_dockPanel, DockState.DockLeft);

                var treePresenter = new DirectoryTreePresenter(directoryTreeView, progressForm, clipboard);
                treePresenter.UpdateRootDirectories();
            }

            {
                var queryResultView = new ThumbnailGridControl
                {
                    Text = Resources.QueryResultWindowName
                };
                queryResultView.Show(_dockPanel, DockState.Document);

                var resultPresenter = new QueryResultPresenter(queryResultView, storage, thumbnailGenerator, selection);
                resultPresenter.LoadDirectory("C:/tmp");
            }
        }
    }
}
