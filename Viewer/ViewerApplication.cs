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
using Viewer.Properties;
using Viewer.UI;
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
        /// <param name="icon">Icon shown next to the name</param>
        void AddViewAction(string name, Action action, Image icon);

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
            _appForm.Shutdown += OnShutdown;
            _components = components;
        }

        public void InitializeLayout()
        {
            // invoke the startup event
            foreach (var component in _components)
            {
                component.OnStartup(this);
            }
            
            LoadLayout(Resources.LayoutFilePath);
        }

        private void LoadLayout(string layoutFilePath)
        {
            try
            {
                using (var input = new FileStream(layoutFilePath, FileMode.Open, FileAccess.Read))
                {
                    _appForm.Panel.LoadFromXml(input, Deserialize);
                }
            }
            catch (FileNotFoundException)
            {
                // configuration file is missing
            }
        }

        private void SaveLayout(string layoutFilePath)
        {
            using (var state = new FileStream(layoutFilePath, FileMode.Create, FileAccess.Write))
            {
                _appForm.Panel.SaveAsXml(state, Encoding.UTF8);
            }
        }

        private void OnShutdown(object sender, EventArgs e)
        {
            SaveLayout(Resources.LayoutFilePath);
        }

        private IDockContent Deserialize(string persistString)
        {
            foreach (var component in _components)
            {
                var content = component.Deserialize(persistString);
                if (content != null)
                {
                    return content;
                }
            }

            return null;
        }

        public void AddViewAction(string name, Action action, Image icon)
        {
            _appForm.AddViewAction(name, action, icon);
        }

        public void Run()
        {
            Application.Run(_appForm);
        }
    }
}
