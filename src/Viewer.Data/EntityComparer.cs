using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StringComparer = System.StringComparer;

namespace Viewer.Data
{
    /// <summary>
    /// This class selects a value (<see cref="BaseValue"/>) from an <see cref="IEntity"/> which
    /// is then used for comparing 2 entities.
    /// </summary>
    public class SortParameter
    {
        /// <summary>
        /// Value getter. Given an entity, compute the value based on which we will sort the values.
        /// It must not return null but <see cref="BaseValue.IsNull"/> of the returned value can be true.
        /// </summary>
        public Func<IEntity, BaseValue> Getter { get; set; }

        private int _direction = 1;

        /// <summary>
        /// Sort direction. It can be 1 or -1. Default value is 1. It throws
        /// <see cref="ArgumentOutOfRangeException"/> is you set it to anything else.
        /// </summary>
        public int Direction
        {
            get => _direction;
            set
            {
                if (value != 1 && value != -1)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _direction = value;
            }
        } 
    }

    /// <inheritdoc />
    /// <summary>
    /// Compare entities based on list of <see cref="SortParameter"/>s.
    /// </summary>
    public class EntityComparer : IComparer<IEntity>
    {
        private readonly List<SortParameter> _parameters;

        public static EntityComparer Default { get; } = new EntityComparer();

        /// <summary>
        /// Entities will be compared based on their paths.
        /// </summary>
        public EntityComparer()
        {
            _parameters = new List<SortParameter>();
        }

        /// <summary>
        /// Entities will be compared based on values returned by <paramref name="parameters"/>.
        /// </summary>
        /// <param name="parameters"></param>
        public EntityComparer(List<SortParameter> parameters)
        {
            _parameters = parameters;
        }
        
        /// <summary>
        /// Combine 2 entity comparers so that values will be sorted by <paramref name="first"/>
        /// fist, and <paramref name="second"/> second.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        public EntityComparer(EntityComparer first, EntityComparer second)
        {
            _parameters = new List<SortParameter>(first._parameters);
            _parameters.AddRange(second._parameters);
        }

        /// <inheritdoc />
        /// <summary>
        /// Compare <see cref="IEntity"/> types. <see cref="DirectoryEntity"/> will be sorted first.
        /// If both <paramref name="x"/> and <paramref name="y"/> are not <see cref="DirectoryEntity"/>,
        /// <see cref="ValueComparer"/> will be used on values returned by <see cref="SortParameter"/>s
        /// passed in constructor. If all value comparisons return 0, paths will be compared.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(IEntity x, IEntity y)
        {
            if (x == y)
            {
                return 0;
            }
            else if (x is DirectoryEntity && y is DirectoryEntity)
            {
                return Comparer<string>.Default.Compare(x.Path, y.Path);
            }
            else if (x is DirectoryEntity || y == null)
            {
                return -1;
            }
            else if (y is DirectoryEntity || x == null)
            {
                return 1;
            }

            foreach (var parameter in _parameters)
            {
                var valueA = parameter.Getter(x);
                var valueB = parameter.Getter(y);
                var result = ValueComparer.Default.Compare(valueA, valueB) * parameter.Direction;
                if (result != 0)
                    return result;
            }

            return Comparer<string>.Default.Compare(x?.Path, y?.Path);
        }
    }

    public class EntityPathEqualityComparer : IEqualityComparer<IEntity>
    {
        public static EntityPathEqualityComparer Default { get; } = new EntityPathEqualityComparer();

        public bool Equals(IEntity x, IEntity y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                return false;
            return StringComparer.CurrentCultureIgnoreCase.Equals(x.Path, y.Path);
        }

        public int GetHashCode(IEntity obj)
        {
            return StringComparer.CurrentCultureIgnoreCase.GetHashCode(obj.Path);
        }
    }
}
