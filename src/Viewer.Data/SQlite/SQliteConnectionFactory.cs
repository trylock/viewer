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

namespace Viewer.Data.SQLite
{
    [Export]
    public class SQLiteConnectionFactory
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _dataSource;

        [ImportingConstructor]
        public SQLiteConnectionFactory(IFileSystem fileSystem) 
            : this(fileSystem, Environment.ExpandEnvironmentVariables(Resources.CacheFilePath))
        {
        }
        
        public SQLiteConnectionFactory(IFileSystem fileSystem, string dataSource)
        {
            _fileSystem = fileSystem;
            _dataSource = Path.GetFullPath(dataSource);
            Initialize(_dataSource);
        }
        
        /// <summary>
        /// If the default data source is a file, create its directory. This function will create
        /// expected database structure in the default datasource and registre all custom functions.
        /// </summary>
        private void Initialize(string dataSource)
        {
            // make sure its directory exists
            _fileSystem.CreateDirectory(Path.GetDirectoryName(dataSource));

            using (var connection = Create(dataSource))
            {
                var initialization = Resources.SqliteInitializationScript.Split(';');
                using (var command = connection.CreateCommand())
                {
                    foreach (var part in initialization)
                    {
                        command.CommandText = part;
                        command.ExecuteNonQuery();
                    }
                }

                SQLiteFunction.RegisterFunction(typeof(CurrentCultureIgnoreCase));
            }
        }

        /// <summary>
        /// Create a connection to the default datasource.
        /// </summary>
        /// <returns>New connection.</returns>
        public SQLiteConnection Create()
        {
            return Create(_dataSource);
        }

        /// <summary>
        /// Create a new connection to <paramref name="dataSource"/>.
        /// </summary>
        /// <param name="dataSource"></param>
        /// <returns></returns>
        private SQLiteConnection Create(string dataSource)
        {
            var connection = new SQLiteConnection(string.Format(Resources.SqliteConnectionString, dataSource));
            connection.Open();
            return connection;
        }
    }
}
