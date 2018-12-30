using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Localization;

namespace Viewer.UI.UserSettings
{
    [Export(typeof(IComponent))]
    public class UserSettingsComponent : Component
    { 
        private SettingsPresenter _settingsPresenter;

        public override void OnStartup(IViewerApplication app)
        {
            base.OnStartup(app);
            
            app.AddLayoutDeserializeCallback(Deserialize);
            app.AddMenuItem(new []{ Strings.File_Label, Strings.Settings_Label }, OpenSettings, null);
        }

        private IWindowView Deserialize(string persiststring)
        {
            if (persiststring == typeof(SettingsView).FullName)
            {
                return CreateSettings();
            }

            return null;
        }

        private IWindowView CreateSettings()
        {
            if (_settingsPresenter != null)
            {
                _settingsPresenter.View.EnsureVisible();
            }
            else
            {
                _settingsPresenter = new SettingsPresenter(new SettingsView());
                _settingsPresenter.View.CloseView += (sender, args) =>
                {
                    _settingsPresenter?.Dispose();
                    _settingsPresenter = null;
                };
            }

            return _settingsPresenter.View;
        }

        private void OpenSettings()
        {
            var view = CreateSettings();
            view.Show(Application.Panel);
        }
    }
}
