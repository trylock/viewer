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

    public class Attribute : IEquatable<Attribute>
    {
        /// <summary>
        /// Name of the attribute
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Determines location where the attribute is stored
        /// </summary>
        public AttributeFlags Flags { get; }

        /// <summary>
        /// Value of the attribute
        /// </summary>
        public BaseValue Value { get; }

        public Attribute(string name, BaseValue value, AttributeFlags flags = AttributeFlags.None)
        {
            Name = name;
            Value = value;
            Flags = flags;
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
            if (Name != other.Name || Flags != other.Flags)
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
            hashCode = hashCode * -1521134295 + Flags.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<BaseValue>.Default.GetHashCode(Value);
            return hashCode;
        }

        public override string ToString()
        {
            return "(\"" + Name + "\", " + Value + ")";
        }
    }
}
