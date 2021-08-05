using BigMath;

using System;
using System.Collections.Generic;
using System.Numerics;

namespace GeometryLib.Numbers
{
    /// <summary> Positive Rational Number between 0 and 1 </summary>
    public readonly struct Fraction128 : IComparable<Fraction128>, IEquatable<Fraction128>
    {
        public readonly Int128 Num;

        public readonly Int128 Den;

        private const ulong MaxDbl = 1L << 53;

        public static readonly Fraction128 Zero = new Fraction128(0, 1);

        public static readonly Fraction128 One = new Fraction128(1, 1);

        public bool IsZero => Num == 0;

        public bool IsOne => Num == Den;

        public static Comparer<Fraction128> Desc => Comparer<Fraction128>.Create((a, b) => b.CompareTo(a));

        public Fraction128(Int128 numerator, Int128 denominator)
        {
            (Num, Den) = (denominator <= 0) || (numerator > denominator) ?
                throw new ArithmeticException("Invalid Fraction") :
                numerator == 0 ? (0, 1) :
                    numerator == denominator ? (1, 1) : Divide(numerator, denominator, Gcd(numerator, denominator));
        }


        internal static Int128 Gcd(in Int128 num, in Int128 den)
        {
            // Euclidean algorithm to find the gcd
            var q = den;
            var r = num;
            while (r > 0)
            {
                (q, r) = (r, q % r);
            }

            return q > 0 ? q : 1;
        }

        /// <summary> Rounds fraction, half up </summary>
        public int Round => (Den - Num) > Num ? 0 : 1;


        public void Deconstruct(out Int128 num, out Int128 den) { (num, den) = (Num, Den); }

        //       public void Deconstruct(out BigInteger num, out BigInteger den) => (num, den) = (Num, Den);

        private static (Int128, Int128) Divide(in Int128 num, in Int128 den, in Int128 div) => (num / div, den / div);

        public Fraction128 Reverse() => new Fraction128(Den - Num, Den);

        public int Rounded => (Den - Num) > Num ? 0 : 1;

        public (Int128 num, Int128 den) Tuple => (Num, Den);

        public (Int128 num, Int128 den) TupleReverse => (Den - Num, Den);

        private static int Comp(Int128 ad, Int128 an, Int128 bd, Int128 bn)
        {
            // Comparison as continued fraction
            int retVal = -1;
            do
            {
                var qa = an / ad;
                var qb = bn / bd;

                // compare if quotients different
                if (qa != qb)
                {
                    return qa < qb ? -retVal : retVal;
                }

                // change return sign
                retVal = -retVal;

                // new remainders
                (an, ad) = (ad, an % ad);
                (bn, bd) = (bd, bn % bd);
            } while ((ad != 0) && (bd != 0));

            return ad == bd ? 0 :
                ad == 0 ? retVal : -retVal; // NOTE: check correctness
        }

        public int CompareTo(Fraction128 other) =>
            Den == other.Den ? Num.CompareTo(other.Num) :
            Num == other.Num ? other.Den.CompareTo(Den) :
            IsZero ? other.IsZero ? 0 : -1 :
            IsOne ? other.IsOne ? 0 : 1 :
            other.IsZero ? 1 :
            other.IsOne ? -1 : Comp(Num, Den, other.Num, other.Den);

        public bool Equals(Fraction128 other) => (Den == other.Den) && (Num == other.Num);

        public override bool Equals(object? obj) => obj is Fraction128 other && Equals(other);

        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return Num.GetHashCode() ^ Den.GetHashCode();
#else
            return HashCode.Combine(Num, Den);
#endif
        }

        public static bool operator ==(Fraction128 left, Fraction128 right) => left.Equals(right);

        public static bool operator !=(Fraction128 left, Fraction128 right) => !left.Equals(right);

        public static bool operator <(Fraction128 left, Fraction128 right) => left.CompareTo(right) < 0;

        public static bool operator >(Fraction128 left, Fraction128 right) => left.CompareTo(right) > 0;

        public static bool operator <=(Fraction128 left, Fraction128 right) => left.CompareTo(right) <= 0;

        public static bool operator >=(Fraction128 left, Fraction128 right) => left.CompareTo(right) >= 0;

        public override string ToString() => $"{S(3)} ({Num}/{Den})";

        public string S(int decimals)
        {
            if (decimals < 1)
            {
                return (Num / Den).ToString();
            }
            string s = Num == Den ? "1." : "0.";
            var n = Num;
            for (int i = 0; i < decimals; i++)
            {
                n *= 10;
                var c = Int128.DivRem(n, Den, out n);
                s += c.ToString();
            }
            return s;
        }

    }
}
