using System;
using System.Collections.Generic;
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
        Image
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

    /// <inheritdoc />
    /// <summary>
    /// Base class of all value types used in a query
    /// </summary>
    public abstract class BaseValue : IEquatable<BaseValue>
    {
        /// <summary>
        /// Type ID of this value
        /// </summary>
        public abstract TypeId Type { get; }

        public abstract bool Equals(BaseValue other);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((BaseValue)obj);
        }

        public abstract override int GetHashCode();

        public abstract override string ToString();

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

    public sealed class IntValue : BaseValue
    {
        public int? Value { get; }

        public override TypeId Type => TypeId.Integer;

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
            if (other.GetType() != GetType())
                return false;
            return Value == ((IntValue)other).Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => Value.ToString();

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IValueVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public sealed class RealValue : BaseValue
    {
        public double? Value { get; }

        public override TypeId Type => TypeId.Real;

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
            if (other.GetType() != GetType())
                return false;
            return Value == ((RealValue)other).Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => Value.ToString();

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IValueVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public sealed class StringValue : BaseValue
    {
        public string Value { get; }

        public override TypeId Type => TypeId.String;

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
            if (other.GetType() != GetType())
                return false;
            return Value == ((StringValue)other).Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => Value;

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IValueVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public sealed class DateTimeValue : BaseValue
    {
        /// <summary>
        /// Format of a DateTime value in string
        /// </summary>
        public const string Format = "yyyy-MM-ddTHH:mm:ss.szzz";

        public DateTime? Value { get; }

        public override TypeId Type => TypeId.DateTime;

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
            if (other.GetType() != GetType())
                return false;
            return Value == ((DateTimeValue)other).Value;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString() => Value.ToString();

        public override void Accept(IValueVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override T Accept<T>(IValueVisitor<T> visitor)
        {
            return visitor.Visit(this);
        }
    }

    public sealed class ImageValue : BaseValue
    {
        public byte[] Value { get; }

        public override TypeId Type => TypeId.Image;

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
            return Value.GetHashCode();
        }

        public override string ToString() => Value.ToString();

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