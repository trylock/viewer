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
using Path = System.IO.Path;

namespace Viewer.Core
{
    [Flags]
    public enum ExternalApplicationFlags
    {
        /// <summary>
        /// Applications with this flag can't be run with file paths as their arguments
        /// </summary>
        DisallowFiles = 1,
        
        /// <summary>
        /// Applications with this flag can't be run with directory paths as their arguments
        /// </summary>
        DisallowDirectories = 2,

        /// <summary>
        /// Applications witch this flag can be run with multiple paths as their arguments
        /// </summary>
        AcceptMultiplePaths = 4
    }

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
        /// Command to execute without arguments (i.e. path to executable or name of a system
        /// executable)
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Command line arguments provided after <see cref="Command"/>.
        /// </summary>
        public string Arguments { get; set; }

        /// <summary>
        /// Application options
        /// </summary>
        public ExternalApplicationFlags Flags { get; set; } 

        /// <summary>
        /// Run application with <paramref name="paths"/> as arguments.
        /// <see cref="string.Format(string,object)"/> is used to format <see cref="Arguments"/>
        /// where <paramref name="paths"/> is a parameter. The result is then passed as arguments
        /// to <see cref="Command"/>
        /// </summary>
        /// <remarks>
        /// > [!NOTE]
        /// > It is up to the caller to ensure that the <paramref name="paths"/> argument contains
        /// > correct number of paths with correct types according to <see cref="Flags"/>. This
        /// > method does **not** use this flag.
        /// </remarks>
        /// <param name="paths">Paths to open using this program/script</param>
        /// <exception cref="ArgumentNullException"><paramref name="paths"/> is null</exception>
        /// <exception cref="InvalidOperationException">
        /// <see cref="Command"/> or <see cref="Arguments"/> is null
        /// </exception>
        /// <exception cref="Win32Exception">Starting the process failed</exception>
        /// <seealso cref="Process.Start(string, string)"/>
        public void Run(IEnumerable<string> paths)
        {
            if (paths == null)
                throw new ArgumentNullException(nameof(paths));
            if (Command == null || Arguments == null)
                throw new InvalidOperationException();
            
            var arguments = new StringBuilder();
            arguments.Append(Arguments);
            arguments.Append(' ');
            foreach (var path in paths)
            {
                var fullPath = Path.GetFullPath(path);
                arguments.Append('"');
                arguments.Append(fullPath);
                arguments.Append('"');
                arguments.Append(' ');
            }
            
            var process = Process.Start(Command, arguments.ToString());
            process?.Dispose();
        }
    }
}
