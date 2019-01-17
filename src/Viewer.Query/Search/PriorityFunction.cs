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
using Viewer.Data.Formats;
using Viewer.Data.SQLite;
using Viewer.IO;
using Viewer.Query.Expressions;
using Attribute = Viewer.Data.Attribute;

namespace Viewer.Query.Search
{
    /// <summary>
    /// This class computes a priority based on an expression and statistics. The priority can
    /// be viewed as a probability that the expression does not evaluate to null (false). 
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
            private readonly HashSet<string> _metadataAttributes;

            public PriorityVisitor(
                SubsetCollection<string> subsets, 
                HashSet<string> metadataAttributes)
            {
                _subsets = subsets ?? throw new ArgumentNullException(nameof(subsets));
                _metadataAttributes = metadataAttributes ?? 
                                      throw new ArgumentNullException(nameof(metadataAttributes));
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

            public Bitmap Visit(UnaryMinusExpression expr)
            {
                return expr.Parameter.Accept(this);
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
                // if this is a metadata attribute, assume it is in all files
                if (_metadataAttributes.Contains(expr.Name))
                {
                    return new Bitmap(_subsets.Count, true);
                }

                // find all subsets which contain this attribute
                var bitmap = new Bitmap(_subsets.Count);
                foreach (var index in _subsets.FindIndices(item => item == expr.Name))
                {
                    bitmap.Set(index);
                }

                return bitmap;
            }
        }

        private readonly IAttributeStatistics _statistics;
        private readonly HashSet<string> _metadataAttributes;

        public PriorityFunction(
            IAttributeStatistics statistics, 
            HashSet<string> metadataAttributes)
        {
            _statistics = statistics;
            _metadataAttributes = metadataAttributes;
        }
        
        public double Compute(ValueExpression expression, string path)
        {
            path = PathUtils.NormalizePath(path);

            var statistics = _statistics.GetStatistics(path);
            if (statistics == null)
            {
                return 0;
            }

            // find which subsets of attributes match this expression
            long sum = 0;
            long totalSum = 0;
            var visitor = new PriorityVisitor(statistics.AttributeSubsets, _metadataAttributes);
            var result = expression.Accept(visitor);

            // count the files which contain these subsets
            for (var i = 0; i < result.Count; ++i)
            {
                if (result[i])
                {
                    sum += statistics.GetSubsetCount(i);
                }

                totalSum += statistics.GetSubsetCount(i);
            }

            Trace.Assert(sum <= totalSum);

            // compute the probability
            return sum / (double) totalSum;
        }
    }
}
