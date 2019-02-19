using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Diagnostics;
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
        /// <summary>
        /// Current user version of the schema initialized by the <see cref="Initialize"/> method.
        /// </summary>
        private const int CurrentVersion = 4;

        private readonly IFileSystem _fileSystem;
        private readonly string _dataSource;
        private bool _isInitialized;
        private readonly object _initializationLock = new object();

        [ImportingConstructor]
        public SQLiteConnectionFactory(IFileSystem fileSystem) 
            : this(fileSystem, Environment.ExpandEnvironmentVariables(Resources.CacheFilePath))
        {
        }
        
        public SQLiteConnectionFactory(IFileSystem fileSystem, string dataSource)
        {
            _fileSystem = fileSystem;
            _dataSource = Path.GetFullPath(dataSource);
        }
        
        /// <summary>
        /// If the default data source is a file, create its directory. This function will create
        /// expected database structure in the default datasource and register all custom functions.
        /// </summary>
        private void Initialize(string dataSource)
        {
            // make sure its directory exists
            _fileSystem.CreateDirectory(Path.GetDirectoryName(dataSource));

            using (var connection = Create(dataSource))
            {
                var version = GetVersion(connection);
                if (version < CurrentVersion)
                {
                    connection.Close();
                    connection.Dispose();
                    SQLiteConnection.ClearAllPools();
                    _fileSystem.DeleteFile(dataSource);
                }
            }

            using (var connection = Create(dataSource))
            {
                SQLiteFunction.RegisterFunction(typeof(InvariantCulture));
                SQLiteFunction.RegisterFunction(typeof(InvariantCultureIgnoreCase));
                SQLiteFunction.RegisterFunction(typeof(GetParentPathFunction));

                var initialization = Resources.Structure.Split(new []{ "----" }, 
                    StringSplitOptions.None);
                using (var command = connection.CreateCommand())
                {
                    foreach (var part in initialization)
                    {
                        command.CommandText = part;
                        command.ExecuteNonQuery();
                    }

                    // set current schema version
                    command.CommandText = "PRAGMA user_version = " + CurrentVersion;
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Create a connection to the default datasource.
        /// </summary>
        /// <returns>New connection.</returns>
        public SQLiteConnection Create()
        {
            lock (_initializationLock)
            {
                if (!_isInitialized)
                {
                    _isInitialized = true;
                    Initialize(_dataSource);
                }
            }

            return Create(_dataSource);
        }

        private long GetVersion(SQLiteConnection connection)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "PRAGMA user_version";
                return (long) command.ExecuteScalar();
            }
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
