using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.UI.Tasks;

namespace Viewer
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var catalog = new AggregateCatalog(
                new AssemblyCatalog(Assembly.GetExecutingAssembly())
            );
            using (var container = new CompositionContainer(catalog))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                var app = container.GetExportedValue<ViewerApplication>();
                app.InitializeLayout();
                app.Run();
            }
        }
    }
}
