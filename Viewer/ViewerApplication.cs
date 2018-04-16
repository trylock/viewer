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
    public class ViewerApplication : IPartImportsSatisfiedNotification
    {
        [Import] private ViewerForm _appForm;
        [Import] private IAttributeStorage _storage;
        [Import] private IEntityRepository _modifiedEntities;

        [Import] private ImagesPresenter _images;
        [Import] private DirectoryTreePresenter _explorer;
        [Import] private AttributesPresenter _mainAttributes;
        [Import] private AttributesPresenter _exifAttributes;
        [Import] private PresentationPresenter _presentation;
        [Import] private LogPresenter _log;
        
        // for testing purposes only 
        private IEntityManager _queryResult;
        
        public void OnImportsSatisfied()
        {
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

            _mainAttributes.AttributePredicate = attr => (attr.Data.Flags & AttributeFlags.ReadOnly) == 0;
            _mainAttributes.EditingEnabled = true;
            _mainAttributes.ShowView(DockState.DockRight);

            _exifAttributes.AttributePredicate = attr => attr.Data.GetType() != typeof(ImageAttribute) &&
                                                         (attr.Data.Flags & AttributeFlags.ReadOnly) != 0;
            _exifAttributes.EditingEnabled = false;
            _exifAttributes.ShowView(DockState.DockRight);

            _log.ShowView(DockState.DockBottom);
        }

        public void Run()
        {
            Application.Run(_appForm);
        }
    }
}
