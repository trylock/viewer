using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Viewer.IO;

namespace ViewerTest.IO
{
    public class FileSystemMock : IFileSystem
    {
        private HashSet<string> _directories = new HashSet<string>();
        private HashSet<string> _files = new HashSet<string>();
        
        // Mock interface
        public void AddDirectory(string path)
        {
            _directories.Add(path);
        }

        public void AddFile(string path)
        {
            _directories.Add(path);
        }

        // Mocked interface
        public long CountFiles(IEnumerable<string> paths, bool isRecursive)
        {
            long count = 0;
            foreach (var path in paths)
            {
                if (_files.Contains(path))
                {
                    ++count;
                }
                else if (isRecursive)
                {
                    foreach (var file in _files)
                    {
                        if (file.StartsWith(path))
                        {
                            ++count;
                        }
                    }
                }
            }

            return count;
        }

        public void CopyFile(string sourcePath, string destPath)
        {
            if (sourcePath == null)
                throw new ArgumentNullException(nameof(sourcePath));
            if (destPath == null)
                throw new ArgumentNullException(nameof(destPath));
            if (!_files.Contains(sourcePath))
                throw new FileNotFoundException(sourcePath);
            if (_files.Contains(destPath))
                throw new IOException($"File {destPath} already exists.");
            _files.Add(destPath);
        }

        public void MoveFile(string sourcePath, string destPath)
        {
            if (sourcePath == null)
                throw new ArgumentNullException(nameof(sourcePath));
            if (destPath == null)
                throw new ArgumentNullException(nameof(destPath));
            if (!_files.Contains(sourcePath))
                throw new IOException($"File {sourcePath} was not found.");
            if (_files.Contains(destPath))
                throw new IOException($"File {destPath} already exists.");
            _files.Remove(sourcePath);
            _files.Add(destPath);
        }

        public void MoveDirectory(string sourcePath, string destPath)
        {
            if (sourcePath == null)
                throw new ArgumentNullException(nameof(sourcePath));
            if (destPath == null)
                throw new ArgumentNullException(nameof(destPath));
            if (!_directories.Contains(sourcePath))
                throw new DirectoryNotFoundException(sourcePath);
            if (_directories.Contains(destPath))
                throw new IOException($"Directory {destPath} already exists");
            _directories.Remove(sourcePath);
            _directories.Add(destPath);
        }

        public void CreateDirectory(string path)
        {
            _directories.Add(path);
        }

        public void DeleteDirectory(string path, bool isRecursive)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (!_directories.Contains(path))
                throw new DirectoryNotFoundException(path);

            _directories.Remove(path);
        }

        public FileAttributes GetAttributes(string path)
        {
            if (_directories.Contains(path))
            {
                return FileAttributes.Directory;
            }
            else if (_files.Contains(path))
            {
                return FileAttributes.Normal;
            }
            else
            {
                return default(FileAttributes);
            }
        }

        public void Search(string path, SearchCallback directoryCallback, SearchCallback fileCallback)
        {
            foreach (var dir in _directories)
            {
                if (dir.StartsWith(path))
                {
                    directoryCallback(dir);
                }
            }

            foreach (var file in _files)
            {
                if (file.StartsWith(path))
                {
                    fileCallback(path);
                }
            }
        }
    }
}
