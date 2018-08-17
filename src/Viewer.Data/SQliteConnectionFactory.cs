using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Properties;
using Viewer.IO;

namespace Viewer.Data
{
    public class SQliteConnectionFactory
    {
        [Import] private IFileSystem _fileSystem;

        [Export(typeof(SQLiteConnection))]
        public SQLiteConnection Connection 
        {
            get
            {
                var indexFilePath = Environment.ExpandEnvironmentVariables(Resources.CacheFilePath);
                var indexFileDirectory = Path.GetDirectoryName(indexFilePath);
                _fileSystem.CreateDirectory(indexFileDirectory); 

                var connection = new SQLiteConnection(string.Format(Resources.SqliteConnectionString, indexFilePath));
                connection.Open();

                var initialization = Resources.SqliteInitializationScript.Split(';');
                using (var command = connection.CreateCommand())
                {
                    foreach (var part in initialization)
                    {
                        command.CommandText = part;
                        command.ExecuteNonQuery();
                    }
                }

                return connection;
            }
        }
    }
}
