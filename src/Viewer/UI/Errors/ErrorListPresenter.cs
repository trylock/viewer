using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core.UI;

namespace Viewer.UI.Errors
{
    internal class ErrorListPresenter : Presenter<IErrorListView>
    {
        private readonly IErrorList _errorList;
        
        public ErrorListPresenter(IErrorListView view, IErrorList errorList)
        {
            _errorList = errorList;
            View = view;
            View.Entries = _errorList;
            View.UpdateEntries();
            SubscribeTo(View, "View");

            _errorList.EntryAdded += LogOnEntryAdded;
            _errorList.EntriesRemoved += LogOnEntriesRemoved;
        }

        public override void Dispose()
        {
            base.Dispose();
            _errorList.EntryAdded -= LogOnEntryAdded;
            _errorList.EntriesRemoved -= LogOnEntriesRemoved;
        }

        private void LogOnEntryAdded(object sender, LogEventArgs e)
        {
            View.BeginInvoke(new Action(() =>
            {
                View.Entries = _errorList;
                View.UpdateEntries();
                View.EnsureVisible();
            }));
        }

        private void LogOnEntriesRemoved(object sender, EventArgs e)
        {
            View.BeginInvoke(new Action(() =>
            {
                View.Entries = _errorList;
                View.UpdateEntries();
            }));
        }

        private void View_Retry(object sender, RetryEventArgs e)
        {
            var entry = e.Entry;
            _errorList.Remove(entry);
            entry.RetryOperation?.Invoke();
        }
    }
}
