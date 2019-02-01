using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor.IO;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Exif;
using Viewer.Data.Formats.Jpeg;
using Directory = MetadataExtractor.Directory;

namespace Viewer.Data.Formats.Exif
{
    public interface IExifMetadata
    {
        /// <summary>
        /// Get segment which contains the exif metadata
        /// </summary>
        JpegSegment Segment { get; }

        /// <summary>
        /// Get parsed exif directory
        /// </summary>
        /// <typeparam name="T">Type of the directory</typeparam>
        /// <returns>Directory in the segment or null</returns>
        T GetDirectoryOfType<T>() where T : class;
    }

    public class ExifMetadata : IExifMetadata
    {
        private readonly IReadOnlyList<Directory> _directories;

        public JpegSegment Segment { get; }

        public ExifMetadata(JpegSegment segment, IReadOnlyList<Directory> directories)
        {
            Segment = segment;
            _directories = directories;
        }

        public T GetDirectoryOfType<T>() where T : class
        {
            return _directories?.OfType<T>().FirstOrDefault();
        }
    }

    [Export(typeof(IJpegSerializer))]
    public class ExifJpegSerializer : IJpegSerializer
    {
        private readonly IList<IExifAttributeParser> _tags;

        private const string ExifHeader = "Exif\0\0";

        // exported attribute names
        public const string Width = "ImageWidth";
        public const string Height = "ImageHeight";
        public const string Orientation = "orientation";
        public const string Thumbnail = "thumbnail";
        public const string DateTaken = "DateTaken";
        public const string CameraModel = "CameraModel";
        public const string CameraMaker = "CameraMaker";
        public const string ExposureTime = "ExposureTime";
        public const string FStop = "FStop";
        public const string ExposureBias = "ExposureBias";
        public const string FocalLength = "FocalLength";
        public const string MaxAperture = "MaxAperture";

        public IEnumerable<string> MetadataAttributes
        {
            get
            {
                yield return Width;
                yield return Height;
                yield return DateTaken;
                yield return Orientation;
                yield return Thumbnail;
                yield return CameraModel;
                yield return CameraMaker;
                yield return ExposureTime;
                yield return FStop;
                yield return ExposureBias;
                yield return FocalLength;
                yield return MaxAperture;
            }
        }

        public ExifJpegSerializer()
        {
            _tags = new List<IExifAttributeParser>
            {
                // image metadata
                new ExifAttributeParser<ExifIfd0Directory>(Width, ExifIfd0Directory.TagImageWidth, AttributeType.Int),
                new ExifAttributeParser<ExifIfd0Directory>(Height, ExifIfd0Directory.TagImageHeight, AttributeType.Int),
                new ExifAttributeParser<ExifSubIfdDirectory>(DateTaken, ExifIfd0Directory.TagDateTimeOriginal, AttributeType.DateTime),
                new ExifAttributeParser<ExifIfd0Directory>(Orientation, ExifIfd0Directory.TagOrientation, AttributeType.Int),
                new ThumbnaiExifAttributeParser<ExifThumbnailDirectory>(Thumbnail),

                // camera metadata
                new ExifAttributeParser<ExifIfd0Directory>(CameraModel, ExifIfd0Directory.TagModel, AttributeType.String),
                new ExifAttributeParser<ExifIfd0Directory>(CameraMaker, ExifIfd0Directory.TagMake, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>(ExposureTime, ExifIfd0Directory.TagExposureTime, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>(FStop, ExifIfd0Directory.TagFNumber, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>(ExposureBias, ExifIfd0Directory.TagExposureBias, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>(FocalLength, ExifIfd0Directory.TagFocalLength, AttributeType.String),
                new ExifAttributeParser<ExifSubIfdDirectory>(MaxAperture, ExifIfd0Directory.TagMaxAperture, AttributeType.String),
            };
        }

        public IEnumerable<Attribute> Deserialize(IReadOnlyList<JpegSegment> segments)
        {
            var exifReader = new ExifReader();
            var exifSegment = segments.FirstOrDefault(segment => 
                segment.MatchSegment(JpegSegmentType.App1, ExifHeader));
            if (exifSegment == null)
            {
                yield break;
            }

            var directories = exifReader.Extract(new ByteArrayReader(exifSegment.Bytes, ExifHeader.Length));
            var exif = new ExifMetadata(exifSegment, directories);
            foreach (var parser in _tags)
            {
                var attribute = parser.Parse(exif);
                if (attribute != null)
                {
                    yield return attribute;
                }
            }
        }

        public List<JpegSegment> Serialize(IReadOnlyList<JpegSegment> segments, IEnumerable<Attribute> attributes)
        {
            // Exif attributes are read-only
            return new List<JpegSegment>(segments);
        }
    }
}
