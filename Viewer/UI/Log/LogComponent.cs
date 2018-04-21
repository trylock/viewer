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
        private readonly ExportFactory<LogPresenter> _logFactory;

        [ImportingConstructor]
        public LogComponent(ExportFactory<LogPresenter> factory)
        {
            _logFactory = factory;
        }

        public void OnStartup()
        {
            var logExport = _logFactory.CreateExport();
            logExport.Value.View.CloseView += (sender, e) =>
            {
                logExport.Dispose();
                logExport = null;
            };
            logExport.Value.ShowView("Event Log", DockState.DockBottom);
        }
    }
}
