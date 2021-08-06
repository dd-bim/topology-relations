using BigMath;

using GeometryLib.Interfaces;
using GeometryLib.Interfaces.D2;

using System;
using System.Globalization;
using System.Numerics;

using Fra2 = GeometryLib.Int64.Fraction.D2;
using Numb = GeometryLib.Numbers;

namespace GeometryLib.Int64.D2
{
    public readonly struct Vector : IEquatable<Vector>, IComparable<Vector>, IIntegerPoint, IGeometryOgc2<long>, IVector<long>
    {
        public int GeometricDimension => 0;

        private static readonly Vector zero = new Vector(0, 0);

        /// <summary>
        ///     Zero length Vector
        /// </summary>
        public static ref readonly Vector Zero => ref zero;

        /// <summary>X Axis Value</summary>
        public long x { get; }

        /// <summary>Y Axis Value</summary>
        public long y { get; }

        public (long x, long y) xy => (x, y);

        public decimal DecimalX => x;

        public decimal DecimalY => y;

        public Decimal.D2.Vector ToDecimal() => new Decimal.D2.Vector(DecimalX, DecimalY);

        /// <summary>
        ///     Initializes a new instance of the <see cref="Vector" /> struct.
        /// </summary>
        public Vector(in long x, in long y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return ToString(" ");
        }

        public string ToString(string separator)
        {
            return string.Format("{0}{2}{1}", x, y, separator);
        }

        public Vector((int x, int y) p) : this(p.x, p.y)
        {
        }

        public void Deconstruct(out long xOut, out long yOut)
        {
            (xOut, yOut) = (x, y);
        }

        public Int128 Sum()
        {
            return (Int128)x + y;
        }

        public Int128 AbsSum()
        {
            return (Int128)Math.Abs(x) + Math.Abs(y);
        }

        public Vector Neg()
        {
            return new Vector(-x, -y);
        }

        public Vector Square()
        {
            return new Vector(x * x, y * y);
        }

        public Vector Abs()
        {
            return new Vector(Math.Abs(x), Math.Abs(y));
        }

        public Int128 SumSq()
        {
            return Dot(x, y);
        }

        public Vector Add(in Vector other)
        {
            return new Vector(x + other.x, y + other.y);
        }

        public Vector Add(in long other)
        {
            return new Vector(x + other, y + other);
        }

        public Vector Sub(in Vector other)
        {
            return new Vector(x - other.x, y - other.y);
        }

        public Vector Sub(in long other)
        {
            return new Vector(x - other, y - other);
        }

        public Vector Mul(in long other)
        {
            return new Vector(other * x, other * y);
        }

        public static Int128 Det(in Vector a, in Vector b, in Vector c)
        {
            var ax = (Int128)b.x - a.x;
            var ay = (Int128)b.y - a.y;
            var bx = (Int128)c.x - a.x;
            var by = (Int128)c.y - a.y;
            return (ax * by) - (ay * bx);
        }

        public Int128 Det(in Vector a, in Vector b)
        {
            return Det(a, b, this);
        }

        public Int128 Det(in (Vector a, Vector b) segment)
        {
            return Det(segment.a, segment.b, this);
        }

        public Int128 Det(in Vector other)
        {
            return Det(x, y, other.x, other.y);
        }

        private static Int128 Det(long ax, long ay, long bx, long by)
        {
            return ((Int128)ax * by) - ((Int128)ay * bx);
        }

        public Int128 Dot(Vector other)
        {
            return Dot(x, y, other.x, other.y);
        }

        internal static Int128 Dot(long ax, long ay, long bx, long by)
        {
            return ((Int128)ax * bx) + ((Int128)ay * by);
        }

        private static Int128 Dot(long x, long y)
        {
            return ((Int128)x * x) + ((Int128)y * y);
        }

        public bool IsBetween(in Vector a, in Vector b)
        {
            var ab = b.Sub(a);
            var ap = Sub(a);
            if (ab.Det(ap) != 0) return false;

            var lnum = ab.Dot(ap);
            var lden = ab.SumSq();

            return lnum > 0 && lnum < lden;
        }


        public bool IsOnRay(in Vector orig, in Vector dest, out Int128 det)
        {
            var ab = dest.Sub(orig);
            var ap = Sub(orig);
            det = ab.Det(ap);
            return det == 0 && ab.Dot(ap) > 0;
        }


        public bool IsInside(Vector a, Vector b, Vector c)
        {
            return Det(a, b, this) > 0 && Det(b, c, this) > 0 && Det(c, a, this) > 0;
        }

        public bool IsDisjoint(Vector a, Vector b, Vector c)
        {
            return Det(a, b, this) < 0 || Det(b, c, this) < 0 || Det(c, a, this) < 0;
        }


        internal static bool Intersect(in Vector a1, in Vector b1, in Vector a2, in Vector b2,
            out Int128 num1, out Int128 num2, out Int128 den)
        {
            var aa = a2 - a1;
            var ab1 = b1 - a1;
            var ab2 = b2 - a2;

            num1 = ab2.Det(aa);
            num2 = ab1.Det(aa);
            den = ab2.Det(ab1);
            // force positive denominator
            if (den < 0)
            {
                num1 = -num1;
                num2 = -num2;
                den = -den;
            }

            if (num1 < 0 || num2 < 0 || den == 0 || num1 > den || num2 > den)
            {
                num1 = default;
                num2 = default;
                den = default;
                return false;
            }

            return true;
        }

        public static bool Collinear(in Vector a, in Vector b, in Vector c)
        {
            return Det(a, b, c) == 0;
        }

        public bool Collinear(in Vector a, in Vector b)
        {
            return Det(a, b) == 0;
        }

        internal static bool Intersect(in Vector a1, in Vector b1, in Vector a2, in Vector b2, out Int128 num1,
            out Int128 den)
        {
            var aa = a2 - a1;
            var ab1 = b1 - a1;
            var ab2 = b2 - a2;

            num1 = ab2.Det(aa);
            den = ab2.Det(ab1);
            // force positive denominator
            if (den < 0)
            {
                num1 = -num1;
                den = -den;
            }

            if (num1 < 0 || den == 0 || num1 > den)
            {
                num1 = default;
                den = default;
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Relative position of intersection point of two segments on first segment
        /// </summary>
        /// <param name="a1"> Start of first segment </param>
        /// <param name="b1"> Dest of first segment </param>
        /// <param name="a2"> Start of second segment </param>
        /// <param name="b2"> Dest of second segment </param>
        /// <param name="pos1"> </param>
        /// <param name="pos2"> </param>
        /// <returns> </returns>
        public static bool Intersect(in Vector a1, in Vector b1, in Vector a2, in Vector b2,
            out Numb.Fraction128 pos1, out Numb.Fraction128 pos2)
        {
            if (Intersect(a1, b1, a2, b2, out Int128 num1, out Int128 num2, out Int128 den))
            {
                pos1 = new Numb.Fraction128(num1, den);
                pos2 = new Numb.Fraction128(num2, den);
                return true;
            }

            pos1 = default;
            pos2 = default;
            return false;
        }

        /// <summary>
        ///     Relative position of intersection point of two segments on first segment
        /// </summary>
        /// <param name="a1"> Start of first segment </param>
        /// <param name="b1"> Dest of first segment </param>
        /// <param name="a2"> Start of second segment </param>
        /// <param name="b2"> Dest of second segment </param>
        /// <param name="pos1"> </param>
        /// <returns> </returns>
        public static bool Intersect(in Vector a1, in Vector b1, in Vector a2, in Vector b2, out Numb.Fraction128 pos1)
        {
            if (Intersect(a1, b1, a2, b2, out Int128 num1, out Int128 den))
            {
                pos1 = new Numb.Fraction128(num1, den);
                return true;
            }

            pos1 = default;
            return false;
        }

        /// <summary>
        ///     Position on line a b (if possible)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="point"></param>
        /// <param name="num"></param>
        /// <param name="den"></param>
        public static bool PositionOf(in Vector a, in Vector b, in Vector point, out Int128 num, out Int128 den)
        {
            var ap = point - a;
            var ab = b - a;

            num = ab.Dot(ap);
            den = ab.SumSq();

            if (num < 0 || den == 0 || num > den)
            {
                num = default;
                den = default;
                return false;
            }

            return true;
        }

        /// <summary> Calculates point position on segment</summary>
        public static bool PositionOf(in Vector a, in Vector b, in Vector point, out Numb.Fraction128 position)
        {
            if (PositionOf(a, b, point, out Int128 num, out Int128 den))
            {
                position = new Numb.Fraction128(num, den);
                return true;
            }

            position = default;
            return false;
        }

        public bool Equals(Vector point)
        {
            return x == point.x && y == point.y;
        }

        public bool Equals(Fra2.Vector point)
        {
            return point.Equals(this);
        }

        public override bool Equals(object? obj)
        {
            return obj is Vector point && Equals(point);
        }

        ///// <inheritdoc />
        //public bool Equals(IPoint? other)
        //{
        //    return other switch
        //    {
        //        PointF pointF => Equals(pointF),
        //        Point point => Equals(point),
        //        _ => false
        //    };
        //}

        public override int GetHashCode()
        {
#if NETSTANDARD2_0 || NET472 || NET48 || NET462
            return (x ^ y).GetHashCode();
#else
            return HashCode.Combine(x, y);
#endif
        }

        public int CompareTo(Vector other)
        {
            int comp = x.CompareTo(other.x);
            return comp != 0 ? comp : y.CompareTo(other.y);
        }

        public static bool operator ==(in Vector left, in Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(in Vector left, in Vector right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(in Vector left, in Vector right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator <=(in Vector left, in Vector right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >(in Vector left, in Vector right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator >=(in Vector left, in Vector right)
        {
            return left.CompareTo(right) >= 0;
        }


        //public static bool operator ==(in Point left, in IPoint right) => left.Equals(right);

        //public static bool operator !=(in Point left, in IPoint right) => !left.Equals(right);

        //public static bool operator ==(in IPoint left, in Point right) => (left != null) && left.Equals(right);

        //public static bool operator !=(in IPoint left, in Point right) => (left != null) && !left.Equals(right);

        public static (Vector, Vector) Sort(in Vector a, in Vector b)
        {
            return a.CompareTo(b) > 0 ? (b, a) : (a, b);
        }

        public Vector Min(in Vector other)
        {
            return new Vector(
                Math.Min(x, other.x),
                Math.Min(y, other.y));
        }

        public Vector Max(in Vector other)
        {
            return new Vector(
                Math.Max(x, other.x),
                Math.Max(y, other.y));
        }

        public static Vector operator +(in Vector left, in Vector right)
        {
            return left.Add(right);
        }

        public static Vector operator +(in Vector left, in int right)
        {
            return left.Add(right);
        }

        public static Vector operator -(in Vector left, in Vector right)
        {
            return left.Sub(right);
        }

        public static Vector operator -(in Vector left, in int right)
        {
            return left.Sub(right);
        }

        public static Vector operator -(in Vector value)
        {
            return value.Neg();
        }

        public static Int128 operator *(in Vector left, in Vector right)
        {
            return left.Dot(right);
        }

        public static Vector operator *(in int left, in Vector right)
        {
            return right.Mul(left);
        }

        public static Vector operator *(in Vector left, in int right)
        {
            return left.Mul(right);
        }

        /// <summary>
        /// Returns only true if this is in Direction of edge
        /// </summary>
        /// <param name="edgeA"></param>
        /// <param name="edgeB"></param>
        /// <returns></returns>
        public bool InDirection(in Vector edgeA, in Vector edgeB)
        {
            var bx = (Int128)edgeB.x - edgeA.x;
            var tx = (Int128)x - edgeA.x;
            var by = (Int128)edgeB.y - edgeA.y;
            var ty = (Int128)y - edgeA.y;
            var det = (bx * ty) - (by * tx);
            var dotx = bx * tx;
            var doty = by * ty;
            var sx = dotx.Sign;
            var sy = doty.Sign;
            // && (dotx + doty) > 0;
            return det == 0 && (((sx + sy) > 0) 
                || (sx < 0 && sy > 0 && doty > dotx) 
                || (sy < 0 && sx > 0 && dotx > doty));
        }

        //public long DirectedEdgeSign(in Vector edgeA, in Vector edgeB)
        //{
        //    int bx = edgeB.x - edgeA.x;
        //    int tx = x - edgeA.x;
        //    int by = edgeB.y - edgeA.y;
        //    int ty = y - edgeA.y;
        //    long det = Math.BigMul(bx, ty) - Math.BigMul(by, tx);
        //    return det == 0 ? (Math.BigMul(bx, tx) + Math.BigMul(by, ty)) > 0 ? 0 : -1 : det;

        //    //return Math.Sign(Math.BigMul(lineA.x - x, lineB.y - y) - Math.BigMul(lineA.y - y, lineB.x - x));
        //}

        //private static int inCircle(in Vector a, in Vector b, in Vector c)
        //{
        //    //Det(in Vector a, in Vector b) => a.Sub(this).Det(b.Sub(this));
        //    // Nicht under/over-flow sicher!
        //    var ab = a.Det(b);
        //    var bc = b.Det(c);
        //    var ca = c.Det(a);

        //    int s1 = Math.Sign(ab);
        //    int s2 = Math.Sign(bc);
        //    int s3 = Math.Sign(ca);
        //    GeometryLib.Int32.Helper.Sort(ref s1, ref s2, ref s3);

        //    /*
        //     * Dreieck muss ccw sein!
        //     * s1  s2  s3
        //     * <0  <0  >0 = Außen (hinter Vertex)           = s2 
        //     * <0  =0  >0 = Außen (hinter Vertex auf Kante) = <0
        //     * <0  >0  >0 = Außen (neben Kante)             =  ?
        //     * =0  =0  >0 = auf Vertex                      = s2
        //     * =0  >0  >0 = auf Kante                       = s2
        //     * >0  >0  >0 = innen                           = s2
        //     */

        //    int sig = s2;
        //    if (s1 < 0)
        //    {
        //        if (s2 > 0)
        //        {
        //            var ad = a.SumSq();
        //            var bd = b.SumSq();
        //            var cd = c.SumSq();
        //            sig = (BigInteger.Multiply(ad, bc) 
        //                 + BigInteger.Multiply(bd, ca) 
        //                 + BigInteger.Multiply(cd, ab)).Sign;
        //        }
        //        else
        //        {
        //            sig = -1;
        //        }
        //    }

        //    return sig;
        //}

//        REAL incirclefast(pa, pb, pc, pd)
//{
//  adx = pa[0] - pd[0];
//  ady = pa[1] - pd[1];
//  bdx = pb[0] - pd[0];
//  bdy = pb[1] - pd[1];
//  cdx = pc[0] - pd[0];
//  cdy = pc[1] - pd[1];

//  abdet = adx* bdy - bdx* ady;
//  bcdet = bdx* cdy - cdx* bdy;
//  cadet = cdx* ady - adx* cdy;
//  alift = adx* adx + ady* ady;
//  blift = bdx* bdx + bdy* bdy;
//  clift = cdx* cdx + cdy* cdy;

//  return alift* bcdet + blift* cadet + clift* abdet;
//    }

    public bool InCircle(in Vector a, in Vector b, in Vector c) // => inCircle(a - this, b - this, c - this);
        {
            // Quelle:https://www.cs.cmu.edu/afs/cs/project/quake/public/code/predicates.c : incirclefast

            var adx = (Int128)a.x - x;
            var ady = (Int128)a.y - y;
            var bdx = (Int128)b.x - x;
            var bdy = (Int128)b.y - y;
            var cdx = (Int128)c.x - x;
            var cdy = (Int128)c.y - y;

            var abdet = adx * bdy - bdx * ady;
            var bcdet = bdx * cdy - cdx * bdy;
            var cadet = cdx * ady - adx * cdy;
            var alift = adx * adx + ady * ady;
            var blift = bdx * bdx + bdy * bdy;
            var clift = cdx * cdx + cdy * cdy;

            return (((Int256)alift * (Int256)bcdet) 
                + ((Int256)blift * (Int256)cadet)
                + ((Int256)clift * (Int256)abdet)).Sign > 0;



            //long cax = (long)a.x - c.x;
            //long pax = (long)a.x - x;
            //long cbx = (long)b.x - c.x;
            //long pbx = (long)b.x - x;
            //long cay = (long)a.y - c.y;
            //long pay = (long)a.y - y;
            //long cby = (long)b.y - c.y;
            //long pby = (long)b.y - y;

            //long e = (cax * cbx) + (cay * cby);
            //long f = (pbx * pay) - (pax * pby);
            //long g = (cbx * cay) - (cax * cby);
            //long h = (pbx * pax) + (pay * pby);

            ////var comp = N.Misc.ProductCompare(e, f, g, h);

            //// return BigInteger.Multiply(e, f).CompareTo(BigInteger.Multiply(g, h)) < 0;
            //return (BigInteger.Multiply(e, f) + BigInteger.Multiply(g, h)).Sign < 0;
        }

        public Vector OtherY(in Vector other)
        {
            return new Vector(x, other.y);
        }


        public Vector AddOne()
        {
            return new Vector(x + 1, y + 1);
        }

        public Vector SubOne()
        {
            return new Vector(x - 1, y - 1);
        }

        //public bool IsBetween(in IIntegerPoint a, in IIntegerPoint b)
        //{
        //    switch ((a, b))
        //    {
        //        case (Vector av, Vector bv):
        //            return IsBetween(av, bv);
        //        case (F.Vector av, Vector bv):
        //            return new F.Vector(this).IsBetween(av, new F.Vector(bv));
        //        case (Vector av, F.Vector bv):
        //            return new F.Vector(this).IsBetween(new F.Vector(av), bv);
        //        case (F.Vector av, F.Vector bv):
        //            return new F.Vector(this).IsBetween(av, bv);
        //        default:
        //            throw new NotImplementedException();
        //    }
        //}

        //public int EdgeSign(in IIntegerPoint a, in IIntegerPoint b)
        //{
        //    if (a is Vector av && b is Vector bv) return EdgeSign(av, bv);
        //    else return new F.Vector(this).EdgeSign(a, b);
        //}

        //public bool InCircle(in IIntegerPoint a, in IIntegerPoint b, in IIntegerPoint c)
        //{
        //    if (a is Vector av && b is Vector bv && c is Vector cv) return InCircle(av, bv, cv);

        //    throw new NotImplementedException();
        //}

        public bool Equals(IIntegerPoint? other)
        {
            return other is Vector v ? Equals(v) : other is Fra2.Vector fv && fv.Equals(this);
        }

        public string ToWktString() => $"{WKTNames.Point}({ToString()})";

        public static bool TryParse(in string input, out Vector vector, char separator = ' ')
        {
            string str = input.Trim();
            if (!string.IsNullOrEmpty(str))
            {
                string[] split = str.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length >= 2
                             && long.TryParse(split[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out long x)
                             && long.TryParse(split[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out long y))
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

        IVector<long> IVector<long>.Min(IVector<long> other)
        {
            return Min(other is Vector v ? v : new Vector(other.x, other.y));
        }

        IVector<long> IVector<long>.Max(IVector<long> other)
        {
            return Max(other is Vector v ? v : new Vector(other.x, other.y));
        }


        int IVector<long>.SideSign(IVector<long> v2, IVector<long> v3)
        {
            return (Det(
                v2 is Vector v ? v : new Vector(v2.x, v2.y),
                v3 is Vector vv ? vv : new Vector(v3.x, v3.y)
                )).Sign;
        }
    }
}