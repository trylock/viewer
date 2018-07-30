using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.Data.Properties;

namespace Viewer.Data
{
    public class SQliteConnectionFactory
    {
        [Export(typeof(SQLiteConnection))]
        public SQLiteConnection Connection 
        {
            get
            {
                var indexFilePath = string.Format(Resource.CacheFilePath, Environment.CurrentDirectory);
                var connection = new SQLiteConnection(string.Format(Resource.SqliteConnectionString, indexFilePath));
                connection.Open();

                var initialization = Resource.SqliteInitializationScript.Split(';');
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
