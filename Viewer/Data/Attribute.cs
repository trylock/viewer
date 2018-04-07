using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    [Flags]
    public enum AttributeFlags
    {
        None = 0x0,

        /// <summary>
        /// Attribute can't be changed
        /// </summary>
        ReadOnly = 0x1,
    }

    public abstract class Attribute : IDisposable, IEquatable<Attribute>
    {
        /// <summary>
        /// Name of the attribute
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Determines location where the attribute is stored
        /// </summary>
        public AttributeFlags Flags { get; }

        protected Attribute(string name, AttributeFlags flags)    
        {
            Name = name;
            Flags = flags;
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

        /// <summary>
        /// Check whether 2 attribute have the same type and represent the same value.
        /// </summary>
        /// <param name="other">Other attribute</param>
        /// <returns>true iff the attributes have the same type and represent the same value</returns>
        public virtual bool Equals(Attribute other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (other.GetType() != GetType())
                return false;
            if (Name != other.Name || Flags != other.Flags)
                return false;
            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((Attribute) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (int) Flags;
            }
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

        public IntAttribute(string name, int value, AttributeFlags flags = AttributeFlags.None) : base(name, flags)
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

        public override bool Equals(Attribute other)
        {
            if (!base.Equals(other))
                return false;
            return Value == ((IntAttribute) other).Value;
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

        public DoubleAttribute(string name, double value, AttributeFlags flags = AttributeFlags.None) : base(name, flags)
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

        public override bool Equals(Attribute other)
        {
            if (!base.Equals(other))
                return false;
            return Value == ((DoubleAttribute)other).Value;
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

        public StringAttribute(string name, string value, AttributeFlags flags = AttributeFlags.None) : base(name, flags)
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

        public override bool Equals(Attribute other)
        {
            if (!base.Equals(other))
                return false;
            return Value == ((StringAttribute)other).Value;
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

        public DateTimeAttribute(string name, DateTime value, AttributeFlags flags = AttributeFlags.None) : base(name, flags)
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

        public override bool Equals(Attribute other)
        {
            if (!base.Equals(other))
                return false;
            return Value == ((DateTimeAttribute)other).Value;
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

        public ImageAttribute(string name, Image value, AttributeFlags flags = AttributeFlags.None) : base(name, flags)
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

        public override bool Equals(Attribute other)
        {
            // image value is unique to the attribute
            return ReferenceEquals(this, other);
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
