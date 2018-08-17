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
using System.Windows.Forms;
using SkiaSharp.Views.Desktop;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.Images;
using Viewer.IO;
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
        private readonly IFileSystem _fileSystem;
        private readonly IFileSystemErrorView _dialogView;

        protected override ExportLifetimeContext<IPresentationView> ViewLifetime { get; }

        // state
        private IReadOnlyList<IEntity> _entities;
        private ImageWindow _images;

        /// <summary>
        /// Last time an image was changed in the presentation
        /// </summary>
        private DateTime _lastImageChange;
        
        [ImportingConstructor]
        public PresentationPresenter(
            ExportFactory<IPresentationView> viewFactory, 
            ISelection selection,
            IImageLoader imageLoader,
            IFileSystem fileSystem,
            IFileSystemErrorView dialogView)
        {
            _selection = selection;
            _imageLoader = imageLoader;
            _dialogView = dialogView;
            _fileSystem = fileSystem;
            ViewLifetime = viewFactory.CreateExport();
            SubscribeTo(View, "View");
        }

        public override void Dispose()
        {
            View.Picture?.Dispose();
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

            _images?.Dispose();
            _images = new ImageWindow(_imageLoader, _fileSystem, _entities, 5);
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
                var image = await _images.GetCurrentAsync(CancellationToken.None);

                // update view
                View.Zoom = 1.0;
                View.Picture?.Dispose();
                View.Picture = image;
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
                    await LoadCurrentEntityAsync();
                }
            }
        }
        
        private async void View_NextImage(object sender, EventArgs e)
        {
            _images.Next();
            await LoadCurrentEntityAsync();
        }

        private async void View_PrevImage(object sender, EventArgs e)
        {
            _images.Previous();
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
