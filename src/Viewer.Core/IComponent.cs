using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.Core
{
    public interface IComponent
    {
        /// <summary>
        /// Current application
        /// </summary>
        IViewerApplication Application { get; set; }

        /// <summary>
        /// Function called once when this component is loaded.
        /// </summary>
        /// <param name="app">Application</param>
        void OnStartup(IViewerApplication app);
    }
}
