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
    public class ErrorListComponent : Component
    {
        public const string Name = "Error List";

        private readonly IErrorList _errorList;
        
        private ErrorListPresenter _errorListPresenter;

        [ImportingConstructor]
        public ErrorListComponent(IErrorList errorList)
        {
            var context = SynchronizationContext.Current;
            _errorList = errorList;
            _errorList.EntryAdded += (sender, args) =>
            {
                context.Post(state => ShowErrorList(), null);
            };
        }

        public override void OnStartup(IViewerApplication app)
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
            if (_errorListPresenter == null)
            {
                _errorListPresenter = new ErrorListPresenter(new ErrorListView(), _errorList);
                _errorListPresenter.View.CloseView += (sender, e) =>
                {
                    _errorListPresenter.Dispose();
                    _errorListPresenter = null;
                };
            }
            else
            {
                _errorListPresenter.View.EnsureVisible();
            }

            return _errorListPresenter;
        }

        private IErrorListView ShowErrorList()
        {
            var errorList = GetErrorList();
            errorList.View.Show(Application.Panel, DockState.DockBottom);
            return errorList.View;
        }
    }
}
