using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Log
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class LogPresenter : Presenter
    {
        private readonly ILog _log;
        private readonly ILogView _view;

        public override IWindowView MainView => _view;
        
        [ImportingConstructor]
        public LogPresenter([Import(RequiredCreationPolicy = CreationPolicy.NonShared)] ILogView logView, ILog log)
        {
            _log = log;
            _view = logView;

            _log.EntryAdded += LogOnEntryAdded;
            _view.Entries = _log;
            _view.UpdateEntries();
            SubscribeTo(_view, "View");
        }
        
        private void LogOnEntryAdded(object sender, LogEventArgs e)
        {
            _view.BeginInvoke(new Action(() =>
            {
                _view.Entries = _log;
                _view.UpdateEntries();
                _view.EnsureVisible();
            }));
        }

        private void View_Retry(object sender, RetryEventArgs e)
        {
            var entry = e.Entry;
            _log.Remove(entry);
            entry.RetryOperation?.Invoke();
        }
    }
}
