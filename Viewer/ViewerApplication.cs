using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.Data.Storage;
using Viewer.IO;
using Viewer.Properties;
using Viewer.UI;
using Viewer.UI.Attributes;
using Viewer.UI.Explorer;
using Viewer.UI.Images;
using Viewer.UI.Log;
using Viewer.UI.Presentation;
using Viewer.UI.Tasks;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer
{
    public interface IViewerApplication
    {
        /// <summary>
        /// Initialize components of the application
        /// </summary>
        void InitializeLayout();

        /// <summary>
        /// Add an option to the View subtree of the application menu
        /// </summary>
        /// <param name="name">Name of the component</param>
        /// <param name="action">Function executed when user clicks on the item</param>
        void AddViewAction(string name, Action action);

        /// <summary>
        /// Run the application
        /// </summary>
        void Run();
    }

    [Export(typeof(IViewerApplication))]
    public class ViewerApplication : IViewerApplication
    {
        private readonly ViewerForm _appForm;
        private readonly IComponent[] _components;
        
        [ImportingConstructor]
        public ViewerApplication(ViewerForm appForm, [ImportMany] IComponent[] components)
        {
            _appForm = appForm;
            _components = components;
        }
        
        public void InitializeLayout()
        {
            foreach (var component in _components)
            {
                component.OnStartup(this);
            }
        }

        public void AddViewAction(string name, Action action)
        {
            _appForm.AddViewAction(name, action);
        }

        public void Run()
        {
            Application.Run(_appForm);
        }
    }
}
