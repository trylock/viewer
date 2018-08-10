using System;

namespace Viewer.Core
{
    public struct Fraction : IEquatable<Fraction>
    {
        /// <summary>
        /// Numerator of the fraction
        /// </summary>
        public int Numerator { get;  }

        /// <summary>
        /// Denominator of the fraction
        /// </summary>
        public int Denominator { get; }

        public Fraction(int numerator, int denominator)
        {
            var gcd = GreatestCommonDivisor(numerator, denominator);
            Numerator = numerator / gcd;
            Denominator = denominator / gcd;
        }

        public static explicit operator double(Fraction value)
        {
            return value.Numerator / (double) value.Denominator;
        }

        public static explicit operator float(Fraction value)
        {
            return value.Numerator / (float)value.Denominator;
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            for (;;)
            {
                if (b == 0)
                    return a;

                var a1 = a;
                a = b;
                b = a1 % b;
            }
        }

        public static bool operator ==(Fraction lhs, Fraction rhs)
        {
            return lhs.Numerator == rhs.Numerator &&
                   lhs.Denominator == rhs.Denominator;
        }

        public static bool operator !=(Fraction lhs, Fraction rhs)
        {
            return !(lhs == rhs);
        }

        public bool Equals(Fraction other)
        {
            return this == other;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Fraction fraction && Equals(fraction);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Numerator * 397) ^ Denominator;
            }
        }
    }
}
