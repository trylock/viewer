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
        /// Custom attribute set by user.
        /// </summary>
        Custom = 0,

        /// <summary>
        /// Attribute which describes a feature of the file of entity (e.g. thumbnail, image size etc.)
        /// </summary>
        Metadata = 1,
    }

    public class Attribute : IEquatable<Attribute>, IComparable<Attribute>
    {
        /// <summary>
        /// Name of the attribute
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Determines location where the attribute is stored
        /// </summary>
        public AttributeSource Source { get; }

        /// <summary>
        /// Value of the attribute
        /// </summary>
        public BaseValue Value { get; }

        public Attribute(string name, BaseValue value, AttributeSource source)
        {
            Name = name;
            Value = value;
            Source = source;
        }

        /// <summary>
        /// Check whether 2 attribute have the same type and represent the same value.
        /// </summary>
        /// <param name="other">Other attribute</param>
        /// <returns>true iff the attributes have the same type and represent the same value</returns>
        public bool Equals(Attribute other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (other.GetType() != GetType())
                return false;
            if (Name != other.Name || Source != other.Source)
                return false;
            return Value.Equals(other.Value);
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
            var hashCode = -1038278788;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Source.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<BaseValue>.Default.GetHashCode(Value);
            return hashCode;
        }

        public override string ToString()
        {
            return "Attribute(\"" + Name + "\", " + Value + ")";
        }

        public int CompareTo(Attribute other)
        {
            if (ReferenceEquals(this, other))
                return 0;
            if (ReferenceEquals(null, other))
                return 1;
            var nameComparison = string.Compare(Name, other.Name, StringComparison.CurrentCulture);
            if (nameComparison != 0)
                return nameComparison;
            return Comparer<BaseValue>.Default.Compare(Value, other.Value);
        }
    }
}
