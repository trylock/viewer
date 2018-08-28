using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Query
{
    /// <summary>
    /// Implementation has to be thread safe.
    /// </summary>
    public interface IErrorListener
    {
        /// <summary>
        /// Method called before each compilation
        /// </summary>
        void BeforeCompilation();

        /// <summary>
        /// Report an error during compilation at <paramref name="line"/> and
        /// <paramref name="column"/>.
        /// </summary>
        /// <param name="line">Line of the query at which the error has happened</param>
        /// <param name="column">Column of the query at which the error has happened</param>
        /// <param name="errorMessage">Error message</param>
        void ReportCompilerError(int line, int column, string errorMessage);

        /// <summary>
        /// Report an error which happened at runtime (e.g. during a function execution).
        /// </summary>
        /// <param name="line">Line of the query at which the error has happened</param>
        /// <param name="column">Column of the query at which the error has happened</param>
        /// <param name="errorMessage">Error message</param>
        void ReportRuntimeError(int line, int column, string errorMessage);

        /// <summary>
        /// Method called after each compilation
        /// </summary>
        void AfterCompilation();
    }

    public class NullErrorListener : IErrorListener
    {
        public void BeforeCompilation()
        {
        }

        public void ReportCompilerError(int line, int column, string errorMessage)
        {
        }

        public void ReportRuntimeError(int line, int column, string errorMessage)
        {
        }
        
        public void AfterCompilation()
        {
        }
    }
}
