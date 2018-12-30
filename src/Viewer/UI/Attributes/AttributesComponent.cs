using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.Localization;
using Viewer.Properties;
using Viewer.UI.Errors;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Attributes
{
    [Export(typeof(IComponent))]
    public class AttributesComponent : Component
    {
        private readonly IAttributeManager _attributes;
        private readonly IEntityManager _entityManager;
        private readonly IAttributeCache _attributeCache;
        private readonly IQueryHistory _queryHistory;
        
        private const string AttributesId = "attributes";
        private const string ExifId = "exif";
        
        private AttributesPresenter _userAttrPresenter;
        private AttributesPresenter _exifAttrPresenter;

        [ImportingConstructor]
        public AttributesComponent(
            IAttributeManager attributes, 
            IEntityManager entities, 
            IAttributeCache attributeCache,
            IQueryHistory queryHistory)
        {
            _attributes = attributes;
            _entityManager = entities;
            _attributeCache = attributeCache;
            _queryHistory = queryHistory;
        }

        public override void OnStartup(IViewerApplication app)
        {
            // add the component to the menu
            app.AddMenuItem(new []{ Strings.View_Label, Strings.Attributes_label }, 
                () => ShowAttributes(), 
                Resources.AttributesComponentIcon.ToBitmap());
            app.AddMenuItem(new[] { Strings.View_Label, "Exif" }, 
                () => ShowExif(), 
                Resources.ExifComponentIcon.ToBitmap());

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

        private AttributesPresenter CreateAttributesPresenter()
        {
            return new AttributesPresenter(
                new AttributeTableView(), 
                _attributes, 
                _entityManager, 
                _attributeCache,
                _queryHistory);
        }

        private AttributesPresenter GetAttributes()
        {
            if (_userAttrPresenter == null)
            {
                _userAttrPresenter = CreateAttributesPresenter();
                _userAttrPresenter.SetType(AttributeViewType.Custom);
                _userAttrPresenter.View.Text = Strings.Attributes_label;
                _userAttrPresenter.View.CloseView += (sender, args) =>
                {
                    _userAttrPresenter.Dispose();
                    _userAttrPresenter = null;
                };
            }
            else
            {
                _userAttrPresenter.View.EnsureVisible();
            }

            return _userAttrPresenter;
        }

        private IDockContent ShowAttributes()
        {
            var attributes = GetAttributes();
            attributes.View.Text = Strings.Attributes_label;
            attributes.View.Show(Application.Panel, DockState.DockRight);
            return attributes.View;
        }

        private AttributesPresenter GetExif()
        {
            if (_exifAttrPresenter == null)
            {
                _exifAttrPresenter = CreateAttributesPresenter();
                _exifAttrPresenter.SetType(AttributeViewType.Exif);
                _exifAttrPresenter.View.Text = "Exif";
                _exifAttrPresenter.View.CloseView += (sender, e) =>
                {
                    _exifAttrPresenter.Dispose();
                    _exifAttrPresenter = null;
                };
            }
            else
            {
                _exifAttrPresenter.View.EnsureVisible();
            }

            return _exifAttrPresenter;
        }

        private IDockContent ShowExif()
        {
            var exif = GetExif();
            exif.View.Text = "Exif";
            exif.View.Show(Application.Panel, DockState.DockRight);
            return exif.View;
        }
    }
}
