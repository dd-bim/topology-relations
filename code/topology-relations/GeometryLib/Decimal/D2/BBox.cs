using System;
using System.Collections.Generic;

namespace GeometryLib.Decimal.D2
{
    /// <summary>
    /// Bounding Box
    /// </summary>
    public readonly struct BBox
    {
        private static readonly BBox empty = new BBox
        (
           new Vector(decimal.MaxValue, decimal.MaxValue),
           new Vector(decimal.MinValue, decimal.MinValue),0
        );

        /// <summary>
        /// Minimal point
        /// </summary>
        public Vector Min { get; }

        /// <summary>
        /// Maximal point
        /// </summary>
        public Vector Max { get; }


        public readonly byte ScaleMax;


        /// <summary>
        /// Bounding Box with no extent
        /// </summary>
        public static ref readonly BBox Empty => ref empty;

        public BBox(in Vector vector)
        {
            this.Min = vector;
            this.Max = vector;
            ScaleMax = (byte)Math.Max(vector.x.ScaleExp(), vector.y.ScaleExp());
        }

        public BBox(in Vector min, in Vector max, byte maxScale)
        {
            this.Min = min;
            this.Max = max;
            ScaleMax = maxScale;
        }

        public static BBox FromVectors(in IReadOnlyCollection<Vector> vectors)
        {
            var box = Empty;
            foreach (var v in vectors)
            {
                box += v;
            }
            return box;
        }

        public Vector Centre => Min.Mid(Max);

        public Vector Range => Max - Min;

        public BBox Extend(in Interfaces.D2.IVector<decimal> vector)
        {
            byte scale = (byte)Math.Max(vector.x.ScaleExp(), vector.y.ScaleExp());
            return new BBox((Vector)vector.Min(Min), (Vector)vector.Max(Max), Math.Max(scale, ScaleMax));
        }

        public BBox Combine(in BBox other) =>
            new BBox(Min.Min(other.Min), Max.Max(other.Max), Math.Max(other.ScaleMax, ScaleMax));

        public bool DoOverlap(in BBox other) =>
            (other.Min.x < Max.x) && (Min.x < other.Max.x) && (other.Min.y < Max.y) && (Min.y < other.Max.y);

        public bool DoOverlapOrTouch(in BBox other) =>
            (other.Min.x <= Max.x) && (Min.x <= other.Max.x) && (other.Min.y <= Max.y) && (Min.y <= other.Max.y);

        public bool Encloses(in BBox other) =>
            (other.Min.x >= Min.x) && (other.Max.x <= Max.x) && (other.Min.y >= Min.y) && (other.Max.y <= Max.y);

        public bool Distinct(in BBox other) =>
            (other.Max.x < Min.x) || (other.Min.x > Max.x) || (other.Max.y < Min.y) || (other.Min.y > Max.y);

        public bool Encloses(in Vector vector) =>
            (vector.x > Min.x) && (vector.x < Max.x) && (vector.y > Min.y) && (vector.y < Max.y);

        public bool EnclosesOrTouch(in Vector vector) =>
            (vector.x >= Min.x) && (vector.x <= Max.x) && (vector.y >= Min.y) && (vector.y <= Max.y);

        public bool Distinct(in Vector vector) =>
            (vector.x < Min.x) || (vector.x > Max.x) || (vector.y < Min.y) || (vector.y > Max.y);

        public static BBox operator +(in BBox box, in Vector vector) => box.Extend(vector);

        public static BBox operator +(in BBox left, in BBox right) => left.Combine(right);

        public override string ToString() => $"Min({Min}) Max({Max})";
    }
}
