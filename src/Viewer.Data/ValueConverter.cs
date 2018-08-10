using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Viewer.Data
{
    public interface IValueConverter
    {
        /// <summary>
        /// Compute cost of convertion from <paramref name="from"/> to <paramref name="to"/>.
        /// The cost will be int.MaxValue if the conversion is not possible
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>Cost of the conversion</returns>
        int ComputeConversionCost(TypeId from, TypeId to);

        /// <summary>
        /// Compute cost of conversion from types <paramref name="from"/> to types <paramref name="to"/>
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        int ComputeConversionCost(IReadOnlyList<TypeId> from, IReadOnlyList<TypeId> to);

        /// <summary>
        /// Convert <paramref name="value"/> to type <paramref name="to"/>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="to"></param>
        /// <returns>Converted value. It won't ever be null but its value may be null</returns>
        BaseValue ConvertTo(BaseValue value, TypeId to);
    }

    [Export(typeof(IValueConverter))]
    public class ValueConvertor : IValueConverter
    {
        private class ConversionVisitor : IValueVisitor<BaseValue>
        {
            private readonly TypeId _resultType;

            public ConversionVisitor(TypeId resultType)
            {
                if (resultType == TypeId.None)
                    throw new ArgumentOutOfRangeException(nameof(resultType));
                _resultType = resultType;
            }

            public BaseValue Visit(IntValue value)
            {
                switch (_resultType)
                {
                    case TypeId.Integer:
                        return value;
                    case TypeId.Real:
                        return new RealValue(value.Value);
                    case TypeId.String:
                        return new StringValue(value?.Value == null ? null : value.Value.ToString());
                    case TypeId.DateTime:
                        return new DateTimeValue(null);
                    case TypeId.Image:
                        return new ImageValue(null);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public BaseValue Visit(RealValue value)
            {
                switch (_resultType)
                {
                    case TypeId.Integer:
                        return new IntValue(null);
                    case TypeId.Real:
                        return value;
                    case TypeId.String:
                        return new StringValue(value?.Value == null ? null : value.Value.ToString());
                    case TypeId.DateTime:
                        return new DateTimeValue(null);
                    case TypeId.Image:
                        return new ImageValue(null);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public BaseValue Visit(StringValue value)
            {
                switch (_resultType)
                {
                    case TypeId.Integer:
                        return new IntValue(null);
                    case TypeId.Real:
                        return new RealValue(null);
                    case TypeId.String:
                        return value;
                    case TypeId.DateTime:
                        if (DateTime.TryParse(value.Value, out DateTime dateValue))
                        {
                            return new DateTimeValue(dateValue);
                        }
                        return new DateTimeValue(null);
                    case TypeId.Image:
                        return new ImageValue(null);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public BaseValue Visit(DateTimeValue value)
            {
                switch (_resultType)
                {
                    case TypeId.Integer:
                        return new IntValue(null);
                    case TypeId.Real:
                        return new RealValue(null);
                    case TypeId.String:
                        return new StringValue(value?.Value == null ? null : value.Value.ToString());
                    case TypeId.DateTime:
                        return value;
                    case TypeId.Image:
                        return new ImageValue(null);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public BaseValue Visit(ImageValue value)
            {
                switch (_resultType)
                {
                    case TypeId.Integer:
                        return new IntValue(null);
                    case TypeId.Real:
                        return new RealValue(null);
                    case TypeId.String:
                        return new StringValue(null);
                    case TypeId.DateTime:
                        return new DateTimeValue(null);
                    case TypeId.Image:
                        return value;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        public int ComputeConversionCost(TypeId from, TypeId to)
        {
            if (from == to)
            {
                return 0;
            }

            switch (from)
            {
                case TypeId.Integer:
                    if (to == TypeId.Real)
                    {
                        return 1;
                    }
                    else if (to == TypeId.String)
                    {
                        return 2;
                    }

                    break;
                case TypeId.Real:
                    if (to == TypeId.String)
                    {
                        return 1;
                    }
                    break;
                case TypeId.String:
                    if (to == TypeId.DateTime)
                    {
                        return 1;
                    }
                    break;
                case TypeId.DateTime:
                    if (to == TypeId.String)
                    {
                        return 1;
                    }
                    break;
            }
            return int.MaxValue;
        }

        public int ComputeConversionCost(IReadOnlyList<TypeId> from, IReadOnlyList<TypeId> to)
        {
            if (from.Count != to.Count)
            {
                return int.MaxValue;
            }

            var totalCost = 0;
            for (var i = 0; i < from.Count; ++i)
            {
                var cost = ComputeConversionCost(from[i], to[i]);
                if (cost == int.MaxValue)
                {
                    return int.MaxValue;
                }

                totalCost += cost;
            }

            return totalCost;
        }

        public BaseValue ConvertTo(BaseValue value, TypeId to)
        {
            var convertor = new ConversionVisitor(to);
            return value.Accept(convertor);
        }
    }
}
