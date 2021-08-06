using BigMath;

using System;
using System.Globalization;

using I = GeometryLib.Int64.D2;


using N = GeometryLib.Numbers;



namespace GeometryLib.Int64.Fraction.D2
{
    public readonly struct Vector : IEquatable<Vector>, IEquatable<I.Vector>, IIntegerPoint
    {
        internal readonly Int128 NumX, NumY, Den;

        public long x { get; }
        public long y { get; }

        private static decimal getFrac(in Int128 num, in Int128 den)
        {
            var rem = num; 
            var frac = 0m;
            var div = 0.1m;
            while(rem != Int128.Zero && div != 0m)
            {
                var f = Int128.DivRem(rem * 10, den, out rem);
                frac += (int)f * div;
                div /= 10;
            }
            return frac;
        }

        public decimal DecimalX => x + getFrac(NumX, Den);

        public decimal DecimalY => y + getFrac(NumY, Den);

        public Decimal.D2.Vector ToDecimal() => new Decimal.D2.Vector(DecimalX, DecimalY);

        #region Constructors

        //public Vector(I.Vector p)
        //{
        //    x = p.x;
        //    y = p.y;
        //    NumX = 0;
        //    NumY = 0;
        //    Den = 1;
        //}

        private Vector(long x, long y, Int128 numX, Int128 numY, Int128 den)
        {
            this.x = x;
            this.y = y;
            this.NumX = numX;
            this.NumY = numY;
            this.Den = den;
        }

        public static IIntegerPoint Create(in I.Vector start, in I.Vector dest, in N.Fraction128 pos)
        {
            long dx = dest.x - start.x, sx;
            long dy = dest.y - start.y, sy;
            (sx, dx) = dx < 0 ? (-1, -dx) : (1, dx);
            (sy, dy) = dy < 0 ? (-1, -dy) : (1, dy);

            // calculate nearest integer positions and remainders
            var n256 = (Int256)pos.Num;
            var d256 = (Int256)pos.Den;
            // use Int256 to avoid overflow
            var dxn = (Int256)dx * n256;
            var dyn = (Int256)dy * n256;

            dx = (long)Int256.DivRem(dxn, d256, out var remX);
            dy = (long)Int256.DivRem(dyn, d256, out var remY);


            // adjust if negative because Fraction is always positive
            if (sx < 0 && remX != 0)
            {
                dx++;
                remX = d256 - remX;
            }

            if (sy < 0 && remY != 0)
            {
                dy++;
                remY = d256 - remY;
            }

            long x = start.x + sx * dx;
            long y = start.y + sy * dy;

            if (remX == 0 && remY == 0)
            {
                return new I.Vector(x, y);
            }

            var remX128 = (Int128)remX;
            var remY128 = (Int128)remY;

            // reduce Fractions if possible, to make equality unique
            var r = N.Fraction128.Gcd(remX128, N.Fraction128.Gcd(remY128, pos.Den));

            return new Vector(x, y, remX128 / r, remY128 / r, pos.Den / r);
        }

        #endregion

        public long RoundX => x + (Den - NumX > NumX ? 0 : 1);

        public long RoundY => y + (Den - NumY > NumY ? 0 : 1);

        public I.Vector Ceiling => new(x + (NumX == 0 ? 0 : 1), y + (NumY == 0 ? 0 : 1));


        public (long x, long y) xy => (x, y);

        public bool Equals(Vector other)
        {
            return x == other.x && y == other.y && NumX == other.NumX && NumY == other.NumY && Den == other.Den;
        }

        public bool Equals(I.Vector other) => false;

        public bool Equals(IIntegerPoint? other)
        {
            return other is Vector fvec ? Equals(fvec) : (other is I.Vector ivec && Equals(ivec));
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector fvec ? Equals(fvec) : (obj is I.Vector ivec && Equals(ivec));
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return x.GetHashCode() ^ y.GetHashCode() ^ NumX.GetHashCode() ^ NumY.GetHashCode() ^ Den.GetHashCode();
#else
            return HashCode.Combine(x, y, NumX, NumY, Den);
#endif
        }

        public void Deconstruct(out long outX, out long outY)
        {
            outX = x;
            outY = y;
        }

        public override string ToString()
        {
            return ToString(" ");//$"{x} {y}+({NumX} {NumY})/{Den}";
        }

        public string ToString(string separator)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:G}{2}{1:G}", DecimalX, DecimalY, separator);
        }

        public string ToWktString()
        {
            return $"POINT({ToString()})";
        }

        public static bool operator ==(in Vector left, in Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in Vector left, in Vector right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(in I.Vector left, in Vector right)
        {
            return right.Equals(left);
        }

        public static bool operator !=(in I.Vector left, in Vector right)
        {
            return !right.Equals(left);
        }

        public static bool operator ==(in Vector left, in I.Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in Vector left, in I.Vector right)
        {
            return !left.Equals(right);
        }
    }
}

