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
using Viewer.Core;
using Viewer.Properties;
using Viewer.UI;
using WeifenLuo.WinFormsUI.Docking;
using IComponent = Viewer.Core.IComponent;

namespace Viewer
{
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
