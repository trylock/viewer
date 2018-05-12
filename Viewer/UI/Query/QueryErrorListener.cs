using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Query;
using Viewer.UI.Log;

namespace Viewer.UI.Query
{
    [Export(typeof(IErrorListener))]
    public class QueryErrorListener : IErrorListener
    {
        private readonly ILog _log;

        [ImportingConstructor]
        public QueryErrorListener(ILog log)
        {
            _log = log;
        }

        public void BeforeCompilation()
        {
            _log.Clear();
        }

        public void ReportError(int row, int column, string errorMessage)
        {
            _log.Add(new LogEntry
            {
                Line = row,
                Column = column,
                Message = errorMessage,
                Group = "Query",
                Type = LogType.Error
            });
        }

        public void AfterCompilation()
        {
        }
    }
}
