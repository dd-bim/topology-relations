using BigMath;

using GeometryLib.Interfaces;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

using I = GeometryLib.Int32.D2;
using I64 = GeometryLib.Int64.D2;

namespace GeometryLib.Decimal.D2
{
    public readonly struct Polygon : IReadOnlyList<LineString>, IGeometryOgc2<decimal>
    {

        public int GeometricDimension => 2;

        public ImmutableArray<LineString> Rings { get; }

        public BBox BBox => Rings[0].BBox;

        public decimal Area { get; }

        public int Count => Rings.Length;

        public LineString this[int index] => Rings[index];

        private Polygon(IReadOnlyList<LineString> rings, decimal area)
        {
            Rings = rings.ToImmutableArray();
            Area = area;
        }

        public Polygon(in LineString ring)
        {
            if (!ring.IsLinearRing)
                throw new ArgumentException($"Parameter {nameof(ring)} must be a LinearRing");
            Rings = ImmutableArray.Create(ring.Area < 0 ? ring.Reverse() : ring);
            Area = ring.Area;
        }

        public MultiLineString ToLineStringCollection() => new MultiLineString(this);

        public I.Polygon ToInteger(in Conversion conversion)
        {
            var lss = ImmutableArray.CreateBuilder<I.LineString>(Rings.Length);
            long area2 = 0;
            foreach (var ls in Rings)
            {
                var ils = ls.ToInteger(conversion);
                lss.Add(ils);
                area2 += ils.Area2;
            }

            return new I.Polygon(lss.MoveToImmutable(), area2);
        }

        public I64.Polygon ToInteger(in Conversion64 conversion)
        {
            var lss = ImmutableArray.CreateBuilder<I64.LineString>(Rings.Length);
            Int128 area2 = 0;
            foreach (var ls in Rings)
            {
                var ils = ls.ToInteger(conversion);
                lss.Add(ils);
                area2 += ils.Area2;
            }

            return new I64.Polygon(lss.MoveToImmutable(), area2);
        }

        public static bool Create(in IReadOnlyList<Vector> ring, out Polygon polygon)
        {
            var lineString = ring is LineString ls ? ls : new LineString(ring, true);
            if (!lineString.IsLinearRing || lineString.Area <= 0)
            {
                polygon = default;
                return false;
            }
            // evtl. drehen
            polygon = new Polygon(lineString);
            return true;
        }

        public static bool Create(out Polygon polygon, params LineString[] rings) => Create(rings, out polygon);

        public static bool Create(in IReadOnlyList<LineString>? rings, out Polygon polygon)
        {
            if (rings is null || rings.Count < 1)
            {
                polygon = default;
                return false;
            }
            var ls = rings[0];
            if (!ls.IsLinearRing)
            {
                polygon = default;
                return false;
            }
            decimal area = ls.Area;
            bool reverse = area < 0;
            var lss = new List<LineString>(rings.Count)
            {
                reverse ? ls.Reverse() : ls
            };

            for (int i = 1; i < rings.Count; i++)
            {
                ls = rings[i];
                if (ls.Area == 0)
                {
                    continue;
                }
                if (!ls.IsLinearRing || ls.Area > 0 != reverse || !lss[0].BBox.Encloses(ls.BBox))
                {
                    polygon = default;
                    return false;
                }
                area += ls.Area;
                lss.Add(reverse ? ls.Reverse() : ls);
            }

            area = reverse ? -area : area;

            if (area <= 0)
            {
                polygon = default;
                return false;
            }

            polygon = new Polygon(lss, area);
            return true;
        }

        public override string ToString()
        {
            var strings = new string[Rings.Length];
            for (int i = 0; i < Rings.Length; i++)
            {
                strings[i] = Rings[i].ToString();
            }
            return '(' + string.Join(",", strings) + ')';
        }

        public IEnumerator<LineString> GetEnumerator() => ((IEnumerable<LineString>)Rings).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        public string ToWktString() => WKTNames.Polygon + ToString();

        public static bool TryParse(in string input, out Polygon polygon)
        {
            int si = input.IndexOf('(') + 1;
            int lastei = input.LastIndexOf(')');
            if (si > 0 && (lastei - si) > 16)
            {
                si = input.IndexOf('(', si);
                if (si > 0)
                {
                    var rings = ImmutableArray.CreateBuilder<LineString>();
                    int ei = input.IndexOf(')', si) + 1;
                    while (ei > si && ei <= lastei && LineString.TryParse(input[si..ei], out var lineString, true))
                    {
                        rings.Add(lineString);
                        int ci = input.IndexOf(',', ei);
                        if (ci < 0) break;
                        si = input.IndexOf('(', ci);
                        ei = input.IndexOf(')', si) + 1;
                    }
                    return Create(rings, out polygon);
                }
            }
            polygon = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out Polygon polygon)
        {
            polygon = default;
            var wi = input.IndexOf(WKTNames.Polygon, StringComparison.InvariantCultureIgnoreCase);
            return wi >= 0 && TryParse(input[(wi + WKTNames.Polygon.Length)..], out polygon);
        }

    }
}