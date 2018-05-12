using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Query
{
    public interface IQueryView : IWindowView
    {
        /// <summary>
        /// Event called when user requests to run the query
        /// </summary>
        event EventHandler RunQuery;

        /// <summary>
        /// Input query
        /// </summary>
        string Query { get; set; }
    }
}
