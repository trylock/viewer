using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Data;
using Viewer.Images;
using Viewer.UI.Explorer;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Presentation
{
    [Export(typeof(IPresentation))]
    [Export(typeof(IComponent))]
    public class PresentationComponent : Component, IPresentation
    {
        private readonly ISelection _selection;
        private readonly IImageLoader _imageLoader;
        private readonly IFileSystemErrorView _dialogView;
        
        private PresentationPresenter _presenter;

        [ImportingConstructor]
        public PresentationComponent(
            ISelection selection,
            IImageLoader imageLoader,
            IFileSystemErrorView dialogView)
        {
            _selection = selection;
            _imageLoader = imageLoader;
            _dialogView = dialogView;
        }

        public override void OnStartup(IViewerApplication app)
        {
        }

        private void ShowPresentation(IEnumerable<IEntity> entities, int index)
        {
            _presenter = new PresentationPresenter(new PresentationView(), _selection, _imageLoader, _dialogView);
            _presenter.View.CloseView += (s, args) =>
            {
                _presenter.Dispose();
                _presenter = null;
            };
            _presenter.View.Show(Application.Panel, DockState.Document);
            _presenter.ShowEntity(entities, index);
        }

        public void Open(IEnumerable<IEntity> entities, int activeIndex)
        {
            ShowPresentation(entities, activeIndex);
        }
    }
}
