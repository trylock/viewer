using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Log
{
    [Export(typeof(IComponent))]
    public class LogComponent : IComponent
    {
        public const string Name = "Event Log";

        private readonly ExportFactory<LogPresenter> _logFactory;

        private ExportLifetimeContext<LogPresenter> _log;

        [ImportingConstructor]
        public LogComponent(ExportFactory<LogPresenter> factory)
        {
            _logFactory = factory;
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddViewAction(Name, ShowLog);
            ShowLog();
        }

        private void ShowLog()
        {
            if (_log == null)
            {
                _log = _logFactory.CreateExport();
                _log.Value.View.CloseView += (sender, e) =>
                {
                    _log.Dispose();
                    _log = null;
                };
                _log.Value.ShowView(Name, DockState.DockBottom);
            }
            else
            {
                _log.Value.View.EnsureVisible();
            }
        }
    }
}
