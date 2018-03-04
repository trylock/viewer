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
        
        public abstract void Accept(IAttributeVisitor visitor);
        
        public virtual void Dispose()
        {
        }
    }

    public class IntAttribute : Attribute
    {
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
    }

    public sealed class DoubleAttribute : Attribute
    {
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
    }

    public sealed class StringAttribute : Attribute
    {
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
    }

    public sealed class DateTimeAttribute : Attribute
    {
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
    }

    public sealed class ImageAttribute : Attribute
    {
        public Image Value { get; private set; }

        public ImageAttribute(string name, AttributeSource source, Image value) : base(name, source)
        {
            Value = value;
        }
        
        public override void Accept(IAttributeVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override void Dispose()
        {
            if (Value != null)
            {
                Value.Dispose();
                Value = null;
            }
        }
    }
}
