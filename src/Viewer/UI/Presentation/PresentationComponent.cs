using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.Data.Formats;
using Viewer.Images;
using Viewer.Localization;
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
        private readonly IEntityManager _entities;
        private readonly IFileSystemErrorView _dialogView;
        
        private PresentationPresenter _presenter;

        [ImportingConstructor]
        public PresentationComponent(
            ISelection selection,
            IImageLoader imageLoader,
            IEntityManager entities,
            IFileSystemErrorView dialogView)
        {
            _entities = entities;
            _selection = selection;
            _imageLoader = imageLoader;
            _dialogView = dialogView;
        }

        public override void OnStartup(IViewerApplication app)
        {
            app.AddLayoutDeserializeCallback(Deserialize);
        }

        public override async void OnInitialized()
        {
            base.OnInitialized();
            
            if (Application.Arguments.Length <= 1)
            {
                return;
            }

            // if this is a path to a jpeg file, try to open it 
            var path = Application.Arguments[1];
            var extension = Path.GetExtension(path);
            if (!string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            try
            {
                var entity = await Task.Run(() => _entities.GetEntity(path));
                await OpenAsync(Enumerable.Repeat(entity, 1), 0);
            }
            catch (ArgumentException) // invalid path
            {
                // ignore
            }
            catch (InvalidDataFormatException)
            {
                // ignore
            }
            catch (PathTooLongException)
            {
                _dialogView.PathTooLong(path);
            }
            catch (IOException e)
            {
                _dialogView.FailedToOpenFile(path, e.Message);
            }
            catch (NotSupportedException)
            {
                // ignore
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
            catch (SecurityException)
            {
                _dialogView.UnauthorizedAccess(path);
            }
        }

        private IWindowView Deserialize(string persiststring)
        {
            if (persiststring == typeof(PresentationView).FullName)
            {
                return GetPresenter().View;
            }

            return null;
        }

        private PresentationPresenter GetPresenter()
        {
            if (_presenter != null)
            {
                return _presenter;
            }

            _presenter = new PresentationPresenter(
                new PresentationView(), _selection, _imageLoader, _dialogView);
            _presenter.View.Text = Strings.Presentation_Label;
            _presenter.View.CloseView += (s, args) =>
            {
                _presenter.Dispose();
                _presenter = null;
            };
            return _presenter;
        }
        
        private async Task ShowPresentationAsync(IEnumerable<IEntity> entities, int index)
        {
            if (_presenter == null)
            {
                _presenter = GetPresenter();
                _presenter.View.Show(Application.Panel, DockState.Document);
            }
            else
            {
                _presenter.View.EnsureVisible();
            }
            await _presenter.ShowEntityAsync(entities.ToList(), index);
        }
        
        public async void Open(IEnumerable<IEntity> entities, int activeIndex)
        {
            await OpenAsync(entities, activeIndex);
        }

        public async Task OpenAsync(IEnumerable<IEntity> entities, int activeIndex)
        {
            await ShowPresentationAsync(entities, activeIndex);
        }

        public async Task PreviewAsync(IEnumerable<IEntity> entities, int activeIndex)
        {
            if (_presenter != null)
            {
                await _presenter.ShowEntityAsync(entities.ToList(), activeIndex);
            }
        }
    }
}
