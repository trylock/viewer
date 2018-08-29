using System;
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
        public IQuery Query { get; }

        public QueryEventArgs(IQuery query)
        {
            Query = query;
        }
    }

    /// <summary>
    /// Query history keeps track of executed queries in the entire application. Executed queries
    /// are kept in a history collection. A query producer (e.g. a query editor) executes a query
    /// by calling <see cref="ExecuteQuery"/> which triggers the <see cref="QueryExecuted"/> event.
    /// A query consumer (e.g. a thumbnail grid) listens for <see cref="QueryExecuted"/> event and
    /// actually executes given query.
    /// </summary>
    public interface IQueryHistory
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
        void ExecuteQuery(IQuery query);

        /// <summary>
        /// Current query in query history.
        /// This can be null if no query has been executed yet.
        /// </summary>
        IQuery Current { get; }

        /// <summary>
        /// Go back in query history.
        /// This will trigger the <see cref="QueryExecuted"/> event iff current query is not the first query in history.
        /// </summary>
        void Back();

        /// <summary>
        /// Go forward in query history.
        /// This will trigger the <see cref="QueryExecuted"/> event iff current query is not the last query in history.
        /// </summary>
        void Forward();

        /// <summary>
        /// Get previous query or null if current query is the first query in history.
        /// </summary>
        IQuery Previous { get; }

        /// <summary>
        /// Get next query or null if current query is the last query in history.
        /// </summary>
        IQuery Next { get; }
    }

    [Export(typeof(IQueryHistory))]
    public class QueryHistory : IQueryHistory
    {
        private readonly List<IQuery> _history = new List<IQuery>();
        private int _historyHead = -1;

        public event EventHandler<QueryEventArgs> QueryExecuted;

        public IQuery Current => _historyHead < 0 ? null : _history[_historyHead];
        public IQuery Previous => _historyHead <= 0 ? null : _history[_historyHead - 1];
        public IQuery Next => _historyHead >= _history.Count - 1 ? null : _history[_historyHead + 1];
        
        public void ExecuteQuery(IQuery query)
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
    }
}
