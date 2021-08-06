using GeometryLib.Interfaces;
using GeometryLib.Interfaces.D2;

using System;
using System.Globalization;

using Int = GeometryLib.Int32.D2;

namespace GeometryLib.Decimal.D2
{
    public readonly struct Vector : IEquatable<Vector>, IComparable<Vector>, IGeometryOgc2<decimal>, IVector<decimal>
    {
        private static readonly Vector zero = new Vector(0m, 0m);

        public int GeometricDimension => 0;

        /// <summary>
        /// Zero length Vector
        /// </summary>
        public static ref readonly Vector Zero => ref zero;

        /// <summary>X Axis Value</summary>
        public decimal x { get; }

        /// <summary>Y Axis Value</summary>
        public decimal y { get; }

        public (decimal x, decimal y) xy => (x, y);

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector"/> struct.
        /// </summary>
        public Vector(in decimal x, in decimal y)
        {
            this.x = x;
            this.y = y;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vector"/> struct.
        /// </summary>
        public Vector(in double x, in double y)
        {
            this.x = (decimal)(x);
            this.y = (decimal)(y);
        }

        public Int.Vector ToInteger(in Conversion conversion) => conversion.Convert(this);

        public GeometryLib.Int64.D2.Vector ToInteger(in Conversion64 conversion) => conversion.Convert(this);

        public decimal Sum() => x + y;

        public decimal AbsSum() => Math.Abs(x) + Math.Abs(y);

        public Vector Neg() => new Vector(-x, -y);

        public Vector Square() => new Vector(x * x, y * y);

        public Vector Abs() => new Vector(Math.Abs(x), Math.Abs(y));

        public decimal SumSq() => Dot(x, y);

        public Vector Add(in Vector other) => new Vector(x + other.x, y + other.y);

        public Vector Add(in decimal other) => new Vector(x + other, y + other);

        public Vector Sub(in Vector other) => new Vector(x - other.x, y - other.y);

        public Vector Sub(in decimal other) => new Vector(x - other, y - other);

        public Vector Mul(in int other) => new Vector(other * x, other * y);

        public Vector Mul(in decimal other) => new Vector(other * x, other * y);

        public decimal Det(in Vector other) => (x * other.y) - (y * other.x);

        public static decimal Det(in Vector a, in Vector b, in Vector c) => b.Sub(a).Det(c.Sub(a));

        public decimal Det(in (Vector a, Vector b) segment) => Det(segment.a, segment.b, this);

        public decimal Dot(in Vector other) => Dot(x, y, other.x, other.y);

        internal static decimal Dot(decimal ax, decimal ay, decimal bx, decimal by) => (ax * bx) + (ay * by);

        private static decimal Dot(decimal x, decimal y) => (x * x) + (y * y);

        public bool IsInside(Vector a, Vector b, Vector c) =>
               Det(a, b, this) > 0
            && Det(b, c, this) > 0
            && Det(c, a, this) > 0;

        public static bool Collinear(in Vector a, in Vector b, in Vector c) => Vector.Det(a, b, c) == 0;

        public bool Equals(Vector point) => x == point.x && y == point.y;

        public override bool Equals(object? obj) => obj is Vector point && Equals(point);

        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return x.GetHashCode() ^ y.GetHashCode();
#else
            return HashCode.Combine(x, y);
#endif
        }

        public int CompareTo(Vector other)
        {
            int comp = x.CompareTo(other.x);
            return comp != 0 ? comp : y.CompareTo(other.y);
        }

        public static bool operator ==(in Vector left, in Vector right) => left.Equals(right);

        public static bool operator !=(in Vector left, in Vector right) => !left.Equals(right);

        public static bool operator <(in Vector left, in Vector right) => left.CompareTo(right) < 0;

        public static bool operator <=(in Vector left, in Vector right) => left.CompareTo(right) <= 0;

        public static bool operator >(in Vector left, in Vector right) => left.CompareTo(right) > 0;

        public static bool operator >=(in Vector left, in Vector right) => left.CompareTo(right) >= 0;


        public static Vector operator +(in Vector left, in Vector right) => left.Add(right);

        public static Vector operator +(in Vector left, in decimal right) => left.Add(right);

        public static Vector operator -(in Vector left, in Vector right) => left.Sub(right);

        public static Vector operator -(in Vector left, in decimal right) => left.Sub(right);

        public static Vector operator -(in Vector value) => value.Neg();

        public static decimal operator *(in Vector left, in Vector right) => left.Dot(right);

        public static Vector operator *(in int left, in Vector right) => right.Mul(left);

        public static Vector operator *(in Vector left, in int right) => left.Mul(right);

        public static Vector operator *(in Vector left, in decimal right) => left.Mul(right);

        public static Vector operator /(in Vector left, in decimal right) => left.Mul(1m / right);

        public override string ToString() => string.Format(CultureInfo.InvariantCulture, "{0} {1}", x, y);

        public static (Vector, Vector) Sort(in Vector a, in Vector b) => a.CompareTo(b) > 0 ? (b, a) : (a, b);

        public Vector Mid(in Vector other) => new Vector(
            (x + other.x) * 0.5m,
            (y + other.y) * 0.5m);

        public Vector Min(in Vector other) => new Vector(
            Math.Min(x, other.x),
            Math.Min(y, other.y));

        public Vector Max(in Vector other) => new Vector(
            Math.Max(x, other.x),
            Math.Max(y, other.y));



        public void Deconstruct(out decimal outX, out decimal outY) => (outX, outY) = (x, y);


        private static int circleSign(in Vector a, in Vector b, in Vector c)
        {
            //Det(in Vector a, in Vector b) => a.Sub(this).Det(b.Sub(this));
            // Nicht under/over-flow sicher!
            var ab = a.Det(b);
            var bc = b.Det(c);
            var ca = c.Det(a);

            int s1 = Math.Sign(ab);
            int s2 = Math.Sign(bc);
            int s3 = Math.Sign(ca);
            Int32.Helper.Sort(ref s1, ref s2, ref s3);

            /*
             * Dreieck muss ccw sein!
             * s1  s2  s3
             * <0  <0  >0 = Außen (hinter Vertex)           = s2 
             * <0  =0  >0 = Außen (hinter Vertex auf Kante) = <0
             * <0  >0  >0 = Außen (neben Kante)             =  ?
             * =0  =0  >0 = auf Vertex                      = s2
             * =0  >0  >0 = auf Kante                       = s2
             * >0  >0  >0 = innen                           = s2
             */

            int sig = s2;
            if (s1 < 0)
            {
                if (s2 > 0)
                {
                    var ad = a.SumSq();
                    var bd = b.SumSq();
                    var cd = c.SumSq();
                    //return false;
                    sig = Math.Sign((ad * bc) + (bd * ca) + (cd * ab));
                }
                else
                {
                    sig = -1;
                }
            }

            return sig;
        }

        public int CircleSign(in Vector a, in Vector b, in Vector c) => circleSign(a - this, b - this, c - this);

        public Vector OtherY(in Vector other) => new Vector(x, other.y);

        public Vector AddOne() => new Vector(x + 1, y + 1);

        public Vector SubOne() => new Vector(x - 1, y - 1);


        public string ToWktString() => $"{WKTNames.Point}({ToString()})";

        public static bool TryParse(in string input, out Vector vector, char separator = ' ')
        {
            string str = input.Trim();
            if (!string.IsNullOrEmpty(str))
            {
                string[] split = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length >= 2 
                    && decimal.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x)
                    && decimal.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                {
                    vector = new Vector(x, y);
                    return true;
                }
            }
            vector = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out Vector vector)
        {
            var si = input.IndexOf(WKTNames.Point, StringComparison.InvariantCultureIgnoreCase) + WKTNames.Point.Length + 1;
            var ei = input.LastIndexOf(')');
            if ((ei - si) > 2)
                return TryParse(input[si..ei], out vector);
            vector = default;
            return false;
        }

        public Vector Min(IVector<decimal> other)=> new Vector(
            Math.Min(x, other.x),
            Math.Min(y, other.y));

        public Vector Max(IVector<decimal> other) => new Vector(
            Math.Max(x, other.x),
            Math.Max(y, other.y));

        public int SideSign(IVector<decimal> v2, IVector<decimal> v3)
        {
            throw new NotImplementedException();
        }

        IVector<decimal> IVector<decimal>.Min(IVector<decimal> other) => (IVector<decimal>)Min(other);

        IVector<decimal> IVector<decimal>.Max(IVector<decimal> other) => (IVector<decimal>)Max(other);
    }
}
