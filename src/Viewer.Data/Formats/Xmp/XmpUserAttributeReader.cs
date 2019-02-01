using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using NLog;
using Viewer.Data.Formats.Attributes;
using Viewer.Data.Formats.Jpeg;
using XmpCore;

namespace Viewer.Data.Formats.Xmp
{
    /// <summary>
    /// Read all user attributes from XMP metadata.
    /// </summary>
    public class XmpUserAttributeReader : XmpBase, IEnumerable<Attribute>
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        public XmpUserAttributeReader(IXmpMeta data) : base(data)
        {
        }

        public IEnumerator<Attribute> GetEnumerator()
        {
            if (Data == null)
            {
                yield break;
            }

            var attributeCount = Data.CountArrayItems(Namespace, "attributes");
            for (var i = 0; i < attributeCount; ++i)
            {
                // parse attribute name and type
                var name = GetAttributeProperty(i + 1, "name");
                var type = GetAttributeProperty(i + 1, "type");
                if (name == null || 
                    type == null || 
                    !int.TryParse(type.Value, out var typeValue))
                {
                    Logger.Warn("Missing name of type.");
                    continue;
                }

                // parse attribute value
                var valueProperty = GetAttributeProperty(i + 1, "value");
                BaseValue value = null;
                switch ((AttributeType)typeValue)
                {
                    case AttributeType.Int:
                        if (int.TryParse(valueProperty.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var intResult))
                        {
                            value = new IntValue(intResult);
                        }
                        else
                        {
                            Logger.Warn("Invalid int value {0}", valueProperty.Value);
                        }
                        break;
                    case AttributeType.Double:
                        if (double.TryParse(valueProperty.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleResult))
                        {
                            value = new RealValue(doubleResult);
                        }
                        else
                        {
                            Logger.Warn("Invalid double value {0}", valueProperty.Value);
                        }
                        break;
                    case AttributeType.String:
                        value = new StringValue(valueProperty.Value);
                        break;
                    case AttributeType.DateTime:
                        if (DateTime.TryParseExact(
                            valueProperty.Value,
                            DateTimeValue.Format,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.AllowWhiteSpaces, out var dateResult))
                        {
                            value = new DateTimeValue(dateResult);
                        }
                        else
                        {
                            Logger.Warn("Invalid date time value {0}", valueProperty.Value);
                        }
                        break;
                    default:
                        continue;
                }

                if (value == null || value.IsNull)
                {
                    continue;
                }

                yield return new Attribute(name.Value, value, AttributeSource.Custom);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}