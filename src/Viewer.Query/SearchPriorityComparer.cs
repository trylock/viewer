using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.IO;
using Viewer.Query.Expressions;
using ConstantExpression = Viewer.Query.Expressions.ConstantExpression;
using ExpressionVisitor = Viewer.Query.Expressions.ExpressionVisitor;

namespace Viewer.Query
{
    /// <summary>
    /// Function which defines an order on searched directories during query evaluation.
    /// </summary>
    internal class SearchPriorityComparer : IComparer<string>
    {
        /// <summary>
        /// This class will find all attribute names which are accessed in given expression. Found
        /// names will be stored in a list so that repeated access is fast.
        /// </summary>
        private class AttributeNameCollection : ExpressionVisitor, IReadOnlyList<string>
        {
            private readonly List<string> _names = new List<string>();

            public AttributeNameCollection(ValueExpression expr)
            {
                expr.Accept(this);
            }

            public override bool Visit(AttributeAccessExpression expr)
            {
                _names.Add(expr.Name);
                return true;
            }

            public IEnumerator<string> GetEnumerator()
            {
                return _names.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public int Count => _names.Count;

            public string this[int index] => _names[index];
        }

        /// <summary>
        /// This class computes a priority based on an expression and statistics. The priority can
        /// be viewed as a probability that the expression does not evaluate to null. It assumes
        /// independence of expression subtrees which will obviously fail for expressions like
        /// "a = a". 
        /// </summary>
        private class PriorityVisitor : IExpressionVisitor<double>
        {
            private Statistics _statistics;
            
            /// <summary>
            /// Compute the probability that <paramref name="expression"/> does not evaluate to null
            /// for a file in a directory. This function works with the <paramref name="statistics"/>
            /// object whcih contain directory subtree statistics.
            /// </summary>
            /// <param name="expression"></param>
            /// <param name="statistics"></param>
            /// <returns></returns>
            public double Compute(ValueExpression expression, Statistics statistics)
            {
                _statistics = statistics;
                return expression.Accept(this);
            }

            /// <summary>
            /// Compute probability that a binary operator is not null.
            /// </summary>
            /// <param name="expr">Binary operator expression</param>
            /// <returns>Probability that the expression is not null for a file</returns>
            private double Compute(BinaryOperatorExpression expr)
            {
                var left = expr.Left.Accept(this);
                var right = expr.Right.Accept(this);
                return left * right;
            }

            double IExpressionVisitor<double>.Visit(AndExpression expr)
            {
                return Compute(expr);
            }

            double IExpressionVisitor<double>.Visit(OrExpression expr)
            {
                var left = expr.Left.Accept(this);
                var right = expr.Right.Accept(this);
                return left + right - left * right;
            }

            double IExpressionVisitor<double>.Visit(NotExpression expr)
            {
                var weight = expr.Parameter.Accept(this);
                return 1 - weight;
            }

            double IExpressionVisitor<double>.Visit(AdditionExpression expr)
            {
                return Compute(expr);
            }

            double IExpressionVisitor<double>.Visit(SubtractionExpression expr)
            {
                return Compute(expr);
            }

            double IExpressionVisitor<double>.Visit(MultiplicationExpression expr)
            {
                return Compute(expr);
            }

            double IExpressionVisitor<double>.Visit(DivisionExpression expr)
            {
                return Compute(expr);
            }

            double IExpressionVisitor<double>.Visit(LessThanOperator expr)
            {
                return Compute(expr);
            }

            double IExpressionVisitor<double>.Visit(LessThanOrEqualOperator expr)
            {
                return Compute(expr);
            }

            private double ComputeEqualsPriority(ValueExpression left, ValueExpression right)
            {
                var leftPriority = left.Accept(this);
                var rightPriority = right.Accept(this);
                // either both are not null or both are null
                return leftPriority * rightPriority + (1 - leftPriority) * (1 - rightPriority);
            }

            double IExpressionVisitor<double>.Visit(EqualOperator expr)
            {
                return ComputeEqualsPriority(expr.Left, expr.Right);
            }

            double IExpressionVisitor<double>.Visit(NotEqualOperator expr)
            {
                // translate this as "not (left = right)"
                return 1 - ComputeEqualsPriority(expr.Left, expr.Right);
            }

            double IExpressionVisitor<double>.Visit(GreaterThanOperator expr)
            {
                return Compute(expr);
            }

            double IExpressionVisitor<double>.Visit(GreaterThanOrEqualOperator expr)
            {
                return Compute(expr);
            }

            double IExpressionVisitor<double>.Visit(FunctionCallExpression expr)
            {
                // Here we assume that function result will be null iff some of its parameters is
                // null. This will be true for many functions but it fails for functions like "or",
                // "not" etc (operator functions are handeled separately if they are used as
                // operators)
                double weight = 1.0;
                foreach (var parameter in expr.Parameters)
                {
                    var parameterWeight = parameter.Accept(this);
                    weight *= parameterWeight;
                }

                return weight;
            }

            double IExpressionVisitor<double>.Visit(ConstantExpression expr)
            {
                return 1; // constant is never null
            }

            double IExpressionVisitor<double>.Visit(AttributeAccessExpression expr)
            {
                // TODO: metadata attributes should return probability of 1.0

                var count = _statistics.GetAttributeCount(expr.Name);
                return count / (double)_statistics.Count;
            }
        }

        private class AttributeStatistics
        {
            /// <summary>
            /// Name of the attribute
            /// </summary>
            public string Name;

            /// <summary>
            /// Number of attribute with this name in this directory or file
            /// </summary>
            public long Count;
        }

