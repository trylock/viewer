using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public enum AttributeSource
    {
        /// <summary>
        /// Attribute is a custom attribute set by user
        /// </summary>
        Custom = 0,

        /// <summary>
        /// Attribute is a read-only attribute stored in the Exif metadata
        /// </summary>
        Exif,
    }

    public abstract class Attribute : IDisposable
    {
        /// <summary>
        /// Name of the attribute
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Determines location where the attribute is stored
        /// </summary>
        public AttributeSource Source { get; }

        protected Attribute(string name, AttributeSource source)    
        {
            Name = name;
            Source = source;
        }
        
        /// <summary>
        /// Accept visitor without a return value
        /// </summary>
        /// <param name="visitor">Visitor</param>
        public abstract void Accept(IAttributeVisitor visitor);
        
        /// <summary>
        /// Accept visitor with a return value
        /// </summary>
        /// <typeparam name="T">Return value of the visitor</typeparam>
        /// <param name="visitor">visitor</param>
        /// <returns>Value returned by the visitor</returns>
        public abstract T Accept<T>(IAttributeVisitor<T> visitor);

        public virtual void Dispose()
        {
        }

        protected string FormatAttribute<T>(T value, string typeName)
        {
            return typeName + "(\"" + Name + "\", " + value + ")";
        }
    }

    public class IntAttribute : Attribute
    {
        /// <summary>
        /// Name of this type
        /// </summary>
        public const string TypeName = "Int32";

        /// <summary>
        /// Value of the attribute
        /// </summary>
        public int Value { get; }

        public IntAttribute(string name, AttributeSource source, int value) : base(name, source)
        {
            Value = value;
        }

        public override void Accept(IAttributeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IAttributeVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return FormatAttribute(Value, TypeName);
        }
    }

    public sealed class DoubleAttribute : Attribute
    {
        /// <summary>
        /// Name of this type
        /// </summary>
        public const string TypeName = "Double";

        /// <summary>
        /// Value of the attribute
        /// </summary>
        public double Value { get; }

        public DoubleAttribute(string name, AttributeSource source, double value) : base(name, source)
        {
            Value = value;
        }

        public override void Accept(IAttributeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IAttributeVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return FormatAttribute(Value, TypeName);
        }
    }

    public sealed class StringAttribute : Attribute
    {
        /// <summary>
        /// Name of this type
        /// </summary>
        public const string TypeName = "String";

        /// <summary>
        /// Value of the attribute
        /// </summary>
        public string Value { get; }

        public StringAttribute(string name, AttributeSource source, string value) : base(name, source)
        {
            Value = value;
        }
        
        public override void Accept(IAttributeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IAttributeVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return FormatAttribute(Value, TypeName);
        }
    }

    public sealed class DateTimeAttribute : Attribute
    {
        /// <summary>
        /// Name of this type
        /// </summary>
        public const string TypeName = "DateTime";

        /// <summary>
        /// Format of a DateTime value in string
        /// </summary>
        public const string Format = "yyyy-MM-ddTHH:mm:ss.szzz";

        /// <summary>
        /// Value of the attribute
        /// </summary>
        public DateTime Value { get; }

        public DateTimeAttribute(string name, AttributeSource source, DateTime value) : base(name, source)
        {
            Value = value;
        }

        public override void Accept(IAttributeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IAttributeVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override string ToString()
        {
            return FormatAttribute(Value.ToString(Format), TypeName);
        }
    }

    public sealed class ImageAttribute : Attribute
    {
        /// <summary>
        /// Name of this type
        /// </summary>
        public const string TypeName = "Image";

        public Image Value { get; private set; }

        public ImageAttribute(string name, AttributeSource source, Image value) : base(name, source)
        {
            Value = value;
        }
        
        public override void Accept(IAttributeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IAttributeVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }

        public override void Dispose()
        {
            if (Value != null)
            {
                Value.Dispose();
                Value = null;
            }
        }

        public override string ToString()
        {
            return FormatAttribute(Value.Size, TypeName);
        }
    }
}
