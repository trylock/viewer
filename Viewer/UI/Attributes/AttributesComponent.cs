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

        private ExportLifetimeContext<AttributesPresenter> _attributes;
        private ExportLifetimeContext<AttributesPresenter> _exif;

        [ImportingConstructor]
        public AttributesComponent(ExportFactory<AttributesPresenter> factory)
        {
            _attributesFactory = factory;
        }

        public void OnStartup(IViewerApplication app)
        {
            // add the component to the menu
            app.AddViewAction("Attributes", ShowAttributes);
            app.AddViewAction("Exif", ShowExif);

            // create default panel
            ShowAttributes();
            ShowExif();
        }

        private void ShowAttributes()
        {
            if (_attributes == null)
            {
                _attributes = _attributesFactory.CreateExport();
                _attributes.Value.AttributePredicate =
                    attr => (attr.Data.Flags & AttributeFlags.ReadOnly) == 0;
                _attributes.Value.EditingEnabled = true;
                _attributes.Value.View.CloseView += (sender, args) =>
                {
                    _attributes.Dispose();
                    _attributes = null;
                };
                _attributes.Value.ShowView("Attributes", DockState.DockRight);
            }
            else
            {
                _attributes.Value.View.EnsureVisible();
            }
        }

        private void ShowExif()
        {
            if (_exif == null)
            {
                _exif = _attributesFactory.CreateExport();
                _exif.Value.AttributePredicate = attr => attr.Data.GetType() != typeof(ImageAttribute) &&
                                                        (attr.Data.Flags & AttributeFlags.ReadOnly) != 0;
                _exif.Value.EditingEnabled = false;
                _exif.Value.View.CloseView += (sender, e) =>
                {
                    _exif.Dispose();
                    _exif = null;
                };
                _exif.Value.ShowView("Exif", DockState.DockRight);
            }
            else
            {
                _exif.Value.View.EnsureVisible();
            }
        }
    }
}
