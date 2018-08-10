using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Viewer.UI.Settings
{
    /// <summary>
    /// This class represents an external application which can open a file from this program.
    /// </summary>
    public class ExternalApplication
    {
        /// <summary>
        /// Name of the operation shown to the user (i.e. "Open in Explorer")
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Command to execute without arguments (i.e. path to executable or name of a system executable)
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// Command line arguments of Path.
        /// Special string {0} will be replaced with a path of a file to open.
        /// </summary>
        public string Arguments { get; }

        public ExternalApplication(string name, string path, string arguments)
        {
            Name = name;
            Command = path;
            Arguments = arguments;
        }

        /// <summary>
        /// Run application with <paramref name="path"/> as an argument.
        /// </summary>
        /// <param name="path">Path to open in this application</param>
        public void Run(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            var fullPath = Path.GetFullPath(path);
            var fullCommand = string.Format(Arguments, fullPath);
            Process.Start(Command, fullCommand);
        }
    }

    public interface ISettings
    {
        /// <summary>
        /// Event called whenever the settings change
        /// </summary>
        event EventHandler Changed;

        /// <summary>
        /// Path to a directory with views
        /// </summary>
        string QueryViewDirectoryPath { get; }

        /// <summary>
        /// List of applications
        /// </summary>
        ICollection<ExternalApplication> Applications { get; }

        /// <summary>
        /// Serialize settings to the <paramref name="output"/> stream.
        /// </summary>
        /// <param name="output">Stream where the serialized settings will be stored</param>
        void Serialize(Stream output);

        /// <summary>
        /// Deserialize settings from the <paramref name="input"/> stream.
        /// Deserialzied settings will overwrite current settings.
        /// </summary>
        /// <param name="input">Input stream wil serialized settings</param>
        void Deserialize(Stream input);
    }
    
    [Export(typeof(ISettings))]
    public class Settings : ISettings, IXmlSerializable
    {
        public event EventHandler Changed;

        public string QueryViewDirectoryPath { get; private set; } = "./views";
        public ICollection<ExternalApplication> Applications { get; private set; } = new List<ExternalApplication>();

        public void Serialize(Stream output)
        {
            var serializer = new XmlSerializer(typeof(Settings));
            serializer.Serialize(output, this);
        }

        public void Deserialize(Stream input)
        {
            var serializer = new XmlSerializer(typeof(Settings));
            var settings = serializer.Deserialize(input) as Settings;
            if (settings == null)
            {
                return;
            }

            Applications = settings.Applications;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {

            reader.ReadStartElement("Settings");

            // read applications
            Applications.Clear();
            reader.ReadStartElement("Applications");

            while (reader.IsStartElement())
            {
                reader.ReadStartElement("Application");

                var name = reader.ReadElementContentAsString("Name", "");
                var command = reader.ReadElementContentAsString("Command", "");
                var arguments = reader.ReadElementContentAsString("Arguments", "");

                Applications.Add(new ExternalApplication(name, command, arguments));

                reader.ReadEndElement();
            }

            reader.ReadEndElement();

            // read view directory path
            QueryViewDirectoryPath = reader.ReadElementContentAsString("QueryViewDirectoryPath", "");

            reader.ReadEndElement();
        }

        public void WriteXml(XmlWriter writer)
        {
            // write ápplications
            writer.WriteStartElement("Applications");

            foreach (var item in Applications)
            {
                writer.WriteStartElement("Application");
                writer.WriteElementString("Name", item.Name);
                writer.WriteElementString("Command", item.Command);
                writer.WriteElementString("Arguments", item.Arguments);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();

            // save view directory path
            writer.WriteElementString("QueryViewDirectoryPath", QueryViewDirectoryPath);
        }
    }
}
