using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Query.Expressions;

namespace Viewer.Query.Search
{
    /// <summary>
    /// Priority function takes a predicate expression and a path to a directory and computes
    /// a search priority of this directory. This priority is used to sort directories in
    /// the <see cref="Viewer.IO.FileFinder"/> class during query evaluation.
    /// </summary>
    internal interface IPriorityFunction
    {
        /// <summary>
        /// Compute search priority of directory at <paramref name="path"/> given
        /// <paramref name="expression"/>.
        /// </summary>
        /// <param name="expression">Expression in the where part of a query</param>
        /// <param name="path">Path to a directory</param>
        /// <returns>
        /// Search priority of <paramref name="path"/>. Directories with higher search priority
        /// will be searched first.
        /// </returns>
        double Compute(ValueExpression expression, string path);
    }
}
