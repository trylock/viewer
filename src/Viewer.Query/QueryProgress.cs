using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Viewer.Query
{
    /// <summary>
    /// Type of <see cref="QueryProgressReport"/>.
    /// </summary>
    public enum ReportType
    {
        /// <summary>
        /// Reports that a query execution has started.
        /// </summary>
        BeginExecution,

        /// <summary>
        /// Reports that the program has started loading a new file.
        /// </summary>
        BeginLoading,

        /// <summary>
        /// Reports that the program has finished loading a file.
        /// </summary>
        EndLoading,

        /// <summary>
        /// Reports taht a query execution has finished.
        /// </summary>
        EndExecution
    }

    /// <summary>
    /// Query progress argument passed to <see cref="IProgress{T}"/> during evaluation.
    /// </summary>
    public struct QueryProgressReport
    {
        /// <summary>
        /// Type of this report.
        /// </summary>
        public ReportType Type { get; }

        /// <summary>
        /// Full path to the last file the query evaluator has searched.
        /// Note: this is not necessarily the last file in the query result set.
        ///       The file might have not matched the query.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Number of read bytes from the disk to search the file. It can only be non-zero if
        /// <see cref="Type"/> is <see cref="ReportType.EndLoading"/>.
        /// Note: this is a lower bound on the total number of read bytes. It can even be 0 if
        ///       no I/O was necessary or the storage could not determine at least an approximate
        ///       ammount of bytes read from a disk.
        /// </summary>
        public long BytesRead { get; }

        /// <summary>
        /// Create a new progress report.
        /// </summary>
        /// <param name="type">Type of the report</param>
        /// <param name="filePath">Searched file path</param>
        /// <param name="bytesRead">Number of bytes read from a disk</param>
        public QueryProgressReport(ReportType type, string filePath, long bytesRead)
        {
            Type = type;
            FilePath = filePath;
            BytesRead = bytesRead;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Report function of this query progress is nop.
    /// </summary>
    public class NullQueryProgress : IProgress<QueryProgressReport>
    {
        public void Report(QueryProgressReport value)
        {
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Query progress computes statistics on the fly and provides a real time look into query execution.
    /// This class is thread safe.
    /// </summary>
    public class QueryProgress : IProgress<QueryProgressReport>
    {
        private long _fileCount;
        private long _readBytes;

        /// <summary>
        /// Path to the file which is currently being loaded. It can be null.
        /// </summary>
        public string LoadingFile { get; set; }

        /// <summary>
        /// Current number of searched files.
        /// </summary>
        public long FileCount => Interlocked.Read(ref _fileCount);

        /// <summary>
        /// Lower bound on the total number of bytes read from a disk.
        /// </summary>
        public long BytesRead => Interlocked.Read(ref _readBytes);
        
        public void Report(QueryProgressReport value)
        {
            switch (value.Type)
            {
                case ReportType.EndExecution:
                    LoadingFile = null;
                    break;
                case ReportType.BeginLoading:
                    LoadingFile = value.FilePath;
                    break;
                case ReportType.EndLoading:
                    Interlocked.Increment(ref _fileCount);
                    Interlocked.Add(ref _readBytes, value.BytesRead);
                    break;
            }
        }
    }
}
