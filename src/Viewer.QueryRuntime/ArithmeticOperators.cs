
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Viewer.Data;
using Viewer.Query;

namespace Viewer.QueryRuntime
{
                [Export(typeof(IFunction))]
            public class IntValueAdditionFunction : IFunction
            {
                public string Name => "+";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext context)
                {
                    var lhs = context.Get<IntValue>(0);
                    var rhs = context.Get<IntValue>(1);
                    
                    
                    return new IntValue(lhs.Value + rhs.Value);
                }
            }
                [Export(typeof(IFunction))]
            public class IntValueSubtractionFunction : IFunction
            {
                public string Name => "-";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext context)
                {
                    var lhs = context.Get<IntValue>(0);
                    var rhs = context.Get<IntValue>(1);
                    
                    
                    return new IntValue(lhs.Value - rhs.Value);
                }
            }
                [Export(typeof(IFunction))]
            public class IntValueMultiplicationFunction : IFunction
            {
                public string Name => "*";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext context)
                {
                    var lhs = context.Get<IntValue>(0);
                    var rhs = context.Get<IntValue>(1);
                    
                    
                    return new IntValue(lhs.Value * rhs.Value);
                }
            }
                [Export(typeof(IFunction))]
            public class IntValueDivisionFunction : IFunction
            {
                public string Name => "/";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Integer,
                    TypeId.Integer
                };

                public BaseValue Call(IExecutionContext context)
                {
                    var lhs = context.Get<IntValue>(0);
                    var rhs = context.Get<IntValue>(1);
                    
                                        if (rhs.Value == 0)
                    {
                        return context.Error("Division by zero.");
                    }
                    
                    return new IntValue(lhs.Value / rhs.Value);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueAdditionFunction : IFunction
            {
                public string Name => "+";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext context)
                {
                    var lhs = context.Get<RealValue>(0);
                    var rhs = context.Get<RealValue>(1);
                    
                    
                    return new RealValue(lhs.Value + rhs.Value);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueSubtractionFunction : IFunction
            {
                public string Name => "-";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext context)
                {
                    var lhs = context.Get<RealValue>(0);
                    var rhs = context.Get<RealValue>(1);
                    
                    
                    return new RealValue(lhs.Value - rhs.Value);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueMultiplicationFunction : IFunction
            {
                public string Name => "*";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext context)
                {
                    var lhs = context.Get<RealValue>(0);
                    var rhs = context.Get<RealValue>(1);
                    
                    
                    return new RealValue(lhs.Value * rhs.Value);
                }
            }
                [Export(typeof(IFunction))]
            public class RealValueDivisionFunction : IFunction
            {
                public string Name => "/";

                public IReadOnlyList<TypeId> Arguments { get; } = new[]
                {
                    TypeId.Real,
                    TypeId.Real
                };

                public BaseValue Call(IExecutionContext context)
                {
                    var lhs = context.Get<RealValue>(0);
                    var rhs = context.Get<RealValue>(1);
                    
                    
                    return new RealValue(lhs.Value / rhs.Value);
                }
            }
    
    [Export(typeof(IFunction))]
    public class StringValueAdditionFunction : IFunction
    {
        public string Name => "+";

        public IReadOnlyList<TypeId> Arguments { get; } = new[]
        {
            TypeId.String,
            TypeId.String
        };

        public BaseValue Call(IExecutionContext arguments)
        {
            var lhs = arguments.Get<StringValue>(0);
            var rhs = arguments.Get<StringValue>(1);
            return new StringValue(lhs.Value + rhs.Value);
        }
    }
}