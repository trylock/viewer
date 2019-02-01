using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;

namespace Viewer.Data.Formats
{
    /// <summary>
    /// JPEG serializer can read/write attributes in JPEG segments.
    /// 
    /// > [!NOTE]
    /// > There can be multiple implementations of IJpegSerializer. Each implementation will
    /// > read/write specific attributes. Some attributes can be read-only in which case the
    /// > <see cref="Serialize"/> method does not have to serialize any attribute.
    /// </summary>
    public interface IJpegSerializer
    {
        /// <summary>
        /// Names of metadata attributes potentially returned by this serializer
        /// </summary>
        IEnumerable<string> MetadataAttributes { get; }

        /// <summary>
        /// Read all attributes from JPEG <paramref name="segments"/>.
        /// </summary>
        /// <param name="segments">JPEG segments containing metadata</param>
        /// <returns>Parsed segments</returns>
        IEnumerable<Attribute> Deserialize(IReadOnlyList<JpegSegment> segments);

        /// <summary>
        /// Serialize <paramref name="attributes"/> to JPEG segments.
        /// </summary>
        /// <remarks>
        /// There can be multile serializers. Each serializer will serialize custom attribute
        /// subset. Unmodified segments should be returned.
        /// </remarks>
        /// <remarks>Implementation can serialzie arbitrary selection of attributes.</remarks>
        /// <param name="segments">Original JPEG segments</param>
        /// <param name="attributes">Attributes to store</param>
        /// <returns>Modified JPEG segments with serialized attributes</returns>
        List<JpegSegment> Serialize(IReadOnlyList<JpegSegment> segments, IEnumerable<Attribute> attributes);
    }
}
