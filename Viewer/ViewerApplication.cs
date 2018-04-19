using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
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

namespace Viewer
{
    [Export]
    public class ViewerApplication
    {
        private readonly ViewerForm _appForm;
        private readonly IAttributeStorage _storage;
        private readonly IEntityRepository _modifiedEntities;

        private readonly ExportFactory<ImagesPresenter> _imagesFactory;
        private readonly ExportFactory<DirectoryTreePresenter> _explorerFactory;
        private readonly ExportFactory<AttributesPresenter> _attributesFactory;
        private readonly ExportFactory<LogPresenter> _logFactory;
        
        [ImportingConstructor]
        public ViewerApplication(
            ViewerForm appForm, 
            IAttributeStorage storage,
            IEntityRepository modifiedEntities,
            ExportFactory<ImagesPresenter> images,
            ExportFactory<DirectoryTreePresenter> explorer,
            ExportFactory<AttributesPresenter> attributesFactory,
            ExportFactory<LogPresenter> log)
        {
            _appForm = appForm;
            _storage = storage;
            _modifiedEntities = modifiedEntities;
            _imagesFactory = images;
            _explorerFactory = explorer;
            _attributesFactory = attributesFactory;
            _logFactory = log;
        }
        
        public void InitializeLayout()
        {
            var queryResult = new EntityManager(_modifiedEntities);
            foreach (var file in Directory.EnumerateFiles(@"D:\dataset\large"))
            {
                queryResult.Add(_storage.Load(file));
            }

            var imagesExport = _imagesFactory.CreateExport();
            imagesExport.Value.LoadFromQueryResult(queryResult);
            imagesExport.Value.View.CloseView += (sender, args) =>
            {
                imagesExport.Dispose();
                imagesExport = null;
            };
            imagesExport.Value.ShowView("Images", DockState.Document);

            var explorerExport = _explorerFactory.CreateExport();
            explorerExport.Value.UpdateRootDirectories();
            explorerExport.Value.View.CloseView += (sender, args) =>
            {
                explorerExport.Dispose();
                explorerExport = null;
            };
            explorerExport.Value.ShowView("Explorer", DockState.DockLeft);

            var mainAttributesExport = _attributesFactory.CreateExport();
            mainAttributesExport.Value.AttributePredicate = attr => (attr.Data.Flags & AttributeFlags.ReadOnly) == 0;
            mainAttributesExport.Value.EditingEnabled = true;
            mainAttributesExport.Value.View.CloseView += (sender, args) =>
            {
                mainAttributesExport.Dispose();
                mainAttributesExport = null;
            };
            mainAttributesExport.Value.ShowView("Attributes", DockState.DockRight);
            
            var exifAttributesExport = _attributesFactory.CreateExport();
            exifAttributesExport.Value.AttributePredicate = attr => attr.Data.GetType() != typeof(ImageAttribute) &&
                                                         (attr.Data.Flags & AttributeFlags.ReadOnly) != 0;
            exifAttributesExport.Value.EditingEnabled = false;
            exifAttributesExport.Value.View.CloseView += (sender, e) =>
            {
                exifAttributesExport.Dispose();
                exifAttributesExport = null;
            };
            exifAttributesExport.Value.ShowView("Exif", DockState.DockRight);

            var logExport = _logFactory.CreateExport();
            logExport.Value.View.CloseView += (sender, e) =>
            {
                logExport.Dispose();
                logExport = null;
            };
            logExport.Value.ShowView("Event Log", DockState.DockBottom);
        }

        public void Run()
        {
            Application.Run(_appForm);
        }
    }
}
