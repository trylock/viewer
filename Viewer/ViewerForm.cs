using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
using Viewer.UI.Log;
using Viewer.UI.Presentation;
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
            _dockPanel.UpdateDockWindowZOrder(DockStyle.Right, true);
            _dockPanel.UpdateDockWindowZOrder(DockStyle.Left, true);
            Controls.Add(_dockPanel);

            var factory = new AttributeStorageFactory();

            // dependencies
            var storage = factory.Create();
            var clipboard = new ClipboardService();
            var fileSystem = new FileSystem();
            var modifiedEntities = new EntityRepository();
            var thumbnailGenerator = new ThumbnailGenerator();
            var selection = new Selection();
            var attributeManager = new AttributeManager(selection);
            var log = new Log();

            // query 
            var queryResult = new EntityManager(modifiedEntities);
            foreach (var file in Directory.EnumerateFiles(@"D:\dataset\moderate"))
            {
                queryResult.Add(storage.Load(file));
            }

            // UI
            var fileSystemErrorView = new FileSystemErrorView();
            
            // explorer
            var directoryTreeView = new DirectoryTreeView(Resources.ExplorerWindowName);
            directoryTreeView.Show(_dockPanel, DockState.DockLeft);

            // images
            var imagesView = new ImagesGridView(Resources.QueryResultWindowName);
            imagesView.Show(_dockPanel, DockState.Document);

            // presentation
            var presentationView = new PresentationView("Presentation");
            presentationView.Show(_dockPanel, DockState.Document);

            // attributes
            var attributesView = new AttributeTableView("Attributes");
            attributesView.Show(_dockPanel, DockState.DockRight);

            // exif attributes
            var exifAttributesView = new AttributeTableView("Exif");
            exifAttributesView.Show(_dockPanel, DockState.DockRight);

            // log
            var logView = new LogView("Log");
            logView.Show(_dockPanel, DockState.DockBottom);

            // background tasks
            var tasksView = new TasksView("Background Tasks");
            tasksView.Show(_dockPanel, DockState.DockBottom);
            
            // tasks
            var progressViewFactory = new ProgressViewFactory(tasksView);

            // presenters
            var attrPresenter = new AttributesPresenter(attributesView, progressViewFactory, selection, storage, attributeManager);
            attrPresenter.AttributePredicate = attr => (attr.Data.Flags & AttributeFlags.ReadOnly) == 0;
            var exifAttrPresenter = new AttributesPresenter(
                exifAttributesView, 
                progressViewFactory, 
                selection,
                storage,
                attributeManager)
            {
                EditingEnabled = false,
                AttributePredicate = attr => attr.Data.GetType() != typeof(ImageAttribute) &&
                                             (attr.Data.Flags & AttributeFlags.ReadOnly) != 0
            };

            var imagesPresenter = new ImagesPresenter(imagesView, fileSystemErrorView, storage, clipboard, selection, thumbnailGenerator);
            imagesPresenter.LoadFromQueryResult(queryResult);

            var presentationPresenter = new PresentationPresenter(presentationView, selection);
            presentationPresenter.ShowEntity(queryResult, 0);

            var treePresenter = new DirectoryTreePresenter(directoryTreeView, progressViewFactory, fileSystemErrorView, fileSystem, clipboard);
            treePresenter.UpdateRootDirectories();

            var logPresenter = new LogPresenter(logView, log);
        }
    }
}
