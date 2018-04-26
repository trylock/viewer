using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Data.Storage;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Images
{
    [Export(typeof(IComponent))]
    public class ImagesComponent : IComponent
    {
        private readonly IApplicationState _state;
        private readonly ExportFactory<ImagesPresenter> _imagesFactory;

        private ExportLifetimeContext<ImagesPresenter> _images;

        [ImportingConstructor]
        public ImagesComponent(IApplicationState state, ExportFactory<ImagesPresenter> images)
        {
            _imagesFactory = images;
            _state = state;
            _state.QueryExecuted += (sender, e) => ShowImages(e.Query);
        }

        public void OnStartup(IViewerApplication app)
        {
        }

        private void ShowImages(IQuery query)
        {
            if (_images == null)
            {
                _images = _imagesFactory.CreateExport();
                _images.Value.View.CloseView += (sender, args) =>
                {
                    _images.Dispose();
                    _images = null;
                };
                _images.Value.ShowView("Images", DockState.Document);
                _images.Value.LoadQueryAsync(query);
            }
            else
            {
                _images.Value.View.EnsureVisible();
                _images.Value.LoadQueryAsync(query);
            }
        }
    }
}
