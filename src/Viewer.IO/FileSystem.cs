using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.IO
{
    public enum SearchControl
    {
        /// <summary>
        /// If this is a directory, we won't search its subdirectories
        /// </summary>
        None,

        /// <summary>
        /// If this is a directory, we will visit all its subdirectories
        /// </summary>
        Visit,

        /// <summary>
        /// Stop the search.
        /// </summary>
        Abort,
    }
    
    /// <summary>
    /// Use this as argument to <see cref="IFileSystem.Search"/> to control the search algorithm.
    /// </summary>
    public interface ISearchListener
    {
        /// <summary>
        /// Function called whenever a directory is found.
        /// </summary>
        /// <param name="path">Path to the directory</param>
        SearchControl OnDirectory(string path);

        /// <summary>
        /// Function called whenever a file is found.
        /// </summary>
        /// <param name="path">Path to the file</param>
        SearchControl OnFile(string path);
    }
    
    /// <summary>
    /// Mockable wrapper for static methods in <see cref="File"/> and <see cref="Directory"/>.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Copy file from <paramref name="sourcePath"/> to <paramref name="destPath"/>.
        /// </summary>
        /// <inheritdoc cref="File.Copy(string, string)"/>
        void CopyFile(string sourcePath, string destPath);

        /// <summary>
        /// Move file from <paramref name="sourcePath"/> to <paramref name="destPath"/>.
        /// </summary>
        /// <inheritdoc cref="File.Move(string, string)"/>
        void MoveFile(string sourcePath, string destPath);

        /// <summary>
        /// Delete file <paramref name="path"/>
        /// </summary>
        /// <param name="path"></param>
        /// <inheritdoc cref="File.Delete(string)"/>
        void DeleteFile(string path);

        /// <summary>
        /// Replace file at <paramref name="destPath"/> with <paramref name="sourcePath"/> using <paramref name="backupFile"/> as a backup file
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        /// <param name="backupFile"></param>
        /// <inheritdoc cref="File.Replace(string,string,string)"/>
        void ReplaceFile(string sourcePath, string destPath, string backupFile);

        /// <summary>
        /// Move a directory to <paramref name="destPath"/>
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destPath"></param>
        /// <inheritdoc cref="Directory.Move(string, string)"/>
        void MoveDirectory(string sourcePath, string destPath);

        /// <summary>
        /// Create a new directory
        /// </summary>
        /// <inheritdoc cref="Directory.CreateDirectory(string)"/>
        void CreateDirectory(string path);

        /// <summary>
        /// Delete a directory
        /// </summary>
        /// <param name="path">Path to a directory</param>
        /// <param name="isRecursive"></param>
        /// <inheritdoc cref="Directory.Delete(string, bool)"/>
        void DeleteDirectory(string path, bool isRecursive);

        /// <summary>
        /// Get file/folder attributes
        /// </summary>
        /// <param name="path">Path to a file or a folder</param>
        /// <returns>Attributes of given file</returns>
        /// <inheritdoc cref="File.GetAttributes(string)"/>
        FileAttributes GetAttributes(string path);

        /// <summary>
        /// Get files in given directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.EnumerateFiles(string)"/>
        IEnumerable<string> EnumerateFiles(string path);

        /// <summary>
        /// Get files in given directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.EnumerateFiles(string, string)"/>
        IEnumerable<string> EnumerateFiles(string path, string pattern);

        /// <summary>
        /// Get files in given directory
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.EnumerateDirectories(string)"/>
        IEnumerable<string> EnumerateDirectories(string path);

        /// <summary>
        /// Get files in given directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.EnumerateDirectories(string, string)"/>
        IEnumerable<string> EnumerateDirectories(string path, string pattern);

        /// <summary>
        /// Check whether there is a directory at <paramref name="path"/>
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <inheritdoc cref="Directory.Exists(string)"/>
        bool DirectoryExists(string path);

        /// <summary>
        /// Read the whole file into memory as a byte array
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <inheritdoc cref="File.ReadAllBytes"/>
        byte[] ReadAllBytes(string path);

        /// <summary>
        /// Read the whole file into memory as a string
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <returns></returns>
        /// <inheritdoc cref="File.ReadAllText(string)"/>
        string ReadAllText(string path);

        /// <summary>
        /// Read the whole file asynchronously
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<string> ReadToEndAsync(string path);

        /// <summary>
        /// <para>
        /// Search given path for files and subdirectories. <see cref="ISearchListener.OnDirectory"/>
        /// will be called for each directory, <see cref="ISearchListener.OnFile"/> will be called
        /// for each file.
        /// </para>
        ///
        /// <para>
        /// If <see cref="ISearchListener.OnDirectory"/> returns <see cref="SearchControl.Visit"/>,
        /// the algorithm will search all its subdirectories. If <see cref="ISearchListener.OnDirectory"/>
        /// does not return <see cref="SearchControl.Visit"/>, its subdirectories and files won't
        /// be searched. If either <see cref="ISearchListener.OnDirectory"/> or
        /// <see cref="ISearchListener.OnFile"/> returns <see cref="SearchControl.Abort"/>, the
        /// search will stop immediately.
        /// </para>
        ///
        /// <para>
        /// If <paramref name="path"/> is a file, <see cref="ISearchListener.OnFile"/> will be called.
        /// </para>
        /// </summary>
        /// <param name="path">Path to a file or directory</param>
        /// <param name="listener">
        /// Whenever a new directory is discovered, <see cref="ISearchListener.OnDirectory"/> is called.
        /// Whenever a new file is discovered, <see cref="ISearchListener.OnFile"/> is called.
        /// </param>
        void Search(string path, ISearchListener listener);
    }

    [Export(typeof(IFileSystem))]
    public class FileSystem : IFileSystem
    {
        public void CopyFile(string sourcePath, string destPath)
        {
            File.Copy(sourcePath, destPath);
        }

        public void MoveFile(string sourcePath, string destPath)
        {
            File.Move(sourcePath, destPath);
        }

        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        public void ReplaceFile(string sourcePath, string destPath, string backupFile)
        {
            File.Replace(sourcePath, destPath, backupFile);
        }

        public void MoveDirectory(string sourcePath, string destPath)
        {
            Directory.Move(sourcePath, destPath);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void DeleteDirectory(string path, bool isRecursive)
        {
            Directory.Delete(path, isRecursive);
        }

        public FileAttributes GetAttributes(string path)
        {
            return File.GetAttributes(path);
        }

        public IEnumerable<string> EnumerateFiles(string path)
        {
            return Directory.EnumerateFiles(path);
        }

        public IEnumerable<string> EnumerateFiles(string path, string pattern)
        {
            return Directory.EnumerateFiles(path, pattern);
        }

        public IEnumerable<string> EnumerateDirectories(string path)
        {
            return Directory.EnumerateDirectories(path);
        }

        public IEnumerable<string> EnumerateDirectories(string path, string pattern)
        {
            return Directory.EnumerateDirectories(path, pattern);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public string ReadAllText(string path)
        {
            return File.ReadAllText(path);
        }
        
        public async Task<string> ReadToEndAsync(string path)
        {
            using (var reader = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public void Search(string path, ISearchListener listener)
        {
            var attrs = File.GetAttributes(path);
            if (!attrs.HasFlag(FileAttributes.Directory))
            {
                listener.OnFile(path);
                return;
            }

            var stack = new Stack<string>();
            stack.Push(path);

            while (stack.Count > 0)
            {
                var dir = stack.Pop();
                var result = listener.OnDirectory(dir);
                if (result == SearchControl.Abort)
                {
                    return;
                }

                if (result == SearchControl.Visit)
                {
                    // find files
                    foreach (var file in Directory.EnumerateFiles(dir))
                    {
                        var fileResult = listener.OnFile(file);
                        if (fileResult == SearchControl.Abort)
                        {
                            return;
                        }
                    }

                    // find directories
                    foreach (var subdir in Directory.EnumerateDirectories(dir))
                    {
                        stack.Push(subdir);
                    }
                }
            }
        }
    }
}
