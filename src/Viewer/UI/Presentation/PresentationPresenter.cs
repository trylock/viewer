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
        private readonly IImageLoader _imageLoader;
        private readonly IFileSystemErrorView _dialogView;
        
        // state
        private IReadOnlyList<IEntity> _entities;
        private ImageWindow _images;

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
            _imageLoader = imageLoader;
            _dialogView = dialogView;
            View = view;
            SubscribeTo(View, "View");
        }

        public override void Dispose()
        {
            View.Picture?.Dispose();
            View.Picture = null; // make sure no code can access the disposed image
            _images?.Dispose();
            base.Dispose();
        }

        public async void ShowEntity(IEnumerable<IEntity> entities, int index)
        {
            _entities = entities.ToArray();
            if (index < 0)
                index = 0;
            if (index >= _entities.Count)
                index = _entities.Count - 1;

            View.Picture?.Dispose();
            View.Picture = null;
            _images?.Dispose();
            _images = new ImageWindow(_imageLoader, _entities, 3);
            _images.SetPosition(index);
            await LoadCurrentEntityAsync();
        }
        
        private async Task LoadCurrentEntityAsync()
        {
            if (_images == null)
            {
                return;
            }

            // replace selection
            var position = _images.CurrnetIndex;
            var entity = _entities[position];
            _selection.Replace(new[] { entity });

            // load new image
            try
            {
                var image = await _images.GetCurrentAsync();
                if (image == null)
                {
                    return;
                }

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
                    _images.SetPosition(_images.CurrnetIndex);
                    await LoadCurrentEntityAsync();
                }
            }
        }

        private bool _isLoading = false;
        
        private async void View_NextImage(object sender, EventArgs e)
        {
            if (_isLoading)
                return;

            _isLoading = true;
            try
            {
                _images.Next();
                await LoadCurrentEntityAsync();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private async void View_PrevImage(object sender, EventArgs e)
        {
            if (_isLoading)
                return;

            _isLoading = true;
            try
            {
                _images.Previous();
                await LoadCurrentEntityAsync();
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

        private void View_ViewGotFocus(object sender, EventArgs e)
        {
            if (_entities == null || _entities.Count == 0)
            {
                return;
            }
            _selection.Replace(new []{ _entities[_images.CurrnetIndex] });
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
