using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.IO;
using Viewer.Data.Formats;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Jpeg;
using Viewer.IO;

namespace Viewer.Data.Storage
{
    /// <inheritdoc />
    /// <summary>
    /// Attribute storage which stores attributes directly in JPEG files.
    /// </summary>
    [Export]
    public class FileSystemAttributeStorage : IAttributeStorage
    {
        private readonly IFileSystem _fileSystem;
        private readonly IAttributeSerializer[] _serializers;

        /// <summary>
        /// Create a file system attribute storage
        /// </summary>
        /// <param name="fileSystem">A service used to access file system.</param>
        /// <param name="serializers">
        /// All available attribute serializers
        /// </param>
        [ImportingConstructor]
        public FileSystemAttributeStorage(
            IFileSystem fileSystem,
            [ImportMany] IAttributeSerializer[] serializers)
        {
            _fileSystem = fileSystem;
            _serializers = serializers;
        }
        
        public IReadableAttributeStorage CreateReader()
        {
            return new ReadableStorageProxy(this);
        }

        /// <inheritdoc />
        /// <summary>
        /// Load attributes from given file. The first capable serializer is used.
        /// </summary>
        /// <param name="path">Path to a file</param>
        /// <returns>Collection of attributes read from the file</returns>
        public IEntity Load(string path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            
            try
            {
                if (_fileSystem.DirectoryExists(path))
                {
                    return new DirectoryEntity(path);
                }
                
                var fileInfo = new FileInfo(path);

                // read all attributes from the file
                IEnumerable<Attribute> attributes = Enumerable.Empty<Attribute>();
                using (var input = _fileSystem.OpenRead(path))
                {
                    foreach (var serializer in _serializers)
                    {
                        if (!serializer.CanRead(fileInfo))
                        {
                            continue;
                        }

                        var result = serializer.Deserialize(fileInfo, input);
                        attributes = attributes.Concat(result);
                    }
                }

                // add attributes to a new entity
                IEntity entity = new FileEntity(path);
                foreach (var attribute in attributes)
                {
                    entity.SetAttribute(attribute);
                }

                return entity;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (DirectoryNotFoundException)
            {
                return null;
            }
        }

        public void Store(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // find capable serializer
            var file = new FileInfo(entity.Path);
            var serialzier = _serializers.FirstOrDefault(item => item.CanWrite(file));
            if (serialzier == null)
            {
                throw new InvalidDataFormatException(0, 
                    $"Unsupported file format: {entity.Path}");
            }

            // create a temporary file and serialize all attributes there
            string tmpFilePath = null;
            using (var input = _fileSystem.OpenRead(entity.Path))
            using (var output = _fileSystem.CreateTemporaryFile(entity.Path, out tmpFilePath))
            {
                serialzier.Serialize(file, input, output, entity);
            }

            // replace the original file with the modified file
            _fileSystem.ReplaceFile(tmpFilePath, entity.Path, null);
        }
        
        public void StoreThumbnail(IEntity entity)
        {
            // this check is only included to be consistent with the API specification
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
        }

        public void Delete(IEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (entity is FileEntity)
            {
                _fileSystem.DeleteFile(entity.Path);
            }
            else if (entity is DirectoryEntity)
            {
                _fileSystem.DeleteDirectory(entity.Path, true);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(entity));
            }
        }

        public void Move(IEntity entity, string newPath)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            if (newPath == null)
                throw new ArgumentNullException(nameof(newPath));

            if (entity is FileEntity)
            {
                _fileSystem.MoveFile(entity.Path, newPath);
            }
            else if (entity is DirectoryEntity)
            {
                _fileSystem.MoveDirectory(entity.Path, newPath);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(entity));
            }
        }
        
        public void Dispose()
        {
        }
    }
}
