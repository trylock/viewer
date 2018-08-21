using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Viewer.Core
{
    /// <summary>
    /// This class represents an external application which can open a file from this program.
    /// </summary>
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ExternalApplication
    {
        /// <summary>
        /// Name of the operation shown to the user (i.e. "Open in Explorer")
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Command to execute without arguments (i.e. path to executable or name of a system executable)
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Command line arguments of Path.
        /// Special string {0} will be replaced with a path of a file to open.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Run application with <paramref name="path"/> as an argument.
        /// <see cref="string.Format(string,object)"/> is used to format <see cref="Arguments"/> where <paramref name="path"/> is a parameter.
        /// The result is then passed as arguments to <see cref="Command"/>
        /// </summary>
        /// <param name="path">Path to open in this application</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null</exception>
        /// <exception cref="InvalidOperationException"><see cref="Command"/> or <see cref="Arguments"/> is null</exception>
        /// <exception cref="Win32Exception">Starting the process failed</exception>
        /// <seealso cref="Process.Start(string, string)"/>
        public void Run(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (Command == null || Arguments == null)
                throw new InvalidOperationException();

            var fullPath = Path.GetFullPath(path);
            var fullCommand = string.Format(Arguments, fullPath);
            var process = Process.Start(Command, fullCommand);
            process?.Dispose();
        }
    }
}
