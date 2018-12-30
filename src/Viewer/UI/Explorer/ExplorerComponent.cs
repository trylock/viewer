using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Data;
using Viewer.IO;
using Viewer.Localization;
using Viewer.Properties;
using Viewer.Query;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Explorer
{
    [Export(typeof(IComponent))]
    public class ExplorerComponent : Component
    {
        private readonly IQueryHistory _state;
        private readonly IQueryFactory _queryFactory;
        private readonly IFileSystem _fileSystem;
        private readonly IClipboardService _clipboard;
        private readonly IFileSystemErrorView _dialogView;
        private readonly IExplorer _explorer;
        
        private DirectoryTreePresenter _presenter;

        [ImportingConstructor]
        public ExplorerComponent(
            IQueryHistory state,
            IQueryFactory queryFactory,
            IFileSystemErrorView dialogView,
            IFileSystem fileSystem,
            IExplorer explorer,
            IClipboardService clipboard)
        {
            _state = state;
            _queryFactory = queryFactory;
            _fileSystem = fileSystem;
            _clipboard = clipboard;
            _dialogView = dialogView;
            _explorer = explorer;
        }

        public override void OnStartup(IViewerApplication app)
        {
            app.AddMenuItem(new []{ Strings.View_Label, Strings.ExplorerWindowName }, 
                () => ShowExplorer(), Resources.ExplorerComponentIcon.ToBitmap());
            app.AddLayoutDeserializeCallback(Deserialize);
        }

        private IWindowView Deserialize(string persistString)
        {
            if (persistString == typeof(DirectoryTreeView).FullName)
            {
                return GetExplorer().View;
            }

            return null;
        }

        private DirectoryTreePresenter GetExplorer()
        {
            if (_presenter == null)
            {
                _presenter = new DirectoryTreePresenter(
                    new DirectoryTreeView(), 
                    _state, 
                    _queryFactory, 
                    _dialogView, 
                    _fileSystem,
                    _explorer, 
                    _clipboard);
                _presenter.View.Text = Strings.ExplorerWindowName;
                _presenter.View.CloseView += (sender, args) =>
                {
                    _presenter.Dispose();
                    _presenter = null;
                };
            }
            else
            {
                _presenter.View.EnsureVisible();
            }

            return _presenter;
        }

        private IDockContent ShowExplorer()
        {
            var explorer = GetExplorer();
            explorer.View.Show(Application.Panel, DockState.DockLeft);
            return explorer.View;
        }
    }
}
