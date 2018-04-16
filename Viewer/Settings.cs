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
