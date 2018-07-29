using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI
{
    public interface IComponent
    {
        /// <summary>
        /// Function called once when this component should be loaded
        /// </summary>
        /// <param name="app">Application</param>
        void OnStartup(IViewerApplication app);

        /// <summary>
        /// Deserialize window of this component from persist string stored in layout configuration file.
        /// If the component does not recognize the <paramref name="persistString"/>, it has to return null.
        /// </summary>
        /// <param name="persistString">Persist string of the component.</param>
        /// <returns>Deserialized window or null if this component does not recognize <paramref name="persistString"/></returns>
        IDockContent Deserialize(string persistString);
    }
}
