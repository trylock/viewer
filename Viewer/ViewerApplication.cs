using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;
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
using Attribute = Viewer.Data.Attribute;

namespace Viewer
{
    [Export]
    public class ViewerApplication
    {
        private readonly ViewerForm _appForm;
        private readonly IAttributeStorage _storage;
        private readonly IEntityRepository _modifiedEntities;

        private readonly ImagesPresenter _images;
        private readonly DirectoryTreePresenter _explorer;
        private readonly ExportFactory<AttributesPresenter> _attrPresenterFactory;
        private readonly LogPresenter _log;
        
        // for testing purposes only 
        private IEntityManager _queryResult;
        
        [ImportingConstructor]
        public ViewerApplication(
            ViewerForm appForm, 
            IAttributeStorage storage,
            IEntityRepository modifiedEntities,
            ImagesPresenter images,
            DirectoryTreePresenter explorer,
            ExportFactory<AttributesPresenter> attrPresenterFactory,
            LogPresenter log)
        {
            _appForm = appForm;
            _storage = storage;
            _modifiedEntities = modifiedEntities;
            _images = images;
            _explorer = explorer;
            _attrPresenterFactory = attrPresenterFactory;
            _log = log;

            _queryResult = new EntityManager(_modifiedEntities);
            foreach (var file in Directory.EnumerateFiles(@"D:\dataset\moderate"))
            {
                _queryResult.Add(_storage.Load(file));
            }
        }
        
        public void InitializeLayout()
        {
            _images.LoadFromQueryResult(_queryResult);
            _images.ShowView(DockState.Document);

            _explorer.UpdateRootDirectories();
            _explorer.ShowView(DockState.DockLeft);

            var mainAttributes = _attrPresenterFactory.CreateExport().Value;
            mainAttributes.AttributePredicate = attr => (attr.Data.Flags & AttributeFlags.ReadOnly) == 0;
            mainAttributes.EditingEnabled = true;
            mainAttributes.ShowView(DockState.DockRight);

            var exifAttributes = _attrPresenterFactory.CreateExport().Value;
            exifAttributes.AttributePredicate = attr => attr.Data.GetType() != typeof(ImageAttribute) &&
                                                         (attr.Data.Flags & AttributeFlags.ReadOnly) != 0;
            exifAttributes.EditingEnabled = false;
            exifAttributes.ShowView(DockState.DockRight);

            _log.ShowView(DockState.DockBottom);
        }

        public void Run()
        {
            Application.Run(_appForm);
        }
    }
}
