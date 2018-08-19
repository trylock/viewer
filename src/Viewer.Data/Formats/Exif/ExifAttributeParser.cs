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
    public class ExifAttributeParser<T> : IExifAttributeParser where T : ExifDirectoryBase
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
        public ExifAttributeParser(string name, int tag, AttributeType type)
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
        public Attribute Parse(IExifMetadata exif)
        {
            var directory = exif.GetDirectoryOfType<T>();
            if (directory == null || !directory.ContainsTag(Tag))
                return null;

            try
            {
                switch (Type)
                {
                    case AttributeType.Int:
                        return new Attribute(Name, new IntValue(directory.GetInt32(Tag)), AttributeSource.Metadata);
                    case AttributeType.Double:
                        return new Attribute(Name, new RealValue(directory.GetDouble(Tag)), AttributeSource.Metadata);
                    case AttributeType.String:
                        return new Attribute(Name, new StringValue(directory.GetString(Tag)), AttributeSource.Metadata);
                    case AttributeType.DateTime:
                        return new Attribute(Name, new DateTimeValue(directory.GetDateTime(Tag)), AttributeSource.Metadata);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (MetadataException)
            {
                // the tag exists but its format is invalid => ignore it
                return null;
            }
        }
    }
}
