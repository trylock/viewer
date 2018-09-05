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
using System.Xml;
using NLog;
using NLog.Config;
using NLog.Layouts;
using NLog.Targets;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Properties;
using Viewer.UI;
using WeifenLuo.WinFormsUI.Docking;
using IComponent = Viewer.Core.IComponent;
using LogLevel = NLog.LogLevel;

namespace Viewer
{
    [Export(typeof(IViewerApplication))]
    public class ViewerApplication : IViewerApplication
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ViewerForm _appForm;
        private readonly List<DeserializeCallback> _layoutDeserializeCallback = new List<DeserializeCallback>();
        private readonly IComponent[] _components;

        public DockPanel Panel => _appForm.Panel;

        [ImportingConstructor]
        public ViewerApplication(ViewerForm appForm, [ImportMany] IComponent[] components)
        {
            _components = components;
            _appForm = appForm;
            _appForm.Shutdown += OnShutdown;

            foreach (var component in _components)
            {
                component.Application = this;
            }
        }

        public void InitializeLayout()
        {
            // invoke the startup event
            foreach (var component in _components)
            {
                component.OnStartup(this);
            }
            
            // deserialize layout
            LoadLayout();
        }

        public void AddMenuItem(IReadOnlyList<string> menuPath, Action action, Image icon)
        {
            _appForm.AddMenuItem(menuPath, action, icon);
        }
        
        public IStatusBarItem CreateStatusBarItem(string text, Image image, ToolStripItemAlignment alignment)
        {
            return _appForm.CreateStatusBarItem(text, image, alignment);
        }

        public IStatusBarSlider CreateStatusBarSlider(string text, Image image, ToolStripItemAlignment alignment)
        {
            return _appForm.CreateStatusBarSlider(text, image, alignment);
        }

        public void AddLayoutDeserializeCallback(DeserializeCallback callback)
        {
            _layoutDeserializeCallback.Add(callback);
        }

        private void LoadLayout()
        {
            var layout = Settings.Default.Layout;
            if (layout.Length > 0)
            {
                try
                {
                    using (var input = new MemoryStream(Encoding.UTF8.GetBytes(layout)))
                    {
                        _appForm.Panel.LoadFromXml(input, Deserialize);
                    }
                }
                catch (XmlException e)
                {
                    // configuration file has an invalid format
                    Logger.Error(e, "Invalid configuration file.");
                    Logger.Trace("Layout:\n{0}", layout);
                }
            }

            // apply application settings
            _appForm.Size = Settings.Default.FormSize;
            _appForm.WindowState =
                Settings.Default.FormIsMaximized ? FormWindowState.Maximized : FormWindowState.Normal;
        }
        
        private void OnShutdown(object sender, EventArgs e)
        {
            // hide the application form while we're saving user settings
            _appForm.Hide();

            using (var layout = new MemoryStream())
            {
                _appForm.Panel.SaveAsXml(layout, Encoding.UTF8);
                Settings.Default.Layout = Encoding.UTF8.GetString(layout.ToArray());
            }

            // save settings
            Settings.Default.FormSize = _appForm.Size;
            Settings.Default.FormIsMaximized = _appForm.WindowState == FormWindowState.Maximized;
            Settings.Default.Save();
        }

        private IWindowView Deserialize(string persistString)
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
