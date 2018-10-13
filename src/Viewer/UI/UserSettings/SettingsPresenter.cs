using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Deployment.Application;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Core;
using Viewer.Core.UI;
using Viewer.Properties;

namespace Viewer.UI.UserSettings
{
    internal class SettingsPresenter : Presenter<ISettingsView>
    {
        public SettingsPresenter(ISettingsView view)
        {
            View = view;

            var programs = Settings.Default.ExternalApplications.ToList();
            programs.Add(new ExternalApplication()); // editable new application
            View.Programs = programs;

            SubscribeTo(View, "View");
        }

        private void View_ProgramsChanged(object sender, EventArgs e)
        {
            var programs = View.Programs
                .Where(app => !string.IsNullOrWhiteSpace(app.Command))
                .ToArray();

            // update settings
            Settings.Default.ExternalApplications = programs;

            if (View.Programs.Count <= 0 ||
                !string.IsNullOrWhiteSpace(View.Programs[View.Programs.Count - 1].Command))
            {
                var views = programs.ToList();
                views.Add(new ExternalApplication());
                View.Programs = views;
            }
        }
    }
}
