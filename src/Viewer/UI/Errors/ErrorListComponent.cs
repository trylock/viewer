using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Properties;
using Viewer.Core;
using Viewer.Core.UI;
using WeifenLuo.WinFormsUI.Docking;
using IComponent = Viewer.Core.IComponent;

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
                context.Post(state => ShowErrorList(), null);
            };
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddMenuItem(new []{ "View", Name }, () => ShowErrorList(), Resources.ErrorListIcon.ToBitmap());

            app.AddLayoutDeserializeCallback(Deserialize);
        }

        private IWindowView Deserialize(string persistString)
        {
            if (persistString == typeof(ErrorListView).FullName)
            {
                return GetErrorList().View;
            }
            return null;
        }

        private ErrorListPresenter GetErrorList()
        {
            if (_errorList == null)
            {
                _errorList = _errorListFactory.CreateExport();
                _errorList.Value.View.CloseView += (sender, e) =>
                {
                    _errorList.Dispose();
                    _errorList = null;
                };
            }
            else
            {
                _errorList.Value.View.EnsureVisible();
            }

            return _errorList.Value;
        }

        private IErrorListView ShowErrorList()
        {
            var errorList = GetErrorList();
            errorList.ShowView(Name, DockState.DockBottom);
            return errorList.View;
        }
    }
}
