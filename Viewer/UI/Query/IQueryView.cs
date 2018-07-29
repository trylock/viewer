﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

    public interface IDropView
    {
        /// <summary>
        /// Event called when user drops something into the view
        /// </summary>
        event EventHandler<DragEventArgs> OnDrop;
    }

    public interface IQueryView : IDropView, IWindowView
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
        /// Full path to a file which contains this query or
        /// null if there is no such file.
        /// </summary>
        string FullPath { get; set; }

        /// <summary>
        /// Input query
        /// </summary>
        string Query { get; set; }

        /// <summary>
        /// Prompts user to pick a file where the query should be saved
        /// </summary>
        /// <returns>Path to selected file or null if user did not select a file</returns>
        string PickFileForWrite();
    }
}
