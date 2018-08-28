using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Viewer.Query;
using Viewer.UI.Errors;

namespace Viewer.UI.QueryEditor
{
    [Export(typeof(IErrorListener))]
    public class QueryErrorListener : IErrorListener
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IErrorList _errorList;
        private readonly ConcurrentDictionary<string, bool> _reportedRuntimeErrors = new ConcurrentDictionary<string, bool>();

        [ImportingConstructor]
        public QueryErrorListener(IErrorList errorList)
        {
            _errorList = errorList;
        }

        public void BeforeCompilation()
        {
            _errorList.Clear();
            _reportedRuntimeErrors.Clear();
        }

        public void ReportCompilerError(int line, int column, string errorMessage)
        {
            Logger.Debug("[{0}][{1}] {2}", line, column, errorMessage);

            _errorList.Add(new ErrorListEntry
            {
                Line = line,
                Column = column,
                Message = errorMessage,
                Group = "Query",
                Type = LogType.Error
            });
        }

        public void ReportRuntimeError(int line, int column, string errorMessage)
        {
            // Runtime errors can be reported for many entities. We only want to report each
            // error once to the user.
            var errorKey = line + ";" + column + ";" + errorMessage;
            if (_reportedRuntimeErrors.ContainsKey(errorKey))
            {
                return;
            }

            Logger.Debug("[{0}][{1}] {2}", line, column, errorMessage);

            _errorList.Add(new ErrorListEntry
            {
                Line = line,
                Column = column,
                Message = errorMessage,
                Group = "Query",
                Type = LogType.Warning
            });
            _reportedRuntimeErrors[errorKey] = true;
        }
        
        public void AfterCompilation()
        {
        }
    }
}
