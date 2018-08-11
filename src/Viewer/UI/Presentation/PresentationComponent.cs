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
    [Export(typeof(IComponent))]
    public class PresentationComponent : IComponent
    {
        private readonly IQueryEvents _state;
        private readonly ExportFactory<PresentationPresenter> _presentationFactory;

        [ImportingConstructor]
        public PresentationComponent(IQueryEvents state, ExportFactory<PresentationPresenter> presentationFactory)
        {
            _presentationFactory = presentationFactory;
            _state = state;
            _state.EntityOpened += (sender, e) => ShowPresentation(e.Entities, e.Index);
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
    }
}
