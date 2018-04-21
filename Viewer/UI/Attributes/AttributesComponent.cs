using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Attributes
{
    [Export(typeof(IComponent))]
    public class AttributesComponent : IComponent
    {
        private readonly ExportFactory<AttributesPresenter> _attributesFactory;

        [ImportingConstructor]
        public AttributesComponent(ExportFactory<AttributesPresenter> factory)
        {
            _attributesFactory = factory;
        }

        public void OnStartup()
        {
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
        }
    }
}
