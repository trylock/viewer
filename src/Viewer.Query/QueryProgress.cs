﻿using System;
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
        /// Create a new progress report.
        /// </summary>
        /// <param name="type">Type of the report</param>
        /// <param name="filePath">Searched file path</param>
        public QueryProgressReport(ReportType type, string filePath)
        {
            Type = type;
            FilePath = filePath;
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

        /// <summary>
        /// Path to the file which is currently being loaded. It can be null.
        /// </summary>
        public string LoadingFile { get; set; }

        /// <summary>
        /// Current number of searched files.
        /// </summary>
        public long FileCount => Interlocked.Read(ref _fileCount);
        
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
                    break;
            }
        }
    }
}
