using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Query;

namespace Viewer.UI
{
    public class QueryEventArgs : EventArgs
    {
        public IExecutableQuery Query { get; }

        public QueryEventArgs(IExecutableQuery query)
        {
            Query = query;
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Query history keeps track of executed queries in the entire application. A query producer
    /// (e.g. a query editor) executes a query by calling <see cref="ExecuteQuery"/> which triggers
    /// the <see cref="QueryExecuted"/> event. A query consumer (e.g. a thumbnail grid) listens for
    /// <see cref="QueryExecuted"/> event and actually executes given query.
    /// </summary>
    /// <remarks>
    /// Queries in the history list are ordered from the newest to the oldest (i.e., the newest
    /// query is at the index 0). The only way to modify this collection is to call the
    /// <see cref="ExecuteQuery"/> method. The only way to modify <see cref="Current"/> is to
    /// either call <see cref="ExecuteQuery"/> or <see cref="Previous"/> and <see cref="Next"/>.
    /// All of these methods will raise the <see cref="QueryExecuted"/> event.
    /// </remarks>
    public interface IQueryHistory : IReadOnlyList<IExecutableQuery>
    {
        /// <summary>
        /// Event occurs when the <see cref="ExecuteQuery"/> method is called. 
        /// </summary>
        event EventHandler<QueryEventArgs> QueryExecuted;
        
        /// <summary>
        /// Execute a query. This triggers the <see cref="QueryExecuted"/> event.
        /// Additionally, this sets current query in history to <paramref name="query"/>.
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <exception cref="ArgumentNullException"><paramref name="query"/> is null</exception>
        void ExecuteQuery(IExecutableQuery query);

        /// <summary>
        /// Current query in query history.
        /// This can be null if no query has been executed yet.
        /// </summary>
        IExecutableQuery Current { get; }

        /// <summary>
        /// Go back in query history. This will trigger the <see cref="QueryExecuted"/> event
        /// iff current query is not the first query in history.
        /// </summary>
        void Back();

        /// <summary>
        /// Go forward in query history. This will trigger the <see cref="QueryExecuted"/> event
        /// iff current query is not the last query in history.
        /// </summary>
        void Forward();

        /// <summary>
        /// Get previous query or null if current query is the first query in history.
        /// </summary>
        IExecutableQuery Previous { get; }

        /// <summary>
        /// Get next query or null if current query is the last query in history.
        /// </summary>
        IExecutableQuery Next { get; }
    }

    [Export(typeof(IQueryHistory))]
    public class QueryHistory : IQueryHistory
    {
        private readonly List<IExecutableQuery> _history = new List<IExecutableQuery>();
        private int _historyHead = -1;

        public event EventHandler<QueryEventArgs> QueryExecuted;

        public IExecutableQuery Current => _historyHead < 0 ? null : _history[_historyHead];
        public IExecutableQuery Previous => _historyHead <= 0 ? null : _history[_historyHead - 1];
        public IExecutableQuery Next => _historyHead >= _history.Count - 1 ? null : _history[_historyHead + 1];
        
        public void ExecuteQuery(IExecutableQuery query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            // only modify the history if the queries differ 
            if (Current == null ||
                StringComparer.CurrentCultureIgnoreCase.Compare(query.Text, Current.Text) != 0)
            {
                // remove all entries in history after _historyHead
                var index = _history.Count - 1;
                while (index > _historyHead)
                {
                    _history.RemoveAt(index--);
                }

                // add a new query to history and make it the current query
                _history.Add(query);
                ++_historyHead;

                Trace.Assert(_historyHead == _history.Count - 1);
            }

            // trigger query executed event
            QueryExecuted?.Invoke(this, new QueryEventArgs(query));
        }

        public void Back()
        {
            if (_historyHead <= 0)
            {
                return;
            }

            --_historyHead;

            QueryExecuted?.Invoke(this, new QueryEventArgs(Current));
        }

        public void Forward()
        {
            if (_historyHead >= _history.Count - 1)
            {
                return;
            }

            ++_historyHead;

            QueryExecuted?.Invoke(this, new QueryEventArgs(Current));
        }

        public IEnumerator<IExecutableQuery> GetEnumerator()
        {
            for (var i = _history.Count - 1; i >= 0; --i)
            {
                yield return _history[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _history.Count;

        public IExecutableQuery this[int index] => _history[_history.Count - index - 1];
    }
}
