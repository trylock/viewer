using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.Images;
using Viewer.UI.Explorer;
using Viewer.UI.Images;

namespace Viewer.UI.Presentation
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PresentationPresenter : Presenter<IPresentationView>
    {
        private readonly ISelection _selection;
        private readonly IImageLoader _imageLoader;
        private readonly IFileSystemErrorView _dialogView;

        protected override ExportLifetimeContext<IPresentationView> ViewLifetime { get; }

        // state
        private readonly List<IEntity> _entities = new List<IEntity>();
        private int _position;

        /// <summary>
        /// Last time an image was changed in the presentation
        /// </summary>
        private DateTime _lastImageChange;
        
        [ImportingConstructor]
        public PresentationPresenter(
            ExportFactory<IPresentationView> viewFactory, 
            ISelection selection,
            IImageLoader imageLoader,
            IFileSystemErrorView dialogView)
        {
            _selection = selection;
            _imageLoader = imageLoader;
            _dialogView = dialogView;
            ViewLifetime = viewFactory.CreateExport();
            SubscribeTo(View, "View");
        }

        public async void ShowEntity(IEnumerable<IEntity> entities, int index)
        {
            _entities.Clear();
            _entities.AddRange(entities);
            _position = index;
            await LoadCurrentEntityAsync();
        }
        
        private async Task LoadCurrentEntityAsync()
        {
            // replace selection
            var position = _position;
            var entity = _entities[position];
            _selection.Replace(new[]
            {
                entity
            });

            // load new image
            try
            {
                var image = await _imageLoader.LoadImageAsync(entity);

                // update view
                View.Zoom = 1.0;
                View.Picture?.Dispose();
                View.Picture = image;
                View.UpdateImage();
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(entity.Path);
            }
            catch (SecurityException)
            {
                _dialogView.UnauthorizedAccess(entity.Path);
            }
            catch (FileNotFoundException)
            {
                _dialogView.FileNotFound(entity.Path);
            }
            catch (IOException e)
            {
                var confirmRetry = _dialogView.FailedToOpenFile(entity.Path, e.Message);
                if (confirmRetry)
                {
                    await LoadCurrentEntityAsync();
                }
            }
        }
        
        private async void View_NextImage(object sender, EventArgs e)
        {
            _position = (_position + 1) % _entities.Count;
            await LoadCurrentEntityAsync();
        }

        private async void View_PrevImage(object sender, EventArgs e)
        {
            --_position;
            if (_position < 0)
            {
                _position = _entities.Count - 1;
            }
            await LoadCurrentEntityAsync();
        }

        private void View_ToggleFullscreen(object sender, EventArgs e)
        {
            View.IsFullscreen = !View.IsFullscreen;
        }
        
        private void View_ExitFullscreen(object sender, EventArgs e)
        {
            View.IsFullscreen = false;
        }

        private void View_PlayPausePresentation(object sender, EventArgs e)
        {
            View.IsPlaying = !View.IsPlaying;
        }

        private void View_TimerTick(object sender, EventArgs e)
        {
            if (!View.IsPlaying)
            {
                return;
            }

            var elapsed = DateTime.Now - _lastImageChange;
            if (elapsed.TotalMilliseconds >= View.Speed)
            {
                View_NextImage(sender, e);
                _lastImageChange = DateTime.Now;
            }
        }

        private void View_ViewGotFocus(object sender, EventArgs e)
        {
            if (_entities.Count == 0)
            {
                return;
            }
            _selection.Replace(new []{ _entities[_position] });
        }

        private void View_CloseView(object sender, EventArgs e)
        {
            _selection.Clear();
        }

        private const double ScaleStep = 1.8;
        private const int StepCount = 5;

        private void View_ZoomIn(object sender, EventArgs e)
        {
            View.Zoom = Math.Min(View.Zoom * ScaleStep, Math.Pow(ScaleStep, StepCount));
            View.UpdateImage();
        }

        private void View_ZoomOut(object sender, EventArgs e)
        {
            View.Zoom = Math.Max(View.Zoom / ScaleStep, Math.Pow(ScaleStep, -StepCount));
            View.UpdateImage();
        }
    }
}
