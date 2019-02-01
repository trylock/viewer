using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data.Formats
{
    /// <summary>
    /// This class can read attributes from stream and serialize attributes to a stream.
    /// </summary>
    /// <remarks>There can be multiple serializes.</remarks>
    public interface IAttributeSerializer
    {
        /// <summary>
        /// Names of metadata attributes potentially returned by this serializer.
        /// </summary>
        IEnumerable<string> MetadataAttributes { get; }

        /// <summary>
        /// Check if this reader can be used for <paramref name="file"/>.
        /// </summary>
        /// <param name="file">File to test</param>
        /// <returns>
        /// true iff this serializer can read attributes from <paramref name="file"/>
        /// </returns>
        bool CanRead(FileInfo file);

        /// <summary>
        /// Check if this serialize can be used to write attributes in <paramref name="file"/>
        /// </summary>
        /// <param name="file">File to test</param>
        /// <returns>
        /// true iff this serializer can write attributes to <paramref name="file"/>
        /// </returns>
        bool CanWrite(FileInfo file);

        /// <summary>
        /// Read attributes from <paramref name="input"/> stream of <paramref name="file"/>
        /// </summary>
        /// <param name="file">File to read</param>
        /// <param name="input">Input stream of <paramref name="file"/></param>
        /// <returns>Read attributes</returns>
        IEnumerable<Attribute> Deserialize(FileInfo file, Stream input);

        /// <summary>
        /// Serialize <paramref name="attributes"/> from <paramref name="input"/> (read stream of
        /// <paramref name="file"/>) to <paramref name="output"/> stream.
        /// </summary>
        /// <param name="file">File info about file of <paramref name="input"/></param>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="attributes"></param>
        void Serialize(FileInfo file, Stream input, Stream output, IEnumerable<Attribute> attributes);
    }
}
