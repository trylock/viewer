using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.Properties;
using Viewer.UI;
using Viewer.UI.Attributes;
using Viewer.UI.Explorer;
using Viewer.UI.Images;
using Viewer.UI.Log;
using Viewer.UI.Presentation;
using Viewer.UI.Tasks;
using WeifenLuo.WinFormsUI.Docking;
using Attribute = Viewer.Data.Attribute;

namespace Viewer
{
    public interface IViewerApplication
    {
        /// <summary>
        /// Show list of entities
        /// </summary>
        /// <param name="title">Title of the component</param>
        /// <param name="entities">Entities</param>
        void ShowImages(string title, IEntityManager entities);

        /// <summary>
        /// Show a single image in a presentation component
        /// </summary>
        /// <param name="title">Title of the component</param>
        /// <param name="entities">Entities</param>
        /// <param name="index">Index of an entity to show</param>
        void ShowImage(string title, IEntityManager entities, int index);
    }

    public class ViewerApplication : IViewerApplication
    {
        private ViewerForm _form;

        // dependencies
        private readonly IAttributeStorage _storage;
        private readonly IClipboardService _clipboard;
        private readonly IFileSystem _fileSystem;
        private readonly IEntityRepository _modifiedEntities;
        private readonly IThumbnailGenerator _thumbnailGenerator;
        private readonly ISelection _selection;
        private readonly ILog _log;
        private readonly IAttributeManager _attributeManager;
        private readonly IFileSystemErrorView _fileSystemErrorView;
        private readonly IProgressViewFactory _progressViewFactory;

        // for testing purposes only 
        private IEntityManager _queryResult;

        public ViewerApplication(ViewerForm form)
        {
            _form = form;
            
            var factory = new AttributeStorageFactory();
            _storage = factory.Create();
            _clipboard = new ClipboardService();
            _fileSystem = new FileSystem();
            _modifiedEntities = new EntityRepository();
            _thumbnailGenerator = new ThumbnailGenerator();
            _selection = new Selection();
            _attributeManager = new AttributeManager(_selection);
            _log = new Log();
            _fileSystemErrorView = new FileSystemErrorView();
            
            // background tasks
            var tasksView = new TasksView("Background Tasks");
            tasksView.Show(_form.DockPanel, DockState.DockBottom);
            _progressViewFactory = new ProgressViewFactory(tasksView);

            // query 
            _queryResult = new EntityManager(_modifiedEntities);
            foreach (var file in Directory.EnumerateFiles(@"D:\dataset\moderate"))
            {
                _queryResult.Add(_storage.Load(file));
            }
        }

        public void InitializeLayout()
        {
            ShowFileExplorer(Resources.ExplorerWindowName);
            ShowImages(Resources.QueryResultWindowName, _queryResult);
            ShowAttributes("Attributes", true, attr => (attr.Data.Flags & AttributeFlags.ReadOnly) == 0);
            ShowAttributes("Exif", false, attr => attr.Data.GetType() != typeof(ImageAttribute) &&
                                                  (attr.Data.Flags & AttributeFlags.ReadOnly) != 0);
            ShowLog("Log");
        }

        public void ShowFileExplorer(string title)
        {
            var directoryTreeView = new DirectoryTreeView(title);
            directoryTreeView.Show(_form.DockPanel, DockState.DockLeft);

            var treePresenter = new DirectoryTreePresenter(directoryTreeView, _progressViewFactory, _fileSystemErrorView, _fileSystem, _clipboard);
            treePresenter.UpdateRootDirectories();
        }

        public void ShowImages(string title, IEntityManager entities)
        {
            var imagesView = new ImagesGridView(title);
            imagesView.Show(_form.DockPanel, DockState.Document);

            var imagesPresenter = new ImagesPresenter(imagesView, _fileSystemErrorView, _storage, _clipboard, _selection, _thumbnailGenerator, this);
            imagesPresenter.LoadFromQueryResult(entities);
        }

        public void ShowImage(string title, IEntityManager entities, int index)
        {
            var presentationView = new PresentationView(title);
            presentationView.Show(_form.DockPanel, DockState.Document);

            var presentationPresenter = new PresentationPresenter(presentationView, _selection);
            presentationPresenter.ShowEntity(entities, index);
        }

        public void ShowAttributes(string name, bool editingEnabled, Func<AttributeGroup, bool> attrPredicate)
        {
            var attributesView = new AttributeTableView(name);
            attributesView.Show(_form.DockPanel, DockState.DockRight);

            var attrPresenter = new AttributesPresenter(
                attributesView,
                _progressViewFactory,
                _selection,
                _storage,
                _attributeManager)
            {
                EditingEnabled = editingEnabled,
                AttributePredicate = attrPredicate
            };
        }

        public void ShowLog(string name)
        {
            var logView = new LogView(name);
            logView.Show(_form.DockPanel, DockState.DockBottom);

            var logPresenter = new LogPresenter(logView, _log);
        }
    }
}
