using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI
{
    public abstract class Presenter 
    {
        [Import]
        private ViewerForm _appForm;
        
        /// <summary>
        /// Main view of the presenter
        /// </summary>
        public abstract IWindowView MainView { get; }

        /// <summary>
        /// Show presenter's view
        /// </summary>
        /// <param name="dockState">Dock state of the view</param>
        public virtual void ShowView(DockState dockState)
        {
            MainView.Show(_appForm.Panel, dockState);
        }
    }
}
