using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Errors
{
    [Export(typeof(IComponent))]
    public class ErrorListComponent : IComponent
    {
        public const string Name = "Error List";

        private readonly ExportFactory<ErrorListPresenter> _errorListFactory;

        private ExportLifetimeContext<ErrorListPresenter> _errorList;

        [ImportingConstructor]
        public ErrorListComponent(ExportFactory<ErrorListPresenter> factory, IErrorList errorList)
        {
            _errorListFactory = factory;

            var context = SynchronizationContext.Current;
            errorList.EntryAdded += (sender, args) =>
            {
                context.Post(state => ShowLog(), null);
            };
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddViewAction(Name, ShowLog);
        }

        public IDockContent Deserialize(string persistString)
        {
            return null;
        }

        private void ShowLog()
        {
            if (_errorList == null)
            {
                _errorList = _errorListFactory.CreateExport();
                _errorList.Value.View.CloseView += (sender, e) =>
                {
                    _errorList.Dispose();
                    _errorList = null;
                };
                _errorList.Value.ShowView(Name, DockState.DockBottom);
            }
            else
            {
                _errorList.Value.View.EnsureVisible();
            }
        }
    }
}
