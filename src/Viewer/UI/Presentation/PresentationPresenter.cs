using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.Images;
using Viewer.IO;
using Viewer.UI.Explorer;
using Viewer.UI.Images;

namespace Viewer.UI.Presentation
{
    internal class PresentationPresenter : Presenter<IPresentationView>
    {
        private readonly ISelection _selection;
        private readonly IFileSystemErrorView _dialogView;
        
        // state
        private readonly ImageWindow _window;

        /// <summary>
        /// Last time an image has changed in the presentation
        /// </summary>
        private DateTime _lastImageChange;
        
        public PresentationPresenter(
            IPresentationView view, 
            ISelection selection,
            IImageLoader imageLoader,
            IFileSystemErrorView dialogView)
        {
            _selection = selection;
            _dialogView = dialogView;
            _window = new ImageWindow(imageLoader, 3);
            View = view;
            SubscribeTo(View, "View");
        }

        private bool _isDisposed;
        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposed = true;
                View.Picture?.Dispose();
                View.Picture = null; // make sure no code can access the disposed image
                _window.Dispose();
            }

            base.Dispose(disposing);
        }

        public async Task ShowEntityAsync(IReadOnlyList<IEntity> entities, int index)
        {
            _window.Initialize(entities, index.Clamp(0, entities.Count - 1));

            await ShowCurrentImageAsync();
        }

        private async Task ShowCurrentImageAsync()
        {
            if (!_window.IsInitialized)
            {
                return;
            }

            // load current image
            var image = await _window.GetCurrentAsync();
            if (_isDisposed || image == null)
            {
                image?.Dispose();
                return;
            }

            // replace selection
            var entity = _window.Entities[_window.CurrnetIndex];
            if (View.IsActivated)
            {
                _selection.Replace(new[] { entity });
            }

            try
            {
                // update view
                View.Picture?.Dispose();
                View.Picture = image;
                View.Zoom = 1.0;
                View.UpdateImage();
            }
            catch (ArgumentException)
            {
                _dialogView.InvalidPath(entity.Path);
            }
            catch (NotSupportedException)
            {
                _dialogView.InvalidPath(entity.Path);
            }
            catch (UnauthorizedAccessException)
            {
                _dialogView.UnauthorizedAccess(entity.Path);
            }
            catch (SecurityException)
            {
                _dialogView.UnauthorizedAccess(entity.Path);
            }
            catch (PathTooLongException)
            {
                _dialogView.PathTooLong(entity.Path);
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
                    _window.Initialize(_window.Entities, _window.CurrnetIndex);
                    await ShowCurrentImageAsync();
                }
            }
        }

        private bool _isLoading = false;
        
        private async void View_NextImage(object sender, EventArgs e)
        {
            if (!_window.IsInitialized || _isLoading)
                return;

            _isLoading = true;
            try
            {
                _window.Next();
                await ShowCurrentImageAsync();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void View_PrevImage(object sender, EventArgs e)
        {
            if (!_window.IsInitialized || _isLoading)
                return;

            _isLoading = true;
            try
            {
                _window.Previous();
                await ShowCurrentImageAsync();
            }
            finally
            {
                _isLoading = false;
            }
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

        private void View_ResetZoom(object sender, EventArgs e)
        {
            View.Zoom = 1.0;
            View.UpdateImage();
        }
    }
}
