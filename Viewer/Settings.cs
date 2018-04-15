using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
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
                // image metadata
                new ExifAttributeParser<ExifIfd0Directory>("ImageWidth", ExifIfd0Directory.TagImageWidth, AttributeType.Int),
                new ExifAttributeParser<ExifIfd0Directory>("ImageHeight", ExifIfd0Directory.TagImageHeight, AttributeType.Int),
                new ExifAttributeParser<ExifSubIfdDirectory>("DateTaken", ExifIfd0Directory.TagDateTimeOriginal, AttributeType.DateTime),
                new ThumbnaiExifAttributeParser("thumbnail"),

                // camera metadata
                new ExifAttributeParser<ExifIfd0Directory>("CameraModel", ExifIfd0Directory.TagModel, AttributeType.String),
                new ExifAttributeParser<ExifIfd0Directory>("CameraMaker", ExifIfd0Directory.TagMake, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("ExposureTime", ExifIfd0Directory.TagExposureTime, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("FStop", ExifIfd0Directory.TagFNumber, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("ExposureBias", ExifIfd0Directory.TagExposureBias, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("FocalLength", ExifIfd0Directory.TagFocalLength, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>("MaxAperture", ExifIfd0Directory.TagMaxAperture, AttributeType.String),
            };

            CacheConnection = new SQLiteConnection($"Data Source={Environment.CurrentDirectory}/../../../cache.db;Version=3");
            CacheConnection.Open();

            // enforce foreign keys
            using (var query = new SQLiteCommand(CacheConnection))
            {
                query.CommandText = "PRAGMA foreign_keys = 1";
                query.ExecuteNonQuery();
            }
        }
    }
}
