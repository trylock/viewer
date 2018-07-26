using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Viewer.Data;
using Viewer.Properties;
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
                new AssemblyCatalog(Assembly.GetExecutingAssembly()),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Data.IEntity))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Query.IRuntime))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.QueryRuntime.IntValueAdditionFunction))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.IO.IFileSystem)))
            );

            using (var container = new CompositionContainer(catalog))
            {
                var indexFilePath = string.Format(Resources.CacheFilePath, Environment.CurrentDirectory);
                var connection = new SQLiteConnection(string.Format(Resources.SqliteConnectionString, indexFilePath));
                connection.Open();
                container.ComposeExportedValue(connection);

                var app = container.GetExportedValue<IViewerApplication>();
                app.InitializeLayout();
                app.Run();
            }
        }
    }
}
