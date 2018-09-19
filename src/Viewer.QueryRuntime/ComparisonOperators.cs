
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Viewer.Data;
using Viewer.Query;

namespace Viewer.QueryRuntime
{
                [Export(typeof(IFunction))]
            public class IntValueLessThanFunction : IFunction
            {
                public string Name => "<";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<IntValue>(0);
                    var rhs = arguments.Get<IntValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<int>.Default.Compare(
                            (int)lhs.Value, 
                            (int)rhs.Value);
                        if (comparison < 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class IntValueLessThanOrEqualFunction : IFunction
            {
                public string Name => "<=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<IntValue>(0);
                    var rhs = arguments.Get<IntValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<int>.Default.Compare(
                            (int)lhs.Value, 
                            (int)rhs.Value);
                        if (comparison <= 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class IntValueNotEqualFunction : IFunction
            {
                public string Name => "!=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<IntValue>(0);
                    var rhs = arguments.Get<IntValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<int>.Default.Compare(
                            (int)lhs.Value, 
                            (int)rhs.Value);
                        if (comparison != 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class IntValueEqualFunction : IFunction
            {
                public string Name => "=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<IntValue>(0);
                    var rhs = arguments.Get<IntValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<int>.Default.Compare(
                            (int)lhs.Value, 
                            (int)rhs.Value);
                        if (comparison == 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class IntValueGreaterThanOrEqualFunction : IFunction
            {
                public string Name => ">=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<IntValue>(0);
                    var rhs = arguments.Get<IntValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<int>.Default.Compare(
                            (int)lhs.Value, 
                            (int)rhs.Value);
                        if (comparison >= 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class IntValueGreaterThanFunction : IFunction
            {
                public string Name => ">";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<IntValue>(0);
                    var rhs = arguments.Get<IntValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<int>.Default.Compare(
                            (int)lhs.Value, 
                            (int)rhs.Value);
                        if (comparison > 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueLessThanFunction : IFunction
            {
                public string Name => "<";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<RealValue>(0);
                    var rhs = arguments.Get<RealValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<double>.Default.Compare(
                            (double)lhs.Value, 
                            (double)rhs.Value);
                        if (comparison < 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueLessThanOrEqualFunction : IFunction
            {
                public string Name => "<=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<RealValue>(0);
                    var rhs = arguments.Get<RealValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<double>.Default.Compare(
                            (double)lhs.Value, 
                            (double)rhs.Value);
                        if (comparison <= 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueNotEqualFunction : IFunction
            {
                public string Name => "!=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<RealValue>(0);
                    var rhs = arguments.Get<RealValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<double>.Default.Compare(
                            (double)lhs.Value, 
                            (double)rhs.Value);
                        if (comparison != 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueEqualFunction : IFunction
            {
                public string Name => "=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<RealValue>(0);
                    var rhs = arguments.Get<RealValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<double>.Default.Compare(
                            (double)lhs.Value, 
                            (double)rhs.Value);
                        if (comparison == 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueGreaterThanOrEqualFunction : IFunction
            {
                public string Name => ">=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<RealValue>(0);
                    var rhs = arguments.Get<RealValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<double>.Default.Compare(
                            (double)lhs.Value, 
                            (double)rhs.Value);
                        if (comparison >= 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueGreaterThanFunction : IFunction
            {
                public string Name => ">";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<RealValue>(0);
                    var rhs = arguments.Get<RealValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<double>.Default.Compare(
                            (double)lhs.Value, 
                            (double)rhs.Value);
                        if (comparison > 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class StringValueLessThanFunction : IFunction
            {
                public string Name => "<";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.String,
                    TypeId.String
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<StringValue>(0);
                    var rhs = arguments.Get<StringValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<string>.Default.Compare(
                            (string)lhs.Value, 
                            (string)rhs.Value);
                        if (comparison < 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class StringValueLessThanOrEqualFunction : IFunction
            {
                public string Name => "<=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.String,
                    TypeId.String
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<StringValue>(0);
                    var rhs = arguments.Get<StringValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<string>.Default.Compare(
                            (string)lhs.Value, 
                            (string)rhs.Value);
                        if (comparison <= 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class StringValueNotEqualFunction : IFunction
            {
                public string Name => "!=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.String,
                    TypeId.String
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<StringValue>(0);
                    var rhs = arguments.Get<StringValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<string>.Default.Compare(
                            (string)lhs.Value, 
                            (string)rhs.Value);
                        if (comparison != 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class StringValueEqualFunction : IFunction
            {
                public string Name => "=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.String,
                    TypeId.String
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<StringValue>(0);
                    var rhs = arguments.Get<StringValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<string>.Default.Compare(
                            (string)lhs.Value, 
                            (string)rhs.Value);
                        if (comparison == 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class StringValueGreaterThanOrEqualFunction : IFunction
            {
                public string Name => ">=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.String,
                    TypeId.String
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<StringValue>(0);
                    var rhs = arguments.Get<StringValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<string>.Default.Compare(
                            (string)lhs.Value, 
                            (string)rhs.Value);
                        if (comparison >= 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class StringValueGreaterThanFunction : IFunction
            {
                public string Name => ">";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.String,
                    TypeId.String
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<StringValue>(0);
                    var rhs = arguments.Get<StringValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<string>.Default.Compare(
                            (string)lhs.Value, 
                            (string)rhs.Value);
                        if (comparison > 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class DateTimeValueLessThanFunction : IFunction
            {
                public string Name => "<";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.DateTime,
                    TypeId.DateTime
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<DateTimeValue>(0);
                    var rhs = arguments.Get<DateTimeValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<DateTime>.Default.Compare(
                            (DateTime)lhs.Value, 
                            (DateTime)rhs.Value);
                        if (comparison < 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class DateTimeValueLessThanOrEqualFunction : IFunction
            {
                public string Name => "<=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.DateTime,
                    TypeId.DateTime
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<DateTimeValue>(0);
                    var rhs = arguments.Get<DateTimeValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<DateTime>.Default.Compare(
                            (DateTime)lhs.Value, 
                            (DateTime)rhs.Value);
                        if (comparison <= 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class DateTimeValueNotEqualFunction : IFunction
            {
                public string Name => "!=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.DateTime,
                    TypeId.DateTime
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<DateTimeValue>(0);
                    var rhs = arguments.Get<DateTimeValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<DateTime>.Default.Compare(
                            (DateTime)lhs.Value, 
                            (DateTime)rhs.Value);
                        if (comparison != 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class DateTimeValueEqualFunction : IFunction
            {
                public string Name => "=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.DateTime,
                    TypeId.DateTime
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<DateTimeValue>(0);
                    var rhs = arguments.Get<DateTimeValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<DateTime>.Default.Compare(
                            (DateTime)lhs.Value, 
                            (DateTime)rhs.Value);
                        if (comparison == 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class DateTimeValueGreaterThanOrEqualFunction : IFunction
            {
                public string Name => ">=";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.DateTime,
                    TypeId.DateTime
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<DateTimeValue>(0);
                    var rhs = arguments.Get<DateTimeValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<DateTime>.Default.Compare(
                            (DateTime)lhs.Value, 
                            (DateTime)rhs.Value);
                        if (comparison >= 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
                [Export(typeof(IFunction))]
            public class DateTimeValueGreaterThanFunction : IFunction
            {
                public string Name => ">";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.DateTime,
                    TypeId.DateTime
                };

                public BaseValue Call(IExecutionContext arguments)
                {
                    var lhs = arguments.Get<DateTimeValue>(0);
                    var rhs = arguments.Get<DateTimeValue>(1);
                    if (lhs?.Value != null && rhs?.Value != null)
                    {
                        var comparison = Comparer<DateTime>.Default.Compare(
                            (DateTime)lhs.Value, 
                            (DateTime)rhs.Value);
                        if (comparison > 0)
                            return new IntValue(1);
                    }
                    return new IntValue(null);
                }
            }
    }