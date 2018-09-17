using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.SQLite
{
    /// <inheritdoc />
    /// <summary>
    /// Custom unicode aware string comparison collation.
    /// </summary>
    [SQLiteFunction("INVARIANT_CULTURE", 2, FunctionType.Collation)]
    public class InvariantCulture : SQLiteFunction
    {
        public override int Compare(string param1, string param2)
        {
            var result = StringComparer.CurrentCultureIgnoreCase.Compare(param1, param2);
            return result;
        }
    }
}