        private class Statistics
        {
            private readonly List<AttributeStatistics> _attributes = new List<AttributeStatistics>();

            /// <summary>
            /// Total number of files 
            /// </summary>
            public long Count;

            /// <summary>
            /// Add an attribute named <paramref name="name"/>
            /// </summary>
            /// <param name="name"></param>
            public void AddAttribute(string name)
            {
                var statistics = _attributes.Find(attr => StringComparer.CurrentCulture.Equals(attr.Name, name));
                if (statistics == null)
                {
                    statistics = new AttributeStatistics
                    {
                        Name = name
                    };
                    _attributes.Add(statistics);
                }

                ++statistics.Count;
            }

            /// <summary>
            /// Get number of files with attribute named <paramref name="name"/> in this subtree
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public long GetAttributeCount(string name)
            {
                var attr = _attributes.Find(item => StringComparer.CurrentCulture.Equals(item.Name, name));
                if (attr == null)
                {
                    return 0;
                }

                return attr.Count;
            }
        }
        
        private sealed class Node
        {
            /// <summary>
            /// Path to the file of this node
            /// </summary>
            public string PathPart { get; set; }

            /// <summary>
            /// Child directories of this file
            /// </summary>
            public List<Node> Children { get; } = new List<Node>();

            /// <summary>
            /// Directory statistics
            /// </summary>
            public Statistics Statistics { get; } = new Statistics();
        }
        
        private static readonly StringComparer PathComparer = StringComparer.CurrentCultureIgnoreCase;

        /// <summary>
        /// Collection of attribute names accessed in _expression
        /// </summary>
        private readonly AttributeNameCollection _names;

        /// <summary>
        /// Expression in the where part of a query
        /// </summary>
        private readonly ValueExpression _expression;

        /// <summary>
        /// Index structure used to compute folder priorities
        /// </summary>
        private Node _root;

        public SearchPriorityComparer(ValueExpression expression)
        {
            _expression = expression;
            _names = new AttributeNameCollection(_expression);
            _root = new Node();
        }

        /// <summary>
        /// Fetch statistics from the disk and build an index structure which is then used to
        /// sped up the file search by ordering searched folders.
        /// </summary>
        /// <param name="attributes">Attribute statistics info</param>
        public void Index(IAttributeCache attributes)
        {
            if (_names.Count > 0)
            {
                // build a directory tree with attribute statistics
                var locations = attributes.GetAttributes(_names);
                _root = IndexAttributes(locations);

                // add file count to directory statistics
                var files = attributes.GetFiles().ToList();
                Parallel.ForEach(files, AddFileCount);
            }
            else
            {
                _root = new Node();
            }
        }

        /// <summary>
        /// Count files in each subtree in index. Only folders with attributes are in the tree.
        /// No new nodes are added.
        /// </summary>
        /// <param name="path"></param>
        private void AddFileCount(string path)
        {
            var node = _root;
            var parts = PathUtils.Split(path).ToList();
            for (var i = 0; i < parts.Count - 1; ++i)
            {
                Interlocked.Increment(ref node.Statistics.Count);

                var part = parts[i];
                var childNode = node.Children.Find(child => PathComparer.Equals(child.PathPart, part));
                if (childNode == null)
                {
                    break;
                }

                node = childNode;
            }

            if (node != null)
            {
                Interlocked.Increment(ref node.Statistics.Count);
            }
        }

        /// <summary>
        /// Create a directory tree with attribute statistics 
        /// </summary>
        /// <param name="locations"></param>
        /// <returns></returns>
        private Node IndexAttributes(List<AttributeLocation> locations)
        {
            var root = new Node();
            foreach (var location in locations)
            {
                // split file path using directory separators
                var node = root;
                var parts = PathUtils.Split(location.FilePath).ToList();

                // add the attribute to all directory nodes
                // the last part is the file name, thus `parts.Count - 1`
                for (var i = 0; i < parts.Count - 1; ++i)
                {
                    // find the child node of the next directory
                    var part = parts[i];
                    var childNode = node.Children.Find(child => PathComparer.Equals(child.PathPart, part));
                    if (childNode == null) // if it does not exist, create it
                    {
                        childNode = new Node
                        {
                            PathPart = part,
                        };
                        node.Children.Add(childNode);
                    }

                    node.Statistics.AddAttribute(location.AttributeName);
                    node = childNode;
                }
                node.Statistics.AddAttribute(location.AttributeName);
            }

            return root;
        }

        /// <summary>
        /// Get search priority of <paramref name="path"/>. Paths with higher priority should be
        /// searched first as there is higher probability that we will find a file.
        /// </summary>
        /// <param name="path">Tested path</param>
        /// <returns>Search priority of <paramref name="path"/></returns>
        public double GetPriority(string path)
        {
            // find statistics for this directory
            var node = _root;
            var parts = PathUtils.Split(path);
            foreach (var part in parts)
            {
                var next = node.Children.Find(child => 
                    StringComparer.CurrentCultureIgnoreCase.Equals(child.PathPart, part));
                node = next;
                if (node == null)
                {
                    break;
                }
            }

            if (node == null)
            {
                return 0;
            }

            // compute statistics for the expression
            var priority = new PriorityVisitor();
            return priority.Compute(_expression, node.Statistics);
        }

        /// <summary>
        /// Compare 2 paths <paramref name="x"/> and <paramref name="y"/> based on their
        /// priorities returned by <see cref="GetPriority"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(string x, string y)
        {
            if (x == y)
            {
                return 0;
            }
            var priorityX = GetPriority(x);
            var priorityY = GetPriority(y);
            return priorityX > priorityY ? -1 : 1;
        }
    }
}
