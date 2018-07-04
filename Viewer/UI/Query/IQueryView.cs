using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI.Query
{
    public class OpenQueryEventArgs : EventArgs
    {
        /// <summary>
        /// Full path to a file to open
        /// </summary>
        public string FullPath { get; }

        public OpenQueryEventArgs(string fullPath)
        {
            FullPath = fullPath;
        }
    }

    public interface IQueryView : IWindowView
    {
        /// <summary>
        /// Event called when user requests to run the query
        /// </summary>
        event EventHandler RunQuery;

        /// <summary>
        /// Event called when user changes the query
        /// </summary>
        event EventHandler QueryChanged;

        /// <summary>
        /// Event called when user tries to save the query to a file
        /// </summary>
        event EventHandler SaveQuery;

        /// <summary>
        /// Event called when user requests to open a new query
        /// </summary>
        event EventHandler<OpenQueryEventArgs> OpenQuery;
        
        /// <summary>
        /// Input query
        /// </summary>
        string Query { get; set; }
    }
}
