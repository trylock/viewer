using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MetadataExtractor.Formats.Exif;
using Viewer.Data.Formats;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Exif;

namespace Viewer
{
    public class Settings
    {
        public static Settings Instance = new Settings();

        public IList<IExifAttributeParser> ExifTags { get; }

        public Settings()
        {
            ExifTags = new List<IExifAttributeParser>
            {
                new Ifd0ExifAttributeParser("ImageWidth", ExifIfd0Directory.TagImageWidth, AttributeType.Int),
                new Ifd0ExifAttributeParser("ImageHeight", ExifIfd0Directory.TagImageHeight, AttributeType.Int),
                new Ifd0ExifAttributeParser("Model", ExifIfd0Directory.TagModel, AttributeType.String),
                new Ifd0ExifAttributeParser("Make", ExifIfd0Directory.TagMake, AttributeType.String),
                new ThumbnaiExifAttributeParser()
            };
        }
    }
}
