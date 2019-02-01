using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Jpeg;
using Viewer.Data.SQLite;
using XmpCore;
using XmpCore.Options;

namespace Viewer.Data.Formats.Xmp
{
    /// <summary>
    /// This class can serialize user attributes. These attributes are then used to
    /// replce all user attributes in the XMP tree.
    /// </summary>
    public class XmpUserAttributeWriter : XmpBase
    {
        /// <summary>
        /// Create a new xmp user attribute writer which will write to <paramref name="data"/>
        /// </summary>
        /// <param name="data">Data to modify</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="data"/> is null.
        /// </exception>
        public XmpUserAttributeWriter(IXmpMeta data) : base(data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
        }

        private class FormattingVisitor : IValueVisitor<string>
        {
            public string Visit(IntValue value)
            {
                return value.Value?.ToString(CultureInfo.InvariantCulture);
            }

            public string Visit(RealValue value)
            {
                return value.Value?.ToString(CultureInfo.InvariantCulture);
            }

            public string Visit(StringValue value)
            {
                return value.Value;
            }

            public string Visit(DateTimeValue value)
            {
                return value.Value?.ToString(DateTimeValue.Format);
            }

            public string Visit(ImageValue value)
            {
                // skip, storing images is not supported at the moment
                return null;
            }
        }

        /// <summary>
        /// Replace user attributes in XMP with <paramref name="attributes"/>
        /// </summary>
        /// <param name="attributes">Attributes to add to XMP</param>
        public void Write(IEnumerable<Attribute> attributes)
        {
            // remove old attributes
            var rootPath = $"{Prefix}:attributes";
            Data.DeleteProperty(Namespace, rootPath);

            // fill the array with attributes
            int index = 0;
            foreach (var attribute in attributes)
            {
                if (attribute.Source != AttributeSource.Custom ||
                    attribute.Value.IsNull)
                {
                    continue; // only store non-null user attributes
                }
                
                ++index; // according to XMP spec, the first index is 1

                Data.AppendArrayItem(Namespace, 
                    "attributes", 
                    new PropertyOptions
                    {
                        IsArray = true,
                        IsArrayOrdered = false
                    },
                    "", 
                    new PropertyOptions
                    {
                        IsStruct = true
                    });

                // set item properties
                Data.SetProperty(Namespace, GetAttributePropertyPath(index, "name"), attribute.Name);
                Data.SetProperty(Namespace, GetAttributePropertyPath(index, "type"), (int)attribute.Value.Type);

                // convert value to a string
                var converted = attribute.Value.Accept(new FormattingVisitor());
                if (converted == null)
                {
                    Data.DeleteArrayItem(Namespace, rootPath, index);
                    --index;
                    continue;
                }

                Data.SetProperty(Namespace, GetAttributePropertyPath(index, "value"), converted);
            }
        }
    }
}
