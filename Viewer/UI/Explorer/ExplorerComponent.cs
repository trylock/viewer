using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Explorer
{
    [Export(typeof(IComponent))]
    public class ExplorerComponent : IComponent
    {
        private readonly ExportFactory<DirectoryTreePresenter> _explorerFactory;

        [ImportingConstructor]
        public ExplorerComponent(ExportFactory<DirectoryTreePresenter> factory)
        {
            _explorerFactory = factory;
        }

        public void OnStartup()
        {
            var explorerExport = _explorerFactory.CreateExport();
            explorerExport.Value.UpdateRootDirectories();
            explorerExport.Value.View.CloseView += (sender, args) =>
            {
                explorerExport.Dispose();
                explorerExport = null;
            };
            explorerExport.Value.ShowView("Explorer", DockState.DockLeft);
        }
    }
}
