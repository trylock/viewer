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

        /// <summary>
        /// Number of loaded images at any given time
        /// </summary>
        public const int WindowSize = 5;

        // state
        private readonly List<IEntity> _entities = new List<IEntity>();
        private ImageWindow _window;

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
            _window = new ImageWindow(_imageLoader, _entities, WindowSize);
            await _window.LoadPositionAsync(index);
            await LoadCurrentEntityAsync();
        }
        
        private async Task LoadCurrentEntityAsync()
        {
            // replace selection
            _selection.Replace(new[]{ _entities[_window.Index] });

            // load new image
            var entity = _entities[_window.Index];
            try
            {
                var image = await _window.Current;
                
                // update view
                View.Zoom = 1.0;
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
        }
        
        private async void View_NextImage(object sender, EventArgs e)
        {
            _window.TryToMoveForward();
            await LoadCurrentEntityAsync();
        }

        private async void View_PrevImage(object sender, EventArgs e)
        {
            _window.TryToMoveBackward();
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
            _selection.Replace(new []{ _entities[_window.Index] });
        }

        private void View_CloseView(object sender, EventArgs e)
        {
            _selection.Clear();
            _window?.Dispose();
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
