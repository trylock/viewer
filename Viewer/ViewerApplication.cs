using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
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
using IComponent = Viewer.UI.IComponent;

namespace Viewer
{
    public interface IViewerApplication
    {
        /// <summary>
        /// Initialize components of the application
        /// </summary>
        void InitializeLayout();

        /// <summary>
        /// Add an option to the main application menu
        /// </summary>
        /// <param name="menuPath">Name of the menu where to put this item (last item is the name of the new menu item)</param>
        /// <param name="action">Function executed when user clicks on the item</param>
        /// <param name="icon">Icon shown next to the name</param>
        void AddMenuItem(IReadOnlyList<string> menuPath, Action action, Image icon);

        /// <summary>
        /// Add layout deserializa callback.
        /// </summary>
        /// <param name="callback">
        ///     The callback gets a persist string to deserialize.
        ///     If it does not recognize the string, it has to return null.
        ///     If it returns null, the deserializer will try to use another deserialization function.
        /// </param>
        void AddLayoutDeserializeCallback(DeserializeDockContent callback);

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
        private readonly List<DeserializeDockContent> _layoutDeserializeCallback = new List<DeserializeDockContent>();

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
            
            // deserialize layout
            LoadLayout(Resources.LayoutFilePath);
        }

        public void AddMenuItem(IReadOnlyList<string> menuPath, Action action, Image icon)
        {
            _appForm.AddMenuItem(menuPath, action, icon);
        }

        public void AddLayoutDeserializeCallback(DeserializeDockContent callback)
        {
            _layoutDeserializeCallback.Add(callback);
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
            foreach (var callback in _layoutDeserializeCallback)
            {
                var content = callback(persistString);
                if (content != null)
                {
                    return content;
                }
            }

            return null;
        }
        
        public void Run()
        {
            Application.Run(_appForm);
        }
    }
}
