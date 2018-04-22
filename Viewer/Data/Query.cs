using System;
using System.Collections.Generic;
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
        /// <summary>
        /// Directory path pattern
        /// </summary>
        public string SelectPattern { get; }

        /// <summary>
        /// Create a new empty query
        /// </summary>
        public Query()
        {
        }
        
        private Query(string selectPattern)
        {
            SelectPattern = selectPattern;
        }

        /// <summary>
        /// Create a new query with given select pattern
        /// </summary>
        /// <param name="pattern">New select pattern</param>
        /// <returns>New query with given select pattern</returns>
        public Query Select(string pattern)
        {
            return new Query(pattern);
        }
    }
}
