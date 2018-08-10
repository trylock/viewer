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
                var indexFilePath = string.Format(Resources.CacheFilePath, Environment.CurrentDirectory);
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
