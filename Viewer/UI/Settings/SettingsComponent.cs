using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Properties;
using WeifenLuo.WinFormsUI.Docking;

namespace Viewer.UI.Settings
{
    [Export(typeof(IComponent))]
    public class SettingsComponent : IComponent
    {
        private readonly ISettings _settings;

        [ImportingConstructor]
        public SettingsComponent(ISettings settings)
        {
            _settings = settings;
        }

        public void OnStartup(IViewerApplication app)
        {
            LoadSettings(Resources.SettingsFilePath);
        }

        public IDockContent Deserialize(string persistString)
        {
            return null;
        }
        
        private void LoadSettings(string settingsFilePath)
        {
            try
            {
                using (var settings = new FileStream(settingsFilePath, FileMode.Open, FileAccess.Read))
                {
                    _settings.Deserialize(settings);
                }
            }
            catch (FileNotFoundException)
            {

            }
        }

        private void SaveSettings(string settingsFilePath)
        {
            using (var settings = new FileStream(settingsFilePath, FileMode.Create, FileAccess.Write))
            {
                _settings.Serialize(settings);
            }
        }
    }
}
