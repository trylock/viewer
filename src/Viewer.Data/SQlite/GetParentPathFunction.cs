using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.IO;

namespace Viewer.Data.SQLite
{
    [SQLiteFunction(Name = "getParentPath", Arguments = 1, FuncType = FunctionType.Scalar)]
    public class GetParentPathFunction : SQLiteFunction
    {
        public override object Invoke(object[] args)
        {
            var path = args[0].ToString();
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var pattern = new PathPattern(path);
            var parent = pattern.GetParent();
            return parent?.Text;
        }
    }
}
