using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;
using NLog.Config;
using NLog.Targets;
using Viewer.Core;
using Viewer.Data;
using Viewer.Properties;
using Viewer.Query;
using Viewer.UI.Tasks;

namespace Viewer
{
    internal static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        static Program()
        {
            var config = new LoggingConfiguration();
            var logFile = Environment.ExpandEnvironmentVariables(Settings.Default.LogFilePath);
            var file = new FileTarget("file")
            {
                FileName = logFile,
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${threadid}|${message}|${exception:format=tostring}"
            };

            config.AddRule(LogLevel.Debug, LogLevel.Fatal, file);
            LogManager.Configuration = config;
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var catalog = new AggregateCatalog(
                new AssemblyCatalog(Assembly.GetExecutingAssembly()),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Core.Fraction))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Data.IEntity))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.Query.IRuntime))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.QueryRuntime.IntValueAdditionFunction))),
                new AssemblyCatalog(Assembly.GetAssembly(typeof(Viewer.IO.IFileSystem)))
            );

            using (var container = new CompositionContainer(catalog))
            {
                var app = container.GetExportedValue<IViewerApplication>();
                app.InitializeLayout();
                app.Run();
            }
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is CompositionException compositionException)
            {
                // It is useless to log composition exception directly as that would include everything 
                // but an information about the error.
                LogCompositionException(compositionException);
                Logger.Fatal(compositionException, "Unhandeled exception.");
            }
            else
            {
                Logger.Fatal(e.ExceptionObject as Exception, "Unhandled exception.");
            }
        }

        private static void LogCompositionException(CompositionException ce)
        {
            foreach (var error in ce.Errors)
            {
                if (error.Exception == null)
                {
                    continue;
                }

                LogException(error.Exception);
            }

            if (ce.InnerException != null)
            {
                LogException(ce.InnerException);
            }
        }
        
        private static void LogException(Exception exception)
        {
            if (exception is CompositionException ce)
            {
                LogCompositionException(ce);
            }
            else if (exception is ComposablePartException cpe)
            {
                if (cpe.InnerException != null)
                {
                    LogException(cpe.InnerException);
                }
            }
            else if (exception != null)
            {
                Logger.Fatal(exception, "Cause of CompositionException whose log follows.");
            }
        }
    }
}
