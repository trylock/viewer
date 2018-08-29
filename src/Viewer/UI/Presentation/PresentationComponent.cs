using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Data;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Presentation
{
    [Export(typeof(IPresentation))]
    [Export(typeof(IComponent))]
    public class PresentationComponent : IComponent, IPresentation
    {
        private readonly ExportFactory<PresentationPresenter> _presentationFactory;

        [ImportingConstructor]
        public PresentationComponent(ExportFactory<PresentationPresenter> presentationFactory)
        {
            _presentationFactory = presentationFactory;
        }

        public void OnStartup(IViewerApplication app)
        {
        }

        private void ShowPresentation(IEnumerable<IEntity> entities, int index)
        {
            var presentationExport = _presentationFactory.CreateExport();
            presentationExport.Value.View.CloseView += (s, args) =>
            {
                presentationExport.Dispose();
                presentationExport = null;
            };
            presentationExport.Value.ShowView("Presentation", DockState.Document);
            presentationExport.Value.ShowEntity(entities, index);
        }

        public void Open(IEnumerable<IEntity> entities, int activeIndex)
        {
            ShowPresentation(entities, activeIndex);
        }
    }
}
