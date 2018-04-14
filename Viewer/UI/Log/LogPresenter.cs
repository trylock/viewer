using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Log
{
    public class LogPresenter
    {
        private readonly ILogView _view;
        private readonly ILog _log;

        public LogPresenter(ILogView view, ILog log)
        {
            _log = log;
            _log.EntryAdded += LogOnEntryAdded;
            _view = view;
            _view.Entries = _log;
            _view.UpdateEntries();

            PresenterUtils.SubscribeTo(_view, this, "View");
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
