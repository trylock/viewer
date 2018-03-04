using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
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

        /// <summary>
        /// List of Exif tags to load from every image
        /// </summary>
        public IList<IExifAttributeParser> ExifTags { get; }

        /// <summary>
        /// Connection to the cache DB
        /// </summary>
        public SQLiteConnection CacheConnection { get; }

        private Settings()
        {
            ExifTags = new List<IExifAttributeParser>
            {
                new Ifd0ExifAttributeParser("ImageWidth", ExifIfd0Directory.TagImageWidth, AttributeType.Int),
                new Ifd0ExifAttributeParser("ImageHeight", ExifIfd0Directory.TagImageHeight, AttributeType.Int),
                new Ifd0ExifAttributeParser("Model", ExifIfd0Directory.TagModel, AttributeType.String),
                new Ifd0ExifAttributeParser("Make", ExifIfd0Directory.TagMake, AttributeType.String),
                new ThumbnaiExifAttributeParser()
            };

            CacheConnection = new SQLiteConnection($"Data Source={Environment.CurrentDirectory}/../../../cache.db;Version=3");
            CacheConnection.Open();
        }
    }
}
