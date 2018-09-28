using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Viewer.Core.Collections;
using Viewer.Data;
using Viewer.IO;
using Viewer.Query.Expressions;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.Query.Search
{
    internal class Statistics
    {
        private sealed class Node
        {
            /// <summary>
            /// File name of this node
            /// </summary>
            public string PathPart { get; }

            /// <summary>
            /// Child directories of this file
            /// </summary>
            public List<Node> Children { get; } = new List<Node>();

            /// <summary>
            /// Index in this array is an index of an attribute subset. Value is the number of
            /// files in this subtree which contain exactly this subset of attributes (no more or
            /// less). Indices of subsets are taken from the <see cref="Statistics.Attributes"/>
            /// collection.
            /// </summary>
            /// <example>
            /// If 0th index is the index of the subset `{a, b}`, `Count[0]` will be the number
            /// of files in this subtree which contain attributes `a` and `b` and no other
            /// attributes. 
            /// </example>
            public List<long> SubsetCount { get; } = new List<long>();

            public Node(string pathPart)
            {
                PathPart = pathPart;
            }

            /// <summary>
            /// Make sure <see cref="SubsetCount"/> has at least <paramref name="count"/> elements.
            /// </summary>
            /// <param name="count">
            /// Minimal expected number of subsets in <see cref="SubsetCount"/>.
            /// </param>
            public void EnsureSubsetCount(int count)
            {
                for (var i = SubsetCount.Count; i < count; ++i)
                {
                    SubsetCount.Add(0);
                }
            }
            
            /// <summary>
            /// Find child node named <paramref name="fileName"/>.
            /// </summary>
            /// <param name="fileName">Name of a child node</param>
            /// <returns>
            /// Child node named <paramref name="fileName"/> or null if there is no such child node
            /// </returns>
            public Node FindChild(string fileName)
            {
                return Children.Find(item => 
                    StringComparer.CurrentCultureIgnoreCase.Equals(item.PathPart, fileName));
            }
        }

        /// <summary>
        /// Index structure used to compute folder priorities
        /// </summary>
        private readonly Node _root;

        /// <summary>
        /// Subsets of relevant attributes used in some files. This object only consideres the
        /// attributes used in the where part of a query to be relevant.
        /// </summary>
        /// <remarks>
        /// The size of this collection is potentially exponential. This will not be the case
        /// in any practical situation. In fact, the number of used subsets of attributes will
        /// most probably be linear with the number of attributes. 
        /// </remarks>
        public SubsetCollection<string> Attributes { get; }

        private Statistics(Node root, SubsetCollection<string> subsets)
        {
            _root = root;
            Attributes = subsets;
        }

        /// <summary>
        /// Get cached number of files and attributes in <paramref name="path"/> subtree.
        /// </summary>
        /// <param name="path">
        /// Path to check. If it is an empty string, statistics for the whole tree are returned.
        /// </param>
        /// <returns>
        /// Statistics for directory subtree or null if there are no statistics gathered for
        /// <paramref name="path"/>. 
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        public IReadOnlyList<long> GetDirectory(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (path.Length == 0)
                return _root.SubsetCount;

            var node = FindNode(path);

            return node?.SubsetCount;
        }
        
        /// <summary>
        /// Fetch statistics data from <paramref name="attributeCache"/> and create an interanl
        /// index structure. Only attributes named <paramref name="attributeNames"/> will be fetched.
        /// </summary>
        /// <param name="attributeCache"></param>
        /// <param name="attributeNames"></param>
        /// <returns></returns>
        public static Statistics Fetch(IAttributeCache attributeCache, IReadOnlyList<string> attributeNames)
        {
            Statistics statistics = null;
            if (attributeNames.Count <= 0)
            {
                statistics = new Statistics(new Node(""), new SubsetCollection<string>
                {
                    Enumerable.Empty<string>()
                });
            }
            else
            {
                // build a directory tree with attribute statistics
                var locations = attributeCache.GetAttributes(attributeNames).ToList();
                statistics = IndexAttributes(locations);
            }

            using (var counter = attributeCache.CreateFileAggregate())
            {
                statistics.CountEmptySubsets(counter);
            }

            return statistics;
        }

        private Node FindNode(string path)
        {
            // find statistics for this directory
            var node = _root;
            var parts = PathUtils.Split(path);
            foreach (var part in parts)
            {
                var next = node.FindChild(part);
                node = next;
                if (node == null)
                {
                    break;
                }
            }

            return node;
        }

        /// <summary>
        /// Create a directory tree with attribute statistics 
        /// </summary>
        /// <param name="groups"></param>
        /// <returns></returns>
        private static Statistics IndexAttributes(IEnumerable<AttributeGroup> groups)
        {
            var root = new Node("");
            var subsets = new SubsetCollection<string> { Enumerable.Empty<string>() };
            
            foreach (var group in groups)
            {
                // add this attribute subset to the subset array
                int subsetIndex = subsets.Add(group.Attributes);

                // increment subset counter on the path to this file
                var node = root;
                var pathParts = PathUtils.Split(group.FilePath).ToList();
                for (var i = 0; i < pathParts.Count; ++i)
                {
                    // increment the subset count
                    node.EnsureSubsetCount(subsetIndex + 1);
                    ++node.SubsetCount[subsetIndex];

                    // if this is the last part (file name), don't add a node for it
                    if (i + 1 >= pathParts.Count)
                    {
                        break;
                    }

                    // find a child node
                    var part = pathParts[i];
                    var childNode = node.FindChild(part);
                    if (childNode == null)
                    {
                        if (group.Attributes.Count <= 0)
                        {
                            // If the file does not contain any relevant attributes, don't add it
                            // to the index tree.
                            break;
                        }

                        childNode = new Node(part);
                        node.Children.Add(childNode);
                    }

                    node = childNode;
                }
            }

            return new Statistics(root, subsets);
        }
        
        private void CountEmptySubsets(IFileAggregate aggregate)
        {
            var stack = new Stack<(Node Node, string Path)>();
            foreach (var node in _root.Children)
            {
                stack.Push((node, node.PathPart));
            }

            while (stack.Count > 0)
            {
                var item = stack.Pop();
                var node = item.Node;
                var path = item.Path + '/';

                // make sure the node has a counter for each subset (albeit most of them are 0)
                node.EnsureSubsetCount(Attributes.Count);
                
                // count number of files with no relevant attributes 
                node.SubsetCount[0] = 0;
                var count = aggregate.Count(path);
                var sum = node.SubsetCount.Sum();
                Debug.Assert(sum <= count);
                node.SubsetCount[0] = Math.Max(count - sum, 0);
                
                // process all children
                foreach (var child in node.Children)
                {
                    stack.Push((child, Path.Combine(path, child.PathPart)));
                }
            }

            // aggregate empty set count for the root
            _root.EnsureSubsetCount(Attributes.Count);
            _root.SubsetCount[0] = _root.Children.Sum(item => item.SubsetCount[0]);
        }
    }
    
    /// <summary>
    /// This class computes a priority based on an expression and statistics. The priority can
    /// be viewed as a probability that the expression does not evaluate to null. 
    /// </summary>
    /// <remarks>
    /// This function is a heuristic. Here are some observations on which it depends:
    /// 1) Most files won't contain attributes accessed in given expression.
    /// 2) Denote `n` the number of accessed attributes in given expression, the number of subsets
    ///    of these attributes used in some files is `O(n)`
    /// 3) Functions other than logical operators will be null iff one of their parameters is null
    /// </remarks>
    internal class PriorityFunction : IPriorityFunction
    {
        /// <summary>
        /// Priority visitor returns a collection of attribute subsets for which given expression
        /// will probably not return a null value.
        /// </summary>
        private class PriorityVisitor : IExpressionVisitor<Bitmap>
        {
            private readonly SubsetCollection<string> _subsets;

            public PriorityVisitor(SubsetCollection<string> subsets)
            {
                _subsets = subsets ?? throw new ArgumentNullException(nameof(subsets));
            }

            public Bitmap Visit(AndExpression expr)
            {
                var left = expr.Left.Accept(this);
                var right = expr.Right.Accept(this);
                left.And(right);
                return left;
            }

            public Bitmap Visit(OrExpression expr)
            {
                var left = expr.Left.Accept(this);
                var right = expr.Right.Accept(this);
                left.Or(right);
                return left;
            }

            public Bitmap Visit(NotExpression expr)
            {
                var bitmap = expr.Parameter.Accept(this);
                bitmap.Not();
                return bitmap;
            }

            public Bitmap Visit(AdditionExpression expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(SubtractionExpression expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(MultiplicationExpression expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(DivisionExpression expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(LessThanOperator expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(LessThanOrEqualOperator expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(EqualOperator expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(NotEqualOperator expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(GreaterThanOperator expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(GreaterThanOrEqualOperator expr)
            {
                return ComputeFunction(expr);
            }

            private Bitmap ComputeFunction(ValueExpression expr)
            {
                // Here we assume that a function will be null iff some of its arguments are null.
                // This will be true for many functions but it fails for some (notably the "or" and
                // the "not" functions). Logical operators are therefore handeled differently.
                var bitmap = new Bitmap(_subsets.Count, true);
                foreach (var parameter in expr.Children)
                {
                    var childBitmap = parameter.Accept(this);
                    bitmap.And(childBitmap);
                }

                return bitmap;
            }

            public Bitmap Visit(FunctionCallExpression expr)
            {
                return ComputeFunction(expr);
            }

            public Bitmap Visit(ConstantExpression expr)
            {
                return new Bitmap(_subsets.Count, true);
            }

            public Bitmap Visit(AttributeAccessExpression expr)
            {
                var bitmap = new Bitmap(_subsets.Count);
                // find all subsets which contain this attribute
                foreach (var index in _subsets.FindIndices(item => item == expr.Name))
                {
                    bitmap.Set(index);
                }

                return bitmap;
            }
        }

        private readonly Statistics _statistics;

        public PriorityFunction(Statistics statistics)
        {
            _statistics = statistics;
        }
        
        public double Compute(ValueExpression expression, string path)
        {
            var directory = _statistics.GetDirectory(path);
            if (directory == null)
            {
                return 0;
            }

            // find which subsets of attributes match this expression
            long sum = 0;
            long totalSum = 0;
            var visitor = new PriorityVisitor(_statistics.Attributes);
            var result = expression.Accept(visitor);

            // sum the file counts
            for (var i = 0; i < result.Count; ++i)
            {
                if (result[i])
                {
                    sum += directory[i];
                }

                totalSum += directory[i];
            }

            Trace.Assert(sum <= totalSum);
            
            // compute the probability
            return sum / (double) totalSum;
        }
    }
}
