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
    public class CoalesceFunction : IFunction
    {
        public string Name => "coalesce";

        public IReadOnlyList<TypeId> Arguments { get; } = new[]
        {
            TypeId.String,
            TypeId.String
        };

        public BaseValue Call(IExecutionContext arguments)
        {
            return arguments.Get<StringValue>(0).IsNull ?
                arguments.Get<StringValue>(1) : 
                arguments.Get<StringValue>(0);
        }
    }
}
