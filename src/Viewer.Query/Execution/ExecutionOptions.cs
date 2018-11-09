using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.Query.Execution
{
    public class ExecutionOptions
    {
        /// <summary>
        /// Cancellation token which can be used to cancel running execution. The default value is
        /// <c>CancellationToken.None</c>
        /// </summary>
        public CancellationToken CancellationToken { get; set; } = CancellationToken.None;

        /// <summary>
        /// Object which will be used to report query execution progress. The default value is
        /// <see cref="NullQueryProgress"/> which won't do anything.
        /// </summary>
        public IProgress<QueryProgressReport> Progress { get; set; } = new NullQueryProgress();
    }
}
