using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Properties;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Explorer
{
    [Export(typeof(IComponent))]
    public class ExplorerComponent : IComponent
    {
        private readonly ExportFactory<DirectoryTreePresenter> _explorerFactory;

        private ExportLifetimeContext<DirectoryTreePresenter> _explorer;

        [ImportingConstructor]
        public ExplorerComponent(ExportFactory<DirectoryTreePresenter> factory)
        {
            _explorerFactory = factory;
        }

        public void OnStartup(IViewerApplication app)
        {
            app.AddViewAction(Resources.ExplorerWindowName, () => ShowExplorer(), Resources.ExplorerComponentIcon.ToBitmap());
        }

        public IDockContent Deserialize(string persistString)
        {
            if (persistString == typeof(DirectoryTreeView).FullName)
            {
                return ShowExplorer();
            }

            return null;
        }

        private IDockContent ShowExplorer()
        {
            if (_explorer == null)
            {
                _explorer = _explorerFactory.CreateExport();
                _explorer.Value.UpdateRootDirectories();
                _explorer.Value.View.CloseView += (sender, args) =>
                {
                    _explorer.Dispose();
                    _explorer = null;
                };
                _explorer.Value.ShowView(Resources.ExplorerWindowName, DockState.DockLeft);
            }
            else
            {
                _explorer.Value.View.EnsureVisible();
            }

            return _explorer.Value.View;
        }
    }
}
