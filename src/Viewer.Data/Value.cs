using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public enum TypeId
    {
        None = 0,
        Integer,
        Real,
        String,
        DateTime,
        Image,
        Count
    }

    public interface IValueVisitor
    {
        void Visit(IntValue value);
        void Visit(RealValue value);
        void Visit(StringValue value);
        void Visit(DateTimeValue value);
        void Visit(ImageValue value);
    }

    public interface IValueVisitor<out T>
    {
        T Visit(IntValue value);
        T Visit(RealValue value);
        T Visit(StringValue value);
        T Visit(DateTimeValue value);
        T Visit(ImageValue value);
    }
    
    /// <summary>
    /// Base class of all value types used in a query
    /// </summary>
    public abstract class BaseValue : IEquatable<BaseValue>, IComparable<BaseValue>
    {
        /// <summary>
        /// Type ID of this value
        /// </summary>
        public abstract TypeId Type { get; }

        /// <summary>
        /// true iff this value is null
        /// </summary>
        public abstract bool IsNull { get; }

        public abstract bool Equals(BaseValue other);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            return Equals((BaseValue)obj);
        }

        public abstract override int GetHashCode();

        /// <summary>
        /// Convert this value to a string which can be used in a query for example.
        /// </summary>
        /// <returns></returns>
        public abstract override string ToString();

        /// <summary>
        /// Convert this value to a string using a specific <paramref name="culture"/>
        /// </summary>
        /// <param name="culture">Culture used to format this value</param>
        /// <returns>This value formatted as a string</returns>
        public abstract string ToString(CultureInfo culture);

        public virtual int CompareTo(BaseValue other)
        {
            return ValueComparer.Default.Compare(this, other);
        }

        /// <summary>
        /// Accept a visitor
        /// </summary>
        /// <param name="visitor"></param>
        public abstract void Accept(IValueVisitor visitor);

        /// <summary>
        /// Accept a visitor with a return value
        /// </summary>
        /// <typeparam name="T">Type of the return value</typeparam>
        /// <param name="visitor"></param>
        public abstract T Accept<T>(IValueVisitor<T> visitor);
    }

    public class IntValue : BaseValue
    {
        public int? Value { get; }

        public override TypeId Type => TypeId.Integer;

        public override bool IsNull => Value == null;

        public IntValue(int? value)
        {
            Value = value;
        }

        public override bool Equals(BaseValue other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!(other is IntValue converted))
                return false;
            return Value == converted.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => Value?.ToString() ?? "null";
        public override string ToString(CultureInfo culture) => Value?.ToString(culture) ?? "null";

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IValueVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public class RealValue : BaseValue
    {
        public double? Value { get; }

        public override TypeId Type => TypeId.Real;

        public override bool IsNull => Value == null;

        public RealValue(double? value)
        {
            Value = value;
        }

        public override bool Equals(BaseValue other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!(other is RealValue converted))
                return false;
            return Value == converted.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => Value?.ToString(CultureInfo.InvariantCulture) ?? "null";
        public override string ToString(CultureInfo culture) => Value?.ToString(culture) ?? "null";

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IValueVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public class StringValue : BaseValue
    {
        public string Value { get; }

        public override TypeId Type => TypeId.String;

        public override bool IsNull => Value == null;

        public StringValue(string value)
        {
            Value = value;
        }

        public override bool Equals(BaseValue other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!(other is StringValue converted))
                return false;
            return Value == converted.Value;
        }

        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }

        public override string ToString() => Value == null ? "null" : "\"" + Value + "\"";
        public override string ToString(CultureInfo culture) => ToString();

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IValueVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public class DateTimeValue : BaseValue
    {
        /// <summary>
        /// Format of a DateTime value in string
        /// </summary>
        public const string Format = "yyyy-MM-ddTHH:mm:ss.szzz";

        /// <summary>
        /// Format of a DateTime value in a query
        /// </summary>
        public const string QueryFormat = "yyyy-MM-dd HH:mm:ss";

        public DateTime? Value { get; }

        public override TypeId Type => TypeId.DateTime;

        public override bool IsNull => Value == null;

        public DateTimeValue(DateTime? value)
        {
            Value = value;
        }

        public override bool Equals(BaseValue other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (!(other is DateTimeValue converted))
                return false;
            return Value == converted.Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() =>
            Value == null ? "null" : "\"" + ((DateTime)Value).ToString(QueryFormat) + "\"";

        public override string ToString(CultureInfo culture) =>
            Value == null ? "null" : "\"" + ((DateTime)Value).ToString(culture) + "\"";

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IValueVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public class ImageValue : BaseValue
    {
        public byte[] Value { get; }

        public override TypeId Type => TypeId.Image;

        public override bool IsNull => Value == null;

        public ImageValue(byte[] value)
        {
            Value = value;
        }

        public override bool Equals(BaseValue other)
        {
            return ReferenceEquals(this, other);
        }

        public override int GetHashCode()
        {
            return Value?.GetHashCode() ?? 0;
        }

        public override string ToString() => Value?.ToString() ?? "null";
        public override string ToString(CultureInfo culture) => ToString();

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IValueVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }
}