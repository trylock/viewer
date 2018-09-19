
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data;
using Viewer.Query;

namespace Viewer.QueryRuntime
{
            [Export(typeof(IFunction))]
        public class AndIntValueFunction : IFunction
        {
            public string Name => "and";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.Integer,
                TypeId.Integer,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? arguments[0] : arguments[1];
            }
        }

        [Export(typeof(IFunction))]
        public class OrIntValueFunction : IFunction
        {
            public string Name => "or";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.Integer,
                TypeId.Integer,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? arguments[1] : arguments[0];
            }
        }

        [Export(typeof(IFunction))]
        public class NotIntValueFunction : IFunction
        {
            public string Name => "not";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.Integer,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? new IntValue(1) : new IntValue(null);
            }
        }
            [Export(typeof(IFunction))]
        public class AndRealValueFunction : IFunction
        {
            public string Name => "and";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.Real,
                TypeId.Real,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? arguments[0] : arguments[1];
            }
        }

        [Export(typeof(IFunction))]
        public class OrRealValueFunction : IFunction
        {
            public string Name => "or";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.Real,
                TypeId.Real,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? arguments[1] : arguments[0];
            }
        }

        [Export(typeof(IFunction))]
        public class NotRealValueFunction : IFunction
        {
            public string Name => "not";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.Real,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? new IntValue(1) : new IntValue(null);
            }
        }
            [Export(typeof(IFunction))]
        public class AndStringValueFunction : IFunction
        {
            public string Name => "and";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.String,
                TypeId.String,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? arguments[0] : arguments[1];
            }
        }

        [Export(typeof(IFunction))]
        public class OrStringValueFunction : IFunction
        {
            public string Name => "or";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.String,
                TypeId.String,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? arguments[1] : arguments[0];
            }
        }

        [Export(typeof(IFunction))]
        public class NotStringValueFunction : IFunction
        {
            public string Name => "not";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.String,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? new IntValue(1) : new IntValue(null);
            }
        }
            [Export(typeof(IFunction))]
        public class AndDateTimeValueFunction : IFunction
        {
            public string Name => "and";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.DateTime,
                TypeId.DateTime,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? arguments[0] : arguments[1];
            }
        }

        [Export(typeof(IFunction))]
        public class OrDateTimeValueFunction : IFunction
        {
            public string Name => "or";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.DateTime,
                TypeId.DateTime,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? arguments[1] : arguments[0];
            }
        }

        [Export(typeof(IFunction))]
        public class NotDateTimeValueFunction : IFunction
        {
            public string Name => "not";

            public IReadOnlyList<TypeId> Arguments { get; } = new[]
            {
                TypeId.DateTime,
            };

            public BaseValue Call(IExecutionContext arguments)
            {
                return arguments[0].IsNull ? new IntValue(1) : new IntValue(null);
            }
        }
    }
