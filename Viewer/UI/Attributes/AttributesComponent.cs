using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Properties;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Attributes
{
    [Export(typeof(IComponent))]
    public class AttributesComponent : IComponent
    {
        private const string AttributesId = "attributes";
        private const string ExifId = "exif";

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
            app.AddViewAction("Attributes", () => ShowAttributes(), Resources.AttributesComponentIcon.ToBitmap());
            app.AddViewAction("Exif", () => ShowExif(), Resources.ExifComponentIcon.ToBitmap());
        }

        public IDockContent Deserialize(string persistString)
        {
            if (persistString == typeof(AttributeTableView).FullName + ";" + AttributesId)
            {
                return ShowAttributes();
            }
            else if (persistString == typeof(AttributeTableView).FullName + ";" + ExifId)
            {
                return ShowExif();
            }

            return null;
        }

        private IDockContent ShowAttributes()
        {
            if (_attributes == null)
            {
                _attributes = _attributesFactory.CreateExport();
                _attributes.Value.SetType(AttributeViewType.Custom);
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

            return _attributes.Value.View;
        }

        private IDockContent ShowExif()
        {
            if (_exif == null)
            {
                _exif = _attributesFactory.CreateExport();
                _exif.Value.SetType(AttributeViewType.Exif);
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

            return _exif.Value.View;
        }
    }
}
