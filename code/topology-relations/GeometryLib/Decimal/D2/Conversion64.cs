using GeometryLib;
using GeometryLib.Int64;

using System;
using System.Diagnostics.Contracts;

using static System.Math;

using Fra2 = GeometryLib.Int64.Fraction.D2;
using Int2 = GeometryLib.Int64.D2;

namespace GeometryLib.Decimal.D2
{
    public struct Conversion64
    {
        public const decimal Shift = 1;
        // smaller as possible to allow external points
        public static readonly decimal MaxValue = long.MaxValue - 2;

        private static readonly Int2.Vector minXMinY = new Int2.Vector(0, 0);

        private static readonly Int2.Vector minXMaxY = new Int2.Vector(0, long.MaxValue);

        private static readonly Int2.Vector maxXMinY = new Int2.Vector(long.MaxValue, 0);

        public static ref readonly Int2.Vector MinXMinY => ref minXMinY;

        public static ref readonly Int2.Vector MinXMaxY => ref minXMaxY;

        public static ref readonly Int2.Vector MaxXMinY => ref maxXMinY;

        public readonly bool Approx;

        public readonly decimal Scale;

        public BBox BBox { get; }

        public decimal PointDistance => Constants.SQRT2 / Scale;

        private static decimal GetScale(decimal range, byte maxScale, out bool approx)
        {
            if (range <= MaxValue && range.UnScaleBy(maxScale, out var srange) && srange <= MaxValue)
            {
                approx = false;
                return 1m / 1m.ScaleBy(maxScale);
            }
            else
            {
                approx = true;
                var s = MaxValue / range;
                if(s * range > MaxValue)
                {
                    // letzte Stelle verkleinern, wenn gerundet
                    var r = s.ScaleExp();
                    s -= 1m.ScaleBy(r);
                }
                return s;
                //return Truncate(MaxValue / range);
            }
        }

        public Conversion64(in BBox box)
        {
            var range = box.Range;
            BBox = box;
            Scale = GetScale(Max(range.x, range.y), box.ScaleMax, out Approx);
        }


        [Pure]
        public Int2.Vector Convert(in Interfaces.D2.IVector<decimal> p)
        {
            decimal x = (p.x - BBox.Min.x) * Scale + Shift;
            decimal y = (p.y - BBox.Min.y) * Scale + Shift;
            return new Int2.Vector((long)x, (long)y);
        }

        public Vector Convert(in Int2.Vector p) =>
            new Vector((((decimal)p.x - Shift) / Scale) + BBox.Min.x, (((decimal)p.y - Shift) / Scale) + BBox.Min.y);


        public Vector Convert(in Fra2.Vector p) =>
            new Vector(((p.DecimalX - Shift) / Scale) + BBox.Min.x, ((p.DecimalY - Shift) / Scale) + BBox.Min.y);


        public Vector Convert(in IIntegerPoint p) => p is Int2.Vector ip ? Convert(ip) : Convert((Fra2.Vector)p);

        public Vector Convert(in Int32.IIntegerPoint p)
        {
            throw new NotImplementedException();
        }
    }
}