using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query
{
    /// <summary>
    /// This class takes all exported query error listeners and combines them to a
    /// single error listener. 
    ///
    /// > [!NOTE]
    /// > This class is exported as <see cref="AggregateQueryErrorListener"/>, not as
    /// > a <see cref="IQueryErrorListener"/> even though it implements the interface.
    /// </summary>
    [Export(typeof(AggregateQueryErrorListener))]
    public class AggregateQueryErrorListener : IQueryErrorListener
    {
        private readonly IQueryErrorListener[] _listeners;

        [ImportingConstructor]
        public AggregateQueryErrorListener([ImportMany] IQueryErrorListener[] listeners)
        {
            _listeners = listeners;
        }

        public void BeforeCompilation()
        {
            foreach (var listener in _listeners)
            {
                listener.BeforeCompilation();
            }
        }

        public void OnCompilerError(int line, int column, string errorMessage)
        {
            foreach (var listener in _listeners)
            {
                listener.OnCompilerError(line, column, errorMessage);
            }
        }

        public void OnRuntimeError(int line, int column, string errorMessage)
        {
            foreach (var listener in _listeners)
            {
                listener.OnRuntimeError(line, column, errorMessage);
            }
        }

        public void AfterCompilation()
        {
            foreach (var listener in _listeners)
            {
                listener.AfterCompilation();
            }
        }
    }
}
