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
        /// Report an error at <paramref name="row"/> and <paramref name="column"/>
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <param name="errorMessage"></param>
        void ReportError(int row, int column, string errorMessage);

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

        public void ReportError(int row, int column, string errorMessage)
        {
        }

        public void AfterCompilation()
        {
        }
    }
}
