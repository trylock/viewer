using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
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
    /// <summary>
    /// This class represents an external application which can open a file from this program.
    /// </summary>
    [SettingsSerializeAs(SettingsSerializeAs.Xml)]
    public class ExternalApplication
    {
        private string _name = "";

        /// <summary>
        /// Name of the operation shown to the user (e.g. "Open in Explorer")
        /// </summary>
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name) && 
                    !string.IsNullOrWhiteSpace(Command) &&
                    Command.IndexOfAny(Path.GetInvalidPathChars()) < 0)
                {
                    return Path.GetFileName(Command);
                }

                return _name;
            }
            set => _name = value;
        } 

        /// <summary>
        /// Command to execute without arguments (i.e., path to executable or name of a system
        /// executable)
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Command line arguments provided after <see cref="Command"/>.
        /// </summary>
        public string Arguments { get; set; } = "";

        /// <summary>
        /// true iff this program can be run with files
        /// </summary>
        [DisplayName("Files")]
        public bool RunWithFiles { get; set; } = true;

        /// <summary>
        /// true iff this program can be run with directories
        /// </summary>
        [DisplayName("Directories")]
        public bool RunWithDirectories { get; set; } = true;

        /// <summary>
        /// true iff this program can be run with multiple paths as its argument
        /// </summary>
        [DisplayName("Allow multiple paths")]
        public bool AllowMultiplePaths { get; set; } = false;

        /// <summary>
        /// Get icon image associated with this program
        /// </summary>
        /// <returns>Icon image or null</returns>
        public Image GetImage()
        {
            try
            {
                using (var icon = Icon.ExtractAssociatedIcon(Command))
                {
                    return icon?.ToBitmap();
                }
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (ArgumentException)
            {
                // invalid path
                return null;
            }
        }

        /// <summary>
        /// Run application with <paramref name="paths"/> as arguments.
        /// <see cref="string.Format(string,object)"/> is used to format <see cref="Arguments"/>
        /// where <paramref name="paths"/> is a parameter. The result is then passed as arguments
        /// to <see cref="Command"/>
        /// </summary>
        /// <remarks>
        /// > [!NOTE]
        /// > It is up to the caller to ensure that the <paramref name="paths"/> argument contains
        /// > correct number of paths with correct type. This method does **not** use the
        /// <see cref="RunWithFiles"/>, <see cref="RunWithDirectories"/> and
        /// <see cref="AllowMultiplePaths"/> properties.
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