//private static (BigInteger x, BigInteger y) ToBig(in Vector v, in BigInteger den, in ulong den1, in ulong den2)
//{
//    var fac = BigInteger.Multiply(den1, den2);
//    var x = v.x * den + v.NumX * fac;
//    var y = v.y * den + v.NumY * fac;
//    return (x, y);
//}

//private static BigInteger Det(BigInteger ax, BigInteger ay, BigInteger bx, BigInteger by)
//{
//    return (ax * by) - (ay * bx);
//}

//private static BigInteger Dot(BigInteger ax, BigInteger ay, BigInteger bx, BigInteger by)
//{
//    return (ax * bx) + (ay * by);
//}

//public bool IsBetween(in Vector a, in Vector b)
//{
//    var den = BigInteger.Multiply(Den, a.Den) * b.Den;
//    var pv = ToBig(this, den, a.Den, b.Den);
//    var av = ToBig(a, den, Den, b.Den);
//    var bv = ToBig(b, den, a.Den, Den);

//    var abx = bv.x - av.x;
//    var aby = bv.y - av.y;
//    var apx = pv.x - av.x;
//    var apy = pv.y - av.y;

//    if (Det(abx, aby, apx, apy) != 0) 
//        return false;

//    var lnum = Dot(abx, aby, apx, apy);
//    var lden = Dot(abx, aby, abx, aby);

//    return lnum > 0 && lnum < lden;
//}


//public bool IsBetween(in IIntegerPoint a, in IIntegerPoint b)
//{
//    switch ((a,b))
//    {
//        case (I.Vector av, I.Vector bv):
//            if (IsInteger)
//            {
//                return new I.Vector(x,y).IsBetween(av,bv);
//            }
//            return IsBetween(new Vector(av), new Vector(bv));
//        case (I.Vector av, Vector bv):
//            if (IsInteger && bv.IsInteger)
//            {
//                return new I.Vector(x, y).IsBetween(av, new I.Vector(bv.x,bv.y));
//            }
//            return IsBetween(new Vector(av), bv);
//        case (Vector av, I.Vector bv):
//            if (IsInteger && av.IsInteger)
//            {
//                return new I.Vector(x, y).IsBetween(new I.Vector(av.x, av.y), bv);
//            }
//            return IsBetween(av, new Vector(bv));
//        case (Vector av, Vector bv):
//            if (IsInteger && av.IsInteger && bv.IsInteger)
//            {
//                return new I.Vector(x, y).IsBetween(new I.Vector(av.x, av.y), new I.Vector(bv.x, bv.y));
//            }
//            return IsBetween(av, bv);
//        default:
//            throw new NotImplementedException();
//    }
//}

//public int EdgeSign(in Vector a, in Vector b)
//{
//    var den = BigInteger.Multiply(Den, a.Den) * b.Den;
//    var pv = ToBig(this, den, a.Den, b.Den);
//    var av = ToBig(a, den, Den, b.Den);
//    var bv = ToBig(b, den, a.Den, Den);

//    return ((av.x - pv.x) * (bv.y - pv.y) - (av.y - pv.y) * (bv.x - pv.x)).Sign;
//}

//public int EdgeSign(in IIntegerPoint a, in IIntegerPoint b)
//{
//    switch ((a, b))
//    {
//        case (I.Vector av, I.Vector bv):
//            if (IsInteger)
//                return new I.Vector(x, y).EdgeSign(av, bv);
//            return EdgeSign(new Vector(av), new Vector(bv));
//        case (I.Vector av, Vector bv):
//            if (IsInteger && bv.IsInteger)
//                return new I.Vector(x, y).EdgeSign(av, new I.Vector(bv.x, bv.y));
//            return EdgeSign(new Vector(av), bv);
//        case (Vector av, I.Vector bv):
//            if (IsInteger && av.IsInteger)
//                return new I.Vector(x, y).EdgeSign(new I.Vector(av.x, av.y), bv);
//            return EdgeSign(av, new Vector(bv));
//        case (Vector av, Vector bv):
//            if (IsInteger && av.IsInteger && bv.IsInteger)
//                return new I.Vector(x, y).EdgeSign(new I.Vector(av.x, av.y), new I.Vector(bv.x, bv.y));
//            return EdgeSign(av, bv);
//        default:
//            throw new NotImplementedException();
//    }
//}

//public bool InCircle(in IIntegerPoint a, in IIntegerPoint b, in IIntegerPoint c)
//{
//    throw new NotImplementedException();
//}
