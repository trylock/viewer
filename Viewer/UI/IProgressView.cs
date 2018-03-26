﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.UI
{
    public delegate void WorkDelegate();

    public interface IProgressView
    {
        /// <summary>
        /// Event called when user requests to cancel the operation.
        /// </summary>
        event EventHandler CancelProgress;
        
        /// <summary>
        /// Open a new progress view.
        /// </summary>
        /// <param name="name">Name of the task</param>
        /// <param name="maximum">Total number of units of work to do.</param>
        /// <param name="doWork">Function which starts an intensive computation</param>
        void Show(string name, int maximum, WorkDelegate doWork);

        /// <summary>
        /// Close this progress view
        /// </summary>
        void Hide();

        /// <summary>
        /// Begin a new operation.
        /// It is assumed that the previous operation has been done 
        /// (i.e. a single unit of work has been done).
        /// </summary>
        /// <param name="name">Name of the task</param>
        void StartWork(string name);

        /// <summary>
        /// Called when all the work has been done
        /// </summary>
        void Finish();
    }
}