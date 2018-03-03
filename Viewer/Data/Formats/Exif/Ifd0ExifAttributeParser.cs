using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using Viewer.Data.Formats.Attributes;

namespace Viewer.Data.Formats.Exif
{
    public class Ifd0ExifAttributeParser : IExifAttributeParser
    {
        /// <summary>
        /// Name of the returned attribute 
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Tag of the attribute in Exif IFD
        /// </summary>
        public int Tag { get; }

        /// <summary>
        /// Type of the returned attribute
        /// </summary>
        public AttributeType Type { get; }

        /// <summary>
        /// Create parser for attribute in IFD0
        /// </summary>
        /// <param name="name">Name of the attributed that will be returned</param>
        /// <param name="tag">Tag id in Exif</param>
        /// <param name="type">Type of the returned attribute</param>
        public Ifd0ExifAttributeParser(string name, int tag, AttributeType type)
        {
            Name = name;
            Tag = tag;
            Type = type;
        }

        /// <summary>
        /// Find the attribute in the parsed exif data
        /// </summary>
        /// <param name="exif">Parsed exif data</param>
        /// <returns>New attribute parsed from the exif or null if there is no such tag</returns>
        public Attribute Parse(ExifMetadata exif)
        {
            var directory = exif.GetDirectoryOfType<ExifIfd0Directory>();
            if (directory == null || !directory.ContainsTag(Tag))
                return null;
            
            switch (Type)
            {
                case AttributeType.Int:
                    return new IntAttribute(Name, AttributeSource.Exif, directory.GetInt32(Tag));
                case AttributeType.Double:
                    return new DoubleAttribute(Name, AttributeSource.Exif, directory.GetDouble(Tag));
                case AttributeType.String:
                    return new StringAttribute(Name, AttributeSource.Exif, directory.GetString(Tag));
                case AttributeType.DateTime:
                    return new DateTimeAttribute(Name, AttributeSource.Exif, directory.GetDateTime(Tag));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
