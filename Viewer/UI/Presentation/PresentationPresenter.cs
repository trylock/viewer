using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;

namespace Viewer.UI.Presentation
{
    public class PresentationPresenter 
    {
        // dependencies
        private readonly IPresentationView _presentationView;
        private readonly ISelection _selection;

        // state
        private IEntityManager _entities;
        private Image _image;
        private int _entityIndex;

        public PresentationPresenter(
            IPresentationView presentationView,
            ISelection selection)
        {
            _selection = selection;
            _presentationView = presentationView;

            PresenterUtils.SubscribeTo(_presentationView, this, "View");
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
            var image = await Task.Run(() => Image.FromFile(entity.Path));

            // replace old image with the new one
            _image?.Dispose();
            _image = image;

            // update view
            _presentationView.Data = new ImageView
            {
                Entity = entity,
                Photo = image
            };
            _presentationView.UpdateImage();
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

        private void View_ViewGotFocus(object sender, EventArgs e)
        {
            _selection.Replace(_entities, new []{ _entityIndex });
        }

        private void View_CloseView(object sender, EventArgs e)
        {
            _image?.Dispose();
        }
    }
}
