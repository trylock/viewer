using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Query;
using Viewer.UI.Tasks;

namespace Viewer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var catalog = new AggregateCatalog(
                new AssemblyCatalog(Assembly.GetExecutingAssembly())
            );
            using (var container = new CompositionContainer(catalog))
            {
                var app = container.GetExportedValue<IViewerApplication>();
                app.InitializeLayout();
                app.Run();
            }
        }
    }
}
