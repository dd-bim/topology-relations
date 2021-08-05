using GeometryLib.Interfaces;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

using I = GeometryLib.Int32.D2;

using I64 = GeometryLib.Int64.D2;

namespace GeometryLib.Decimal.D2
{
    public readonly struct MultiLineString : IReadOnlyList<LineString>, IGeometryOgc2<decimal>
    {

        public int GeometricDimension => 1;

        public ImmutableArray<LineString> LineStrings { get; }

        public BBox BBox { get; }

        public int Count => LineStrings.Length;

        public LineString this[int index] => LineStrings[index];

        public MultiLineString(in LineString lineString)
        {
            LineStrings = ImmutableArray.Create(lineString.MakeInvalidLinearRing());
            BBox = lineString.BBox;
        }

        public MultiLineString(in IReadOnlyCollection<LineString> lineStrings)
        {
            var lss = ImmutableArray.CreateBuilder<LineString>(lineStrings.Count);
            var box = BBox.Empty;
            foreach (var ls in lineStrings)
            {
                lss.Add(ls.MakeInvalidLinearRing());
                box = box.Combine(ls.BBox);
            }

            LineStrings = lss.MoveToImmutable();
            BBox = box;
        }

        public MultiLineString(in Polygon polygon)
        {
            var lss = ImmutableArray.CreateBuilder<LineString>(polygon.Count);
            foreach (var ls in polygon)
            {
                lss.Add(ls.MakeInvalidLinearRing());
            }

            LineStrings = lss.MoveToImmutable();
            BBox = polygon.BBox;
        }


        public I.MultiLineString ToInteger(in Conversion conversion)
        {
            var lss = ImmutableArray.CreateBuilder<I.LineString>(LineStrings.Length);
            var box = I.BBox.Empty;
            foreach (var ls in LineStrings)
            {
                var ils = ls.ToInteger(conversion);
                lss.Add(ils);
                box.Combine(ils.BBox);
            }

            return new I.MultiLineString(box, lss.MoveToImmutable());
        }

        public I64.MultiLineString ToInteger(in Conversion64 conversion)
        {
            var lss = ImmutableArray.CreateBuilder<I64.LineString>(LineStrings.Length);
            var box = I64.BBox.Empty;
            foreach (var ls in LineStrings)
            {
                var ils = ls.ToInteger(conversion);
                lss.Add(ils);
                box.Combine(ils.BBox);
            }

            return new I64.MultiLineString(box, lss.MoveToImmutable());
        }

        public string ToString(string separator)
        {
            var lss = new string[LineStrings.Length];
            for (int i = 0; i < LineStrings.Length; i++)
            {
                lss[i] = LineStrings[i].ToString();
            }
            return '(' + string.Join(separator, lss) + ')';
        }

        public override string ToString() => ToString(",");

        public IEnumerator<LineString> GetEnumerator() => ((IEnumerable<LineString>)LineStrings).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string ToWktString() => WKTNames.Polygon + ToString();

        public static bool TryParse(in string input, out MultiLineString multi)
        {
            int si = input.IndexOf('(') + 1;
            int lastei = input.LastIndexOf(')');
            if (si > 0 && (lastei - si) > 16)
            {
                si = input.IndexOf('(', si);
                if (si > 0)
                {
                    var lss = new List<LineString>();
                    int ei = input.IndexOf(')', si) + 1;
                    while (ei > si && ei <= lastei && LineString.TryParse(input[si..ei], out var lineString))
                    {
                        lss.Add(lineString);
                        int ci = input.IndexOf(',', ei);
                        if (ci < 0) break;
                        si = input.IndexOf('(', ci);
                        ei = input.IndexOf(')', si) + 1;
                    }
                    multi = new MultiLineString(lss);
                    return true;
                }
            }
            multi = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out MultiLineString multi)
        {
            multi = default;
            var wi = input.IndexOf(WKTNames.MultiLineString, StringComparison.InvariantCultureIgnoreCase);
            return wi >= 0 && TryParse(input[(wi + WKTNames.MultiLineString.Length)..], out multi);
        }


    }
}