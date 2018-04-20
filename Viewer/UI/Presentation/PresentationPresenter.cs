using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.UI.Images;

namespace Viewer.UI.Presentation
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class PresentationPresenter : Presenter<IPresentationView>
    {
        private readonly ISelection _selection;
        private readonly IImageLoader _imageLoader;

        protected override ExportLifetimeContext<IPresentationView> ViewLifetime { get; }

        // state
        private IEntityManager _entities;
        private Image _image;
        private int _entityIndex;

        /// <summary>
        /// Last time an image was changed in the presentation
        /// </summary>
        private DateTime _lastImageChange;
        
        [ImportingConstructor]
        public PresentationPresenter(
            ExportFactory<IPresentationView> viewFactory, 
            ISelection selection,
            IImageLoader imageLoader)
        {
            _selection = selection;
            _imageLoader = imageLoader;
            ViewLifetime = viewFactory.CreateExport();
            SubscribeTo(View, "View");
        }

        public async void ShowEntity(IEntityManager entities, int index)
        {
            _entities = entities;
            _entityIndex = index;
            await LoadCurrentEntityAsync();
        }
        
        private async Task LoadCurrentEntityAsync()
        {
            // replace selection
            _selection.Replace(_entities, new[]{ _entityIndex });

            // load new image
            var entity = _entities[_entityIndex];
            var image = await Task.Run(() => _imageLoader.LoadImage(entity));

            // replace old image with the new one
            _image?.Dispose();
            _image = image;

            // update view
            View.Picture = _image;
            View.UpdateImage();
        }
        
        private async void View_NextImage(object sender, EventArgs e)
        {
            _entityIndex = (_entityIndex + 1) % _entities.Count;
            await LoadCurrentEntityAsync();
        }

        private async void View_PrevImage(object sender, EventArgs e)
        {
            --_entityIndex;
            if (_entityIndex < 0)
                _entityIndex = _entities.Count - 1;
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
            _selection.Replace(_entities, new []{ _entityIndex });
        }

        private void View_CloseView(object sender, EventArgs e)
        {
            _selection.Clear();
            _image?.Dispose();
        }
    }
}
