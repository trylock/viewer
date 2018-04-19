﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Log
{
    [Export]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class LogPresenter : Presenter<ILogView>
    {
        private readonly ILog _log;
        
        protected override ExportLifetimeContext<ILogView> ViewLifetime { get; }

        [ImportingConstructor]
        public LogPresenter(ExportFactory<ILogView> viewFactory, ILog log)
        {
            _log = log;
            ViewLifetime = viewFactory.CreateExport();

            _log.EntryAdded += LogOnEntryAdded;
            View.Entries = _log;
            View.UpdateEntries();
            SubscribeTo(View, "View");
        }

        public override void Dispose()
        {
            base.Dispose();
            _log.EntryAdded -= LogOnEntryAdded;
        }

        private void LogOnEntryAdded(object sender, LogEventArgs e)
        {
            View.BeginInvoke(new Action(() =>
            {
                View.Entries = _log;
                View.UpdateEntries();
                View.EnsureVisible();
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
