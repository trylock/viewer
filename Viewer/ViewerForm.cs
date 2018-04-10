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
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.Properties;
using Viewer.UI;
using Viewer.UI.Attributes;
using Viewer.UI.Images;
using Viewer.UI.Explorer;
using Viewer.UI.Tasks;
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
            var fileSystem = new FileSystem();
            var entityManager = new EntityManager(storage);
            var attributeManager = new AttributeManager(entityManager, selection);
            var thumbnailGenerator = new ThumbnailGenerator();

            // UI
            var fileSystemErrorView = new FileSystemErrorView();
            
            // explorer
            var directoryTreeView = new DirectoryTreeControl();
            directoryTreeView.Text = Resources.ExplorerWindowName;
            directoryTreeView.Show(_dockPanel, DockState.DockLeft);

            // images
            var imagesView = new GridControl(Resources.QueryResultWindowName);
            imagesView.Show(_dockPanel, DockState.Document);
                
            // attributes
            var attributesView = new AttributeTableControl("Attributes");
            attributesView.Show(_dockPanel, DockState.DockRight);

            // exif attributes
            var exifAttributesView = new AttributeTableControl("Exif");
            exifAttributesView.Show(_dockPanel, DockState.DockRight);

            var tasksView = new TasksView();
            tasksView.Show(attributesView.Pane, DockAlignment.Bottom, 0.4);

            var progressViewFactory = new ProgressViewFactory(tasksView);

            // presenters
            var attrPresenter = new AttributesPresenter(attributesView, progressViewFactory, selection, entityManager, attributeManager);
            attrPresenter.AttributePredicate = attr => (attr.Data.Flags & AttributeFlags.ReadOnly) == 0;
            var exifAttrPresenter = new AttributesPresenter(exifAttributesView, progressViewFactory, selection, entityManager, attributeManager);
            exifAttrPresenter.AttributePredicate = attr => (attr.Data.Flags & AttributeFlags.ReadOnly) != 0;

            var imagesPresenter = new ImagesPresenter(imagesView, fileSystemErrorView, entityManager, clipboard, selection, thumbnailGenerator);
            imagesPresenter.LoadDirectoryAsync("D:/dataset/large");

            var treePresenter = new DirectoryTreePresenter(directoryTreeView, progressViewFactory, fileSystemErrorView, fileSystem, clipboard);
            treePresenter.UpdateRootDirectories();
        }
    }
}
