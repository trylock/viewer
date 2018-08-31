using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Core.UI;
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
            app.AddMenuItem(new []{ "View", "Attributes" }, () => ShowAttributes(), Resources.AttributesComponentIcon.ToBitmap());
            app.AddMenuItem(new[] { "View", "Exif" }, () => ShowExif(), Resources.ExifComponentIcon.ToBitmap());

            app.AddLayoutDeserializeCallback(Deserialize);
        }

        private IWindowView Deserialize(string persistString)
        {
            if (persistString == typeof(AttributeTableView).FullName + ";" + AttributesId)
            {
                return GetAttributes().View;
            }
            else if (persistString == typeof(AttributeTableView).FullName + ";" + ExifId)
            {
                return GetExif().View;
            }

            return null;
        }

        private AttributesPresenter GetAttributes()
        {
            if (_attributes == null)
            {
                _attributes = _attributesFactory.CreateExport();
                _attributes.Value.SetType(AttributeViewType.Custom);
                _attributes.Value.View.Text = "Attributes";
                _attributes.Value.View.CloseView += (sender, args) =>
                {
                    _attributes.Dispose();
                    _attributes = null;
                };
            }
            else
            {
                _attributes.Value.View.EnsureVisible();
            }

            return _attributes.Value;
        }

        private IDockContent ShowAttributes()
        {
            var attributes = GetAttributes();
            attributes.ShowView("Attributes", DockState.DockRight);
            return attributes.View;
        }

        private AttributesPresenter GetExif()
        {
            if (_exif == null)
            {
                _exif = _attributesFactory.CreateExport();
                _exif.Value.SetType(AttributeViewType.Exif);
                _exif.Value.View.Text = "Exif";
                _exif.Value.View.CloseView += (sender, e) =>
                {
                    _exif.Dispose();
                    _exif = null;
                };
            }
            else
            {
                _exif.Value.View.EnsureVisible();
            }

            return _exif.Value;
        }

        private IDockContent ShowExif()
        {
            var exif = GetExif();
            exif.ShowView("Exif", DockState.DockRight);
            return exif.View;
        }
    }
}
