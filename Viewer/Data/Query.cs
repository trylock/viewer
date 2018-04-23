using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    /// <summary>
    /// Immutable representation of a query
    /// </summary>
    public class Query
    {
        private readonly ImmutableList<Func<IEntity, bool>> _filter = ImmutableList<Func<IEntity, bool>>.Empty;

        /// <summary>
        /// Directory path pattern
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// List of conditions
        /// </summary>
        public IReadOnlyList<Func<IEntity, bool>> Filter => _filter;

        /// <summary>
        /// Create a new empty query
        /// </summary>
        public Query()
        {
        }
        
        private Query(string pattern, ImmutableList<Func<IEntity, bool>> filter)
        {
            Pattern = pattern;
            _filter = filter;
        }

        /// <summary>
        /// Create a new query with given select pattern
        /// </summary>
        /// <param name="pattern">New select pattern</param>
        /// <returns>New query with given select pattern</returns>
        public Query Select(string pattern)
        {
            return new Query(pattern, _filter);
        }

        /// <summary>
        /// Create a new query with additional condition
        /// </summary>
        /// <param name="condition">New condition to add</param>
        /// <returns>New query with additional condition</returns>
        public Query Where(Func<IEntity, bool> condition)
        {
            return new Query(Pattern, _filter.Add(condition));
        }
    }
}
