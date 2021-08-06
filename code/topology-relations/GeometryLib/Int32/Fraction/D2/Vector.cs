using System;
using System.Globalization;

using I = GeometryLib.Int32.D2;

/* Unmerged change from project 'GeometryLib (netstandard2.1)'
Before:
using I = GeometryLib.Int32.D2;
After:
using N = GeometryLib.Numbers;
*/


using N = GeometryLib.Numbers;



namespace GeometryLib.Int32.Fraction.D2
{
    public readonly struct Vector : IEquatable<Vector>, IEquatable<I.Vector>, IIntegerPoint
    {
        internal readonly ulong NumX, NumY, Den;

        public int x { get; }
        public int y { get; }

        public decimal DecimalX => x + (decimal)NumX / Den;

        public decimal DecimalY => y + (decimal)NumY / Den;

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

        private Vector(int x, int y, ulong numX, ulong numY, ulong den)
        {
            this.x = x;
            this.y = y;
            this.NumX = numX;
            this.NumY = numY;
            this.Den = den;
        }

        public static IIntegerPoint Create(in I.Vector start, in I.Vector dest, in N.Fraction pos)
        {
            int dx = dest.x - start.x, sx;
            int dy = dest.y - start.y, sy;
            (sx, dx) = dx < 0 ? (-1, -dx) : (1, dx);
            (sy, dy) = dy < 0 ? (-1, -dy) : (1, dy);
            // calculate nearest integer positions and remainders
            ulong remX, remY;
            if (pos.Num > int.MaxValue || pos.Den > long.MaxValue)
            {
                // use decimal to avoid overflow
                decimal dxn = (decimal) dx * pos.Num;
                decimal dyn = (decimal) dy * pos.Num;
                remX = (ulong) (dxn % pos.Den);
                remY = (ulong) (dyn % pos.Den);
                dx = (int) (dxn / pos.Den);
                dy = (int) (dyn / pos.Den);
            }
            else
            {
                dx = (int) Math.DivRem(dx * (long) pos.Num, (long) pos.Den, out long lremX);
                dy = (int) Math.DivRem(dy * (long) pos.Num, (long) pos.Den, out long lremY);
                remX = (ulong) lremX;
                remY = (ulong) lremY;
            }

            // adjust if negative because Fraction is always positive
            if (sx < 0 && remX != 0)
            {
                dx++;
                remX = pos.Den - remX;
            }

            if (sy < 0 && remY != 0)
            {
                dy++;
                remY = pos.Den - remY;
            }

            int x = start.x + sx * dx;
            int y = start.y + sy * dy;

            if(remX == 0 && remY == 0)
            {
                return new I.Vector(x, y);
            }

            // reduce Fractions if possible, to make equality unique
            ulong r = N.Fraction.Gcd(remX, N.Fraction.Gcd(remY, pos.Den));

            return new Vector(x,y, remX / r, remY / r, pos.Den / r);
        }

        #endregion

        public int RoundX => x + (Den - NumX > NumX ? 0 : 1);

        public int RoundY => y + (Den - NumY > NumY ? 0 : 1);

        public I.Vector Ceiling => new I.Vector(x + (NumX == 0 ? 0 : 1), y + (NumY == 0 ? 0 : 1));


        public (int x, int y) xy => (x, y);

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
            return x ^ y ^ NumX.GetHashCode() ^ NumY.GetHashCode() ^ Den.GetHashCode();
#else
            return HashCode.Combine(x, y, NumX, NumY, Den);
#endif
        }

        public void Deconstruct(out int outX, out int outY)
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
