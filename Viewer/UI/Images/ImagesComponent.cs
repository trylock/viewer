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
        private readonly IAttributeStorage _storage;
        private readonly IEntityRepository _modifiedEntities;
        private readonly ExportFactory<ImagesPresenter> _imagesFactory;

        [ImportingConstructor]
        public ImagesComponent(
            IAttributeStorage storage,
            IEntityRepository modifiedEntities,
            ExportFactory<ImagesPresenter> images)
        {
            _storage = storage;
            _modifiedEntities = modifiedEntities;
            _imagesFactory = images;
        }

        public void OnStartup()
        {
            var queryResult = new EntityManager(_modifiedEntities);
            foreach (var file in Directory.EnumerateFiles(@"D:\dataset\large"))
            {
                queryResult.Add(_storage.Load(file));
            }

            var imagesExport = _imagesFactory.CreateExport();
            imagesExport.Value.LoadFromQueryResult(queryResult);
            imagesExport.Value.View.CloseView += (sender, args) =>
            {
                imagesExport.Dispose();
                imagesExport = null;
            };
            imagesExport.Value.ShowView("Images", DockState.Document);
        }
    }
}
