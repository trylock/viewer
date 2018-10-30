using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.SQLite
{
    /// <summary>
    /// This service provides access to the table of files in the cache database
    /// </summary>
    /// <remarks>
    /// > [!NOTE]
    /// > No operation is done in transaction so that the caller can decide what will be executed
    /// > in a transaction. Methods like <see cref="Move"/> are not atomic and require a transaction.
    /// </remarks>
    public interface IFiles : IDisposable
    {
        /// <summary>
        /// Update the last access time of file at <paramref name="path"/> in the database
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <param name="lastAccessTime">New last access time</param>
        void Touch(string path, DateTime lastAccessTime);

        /// <summary>
        /// Delete file at <paramref name="path"/> in the database
        /// </summary>
        /// <param name="path">Path to a file</param>
        void Delete(string path);

        /// <summary>
        /// Insert a new file to the database
        /// </summary>
        /// <param name="path">Full file path</param>
        /// <param name="lastWriteTime">Last write time to the file</param>
        void Insert(string path, DateTime lastWriteTime);

        /// <summary>
        /// Move file from <paramref name="oldPath"/> to <paramref name="newPath"/>
        /// </summary>
        /// <param name="oldPath">Full path to the old file</param>
        /// <param name="newPath">Full new path to the file</param>
        void Move(string oldPath, string newPath);

        /// <summary>
        /// Find file id of a file at <paramref name="path"/>
        /// </summary>
        /// <param name="path">Full path to a file</param>
        /// <returns>File id or -1 if there is no file with <paramref name="path"/></returns>
        long FindId(string path);

        /// <summary>
        /// Delete all files which have not been accessed since <paramref name="threshold"/>
        /// </summary>
        /// <param name="threshold">Last access time threshold</param>
        void DeleteOutdated(DateTime threshold);
    }
    
    public class Files : IFiles
    {
        private readonly SQLiteConnection _connection;
        private readonly InsertFileCommand _insertFile;
        private readonly DeleteFileCommand _deleteFile;
        private readonly TouchFileCommand _touchFile;
        private readonly SelectFileCommand _selectFile;
        private readonly CleanCommand _clean;

        public Files(SQLiteConnection connection)
        {
            _connection = connection;
            _insertFile = new InsertFileCommand(_connection);
            _deleteFile = new DeleteFileCommand(_connection);
            _selectFile = new SelectFileCommand(_connection);
            _touchFile = new TouchFileCommand(_connection);
            _clean = new CleanCommand(_connection);
        }

        private class InsertFileCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteParameter _lastWriteTime = new SQLiteParameter(":lastWriteTime");
            private readonly SQLiteCommand _command;

            public InsertFileCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = 
                    "INSERT INTO files (path, last_row_access_time, last_file_write_time)" +
                    "VALUES (:path, datetime('now'), :lastWriteTime)";
                _command.Parameters.Add(_path);
                _command.Parameters.Add(_lastWriteTime);
            }

            public void Execute(string path, DateTime lastWriteTime)
            {
                _path.Value = path;
                _lastWriteTime.Value = lastWriteTime.ToUniversalTime();
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        private class DeleteFileCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteCommand _command;

            public DeleteFileCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "DELETE FROM files WHERE path = :path";
                _command.Parameters.Add(_path);
            }

            public void Execute(string path)
            {
                _path.Value = path;
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        private class TouchFileCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteParameter _accessTime = new SQLiteParameter(":accessTime");
            private readonly SQLiteCommand _command;

            public TouchFileCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "UPDATE files SET last_row_access_time = :accessTime " +
                                       "WHERE path = :path";
                _command.Parameters.Add(_path);
                _command.Parameters.Add(_accessTime);
            }

            public void Execute(string path, DateTime accessTime)
            {
                _path.Value = path;
                _accessTime.Value = accessTime.ToUniversalTime();
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        private class SelectFileCommand : IDisposable
        {
            private readonly SQLiteParameter _path = new SQLiteParameter(":path");
            private readonly SQLiteCommand _command;

            public SelectFileCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "SELECT id FROM files WHERE path = :path";
                _command.Parameters.Add(_path);
            }

            public long? Execute(string path)
            {
                _path.Value = path;
                return _command.ExecuteScalar() as long?;
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }

        private class CleanCommand : IDisposable
        {
            private readonly SQLiteParameter _lastAccessTimeThreshold =
                new SQLiteParameter(":lastAccessTimeThreshold");

            private readonly SQLiteCommand _command;

            public CleanCommand(SQLiteConnection connection)
            {
                _command = connection.CreateCommand();
                _command.CommandText = "DELETE FROM files " + 
                                       "WHERE last_row_access_time <= :lastAccessTimeThreshold";
                _command.Parameters.Add(_lastAccessTimeThreshold);
            }

            /// <summary>
            /// Remove all files (and their attributes) from the database whose last access time
            /// is less than <paramref name="lastAccessTimeThreshold"/>.
            /// </summary>
            /// <param name="lastAccessTimeThreshold"></param>
            public void Execute(DateTime lastAccessTimeThreshold)
            {
                _lastAccessTimeThreshold.Value = lastAccessTimeThreshold.ToUniversalTime();
                _command.ExecuteNonQuery();
            }

            public void Dispose()
            {
                _command?.Dispose();
            }
        }
        
        public void Touch(string path, DateTime lastAccessTime)
        {
            _touchFile.Execute(path, lastAccessTime);
        }

        public void Delete(string path)
        {
            _deleteFile.Execute(path);
        }

        public void Insert(string path, DateTime lastWriteTime)
        {
            _insertFile.Execute(path, lastWriteTime);
        }

        public void Move(string oldPath, string newPath)
        {
            Delete(newPath);

            // move file to newPath
            using (var command = _connection.CreateCommand())
            {
                command.CommandText = "UPDATE files SET path = :newPath WHERE path = :oldPath";
                command.Parameters.Add(new SQLiteParameter(":oldPath", oldPath));
                command.Parameters.Add(new SQLiteParameter(":newPath", newPath));
                command.ExecuteNonQuery();
            }
        }

        public long FindId(string path)
        {
            long? id = _selectFile.Execute(path);
            if (id == null)
            {
                return -1;
            }

            return (long) id;
        }

        public void DeleteOutdated(DateTime threshold)
        {
            _clean.Execute(threshold);
        }

        public void Dispose()
        {
            _clean?.Dispose();
            _touchFile?.Dispose();
            _selectFile?.Dispose();
            _deleteFile?.Dispose();
            _insertFile?.Dispose();
            _connection?.Dispose();
        }
    }
}
