using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Query;
using Viewer.UI.Errors;

namespace Viewer.UI.Query
{
    [Export(typeof(IErrorListener))]
    public class QueryErrorListener : IErrorListener
    {
        private readonly IErrorList _errorList;

        [ImportingConstructor]
        public QueryErrorListener(IErrorList errorList)
        {
            _errorList = errorList;
        }

        public void BeforeCompilation()
        {
            _errorList.Clear();
        }

        public void ReportError(int row, int column, string errorMessage)
        {
            _errorList.Add(new ErrorListEntry
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
