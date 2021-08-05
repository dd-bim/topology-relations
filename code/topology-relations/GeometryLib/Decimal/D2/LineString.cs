using GeometryLib.Interfaces;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

using I = GeometryLib.Int32.D2;
using I64 = GeometryLib.Int64.D2;

namespace GeometryLib.Decimal.D2
{
    public readonly struct LineString : IReadOnlyList<Vector>, IGeometryOgc2<decimal>
    {
        private static readonly LineString empty = new LineString(
            BBox.Empty, ImmutableArray<Vector>.Empty, 0);

        public int GeometricDimension => 1;

        public static ref readonly LineString Empty => ref empty;

        public BBox BBox { get; }

        public ImmutableArray<Vector> Vertices { get; }

        public bool IsClosed { get; }

        public int Count => Vertices.Length;

        public decimal Area { get; }

        public bool IsLinearRing => Area != 0m;

        public Vector this[int index] => Vertices[index];

        private static bool isClosed(in ImmutableArray<Vector> vertices) => vertices.Length > 2 && vertices[0].Equals(vertices[^1]);

        private static (bool isClosed, decimal area) GetArea(in ImmutableArray<Vector> vertices)
        {
            bool isClosed = LineString.isClosed(vertices);
            if (!isClosed || vertices.Length <= 3) return (isClosed, 0m);
            decimal area = vertices[0].x * (vertices[1].y - vertices[^2].y);
            var prev = vertices[0];
            for (int i = vertices.Length - 2; i > 0; i--)
            {
                area += (vertices[i].x * (prev.y - vertices[i - 1].y));
                prev = vertices[i];
            }
            area *= 0.5m;
            return (true, area);
        }

        internal LineString(in BBox bBox, in ImmutableArray<Vector> vertices, in bool isClosed, in decimal area)
        {
            BBox = bBox;
            Vertices = vertices;
            IsClosed = isClosed;
            Area = area;
        }

        private LineString(in BBox bBox, in ImmutableArray<Vector> vertices, in decimal area)
        {
            BBox = bBox;
            Vertices = vertices;
            IsClosed = area != 0 || isClosed(vertices);
            Area = area;
        }

        public LineString(in IReadOnlyList<Vector>? vertices, bool isLinearRing)
        {
            if (vertices is null || vertices.Count < 1)
            {
                Vertices = ImmutableArray<Vector>.Empty;
                BBox = BBox.Empty;
                IsClosed = false;
                Area = 0m;
            }
            else
            {
                Vertices = vertices.ToImmutableArray();
                BBox = BBox.FromVectors(Vertices);
                if (isLinearRing)
                {
                    (IsClosed, Area) = GetArea(Vertices);
                }
                else
                {
                    IsClosed = isClosed(Vertices);
                    Area = 0;
                }
            }
        }

        public I.LineString ToInteger(in Conversion conversion)
        {
            var vs = ImmutableArray.CreateBuilder<I.Vector>(Vertices.Length);
            var box = I.BBox.Empty;
            foreach (var vertex in Vertices)
            {
                var iv = conversion.Convert(vertex);
                box = box.Extend(iv);
                vs.Add(iv);
            }

            return new I.LineString(box, vs.MoveToImmutable(), Area == 0);
        }

        public I64.LineString ToInteger(in Conversion64 conversion)
        {
            var vs = ImmutableArray.CreateBuilder<I64.Vector>(Vertices.Length);
            var box = I64.BBox.Empty;
            foreach (var vertex in Vertices)
            {
                var iv = conversion.Convert(vertex);
                box = box.Extend(iv);
                vs.Add(iv);
            }

            return new I64.LineString(box, vs.MoveToImmutable(), Area == 0);
        }

        public LineString Reverse()
        {
            var vertices = new Vector[Vertices.Length];
            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; i++, j--)
            {
                vertices[i] = Vertices[j];
            }
            return new LineString(BBox, vertices.ToImmutableArray(), IsClosed, -Area);
        }

        public LineString MakeInvalidLinearRing() => Area != 0 ? new LineString(BBox, Vertices, IsClosed, 0m) : this;

        public override string ToString()
        {
            var strings = new string[Vertices.Length];
            for (int i = 0; i < Vertices.Length; i++)
            {
                strings[i] = Vertices[i].ToString();
            }
            return '(' + string.Join(",", strings) + ')';
        }

        public IEnumerator<Vector> GetEnumerator() => ((IEnumerable<Vector>)Vertices).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string ToWktString() => WKTNames.LineString + ToString();

        public static bool TryParse(in string input, out LineString lineString, in bool isLinearRing = false)
        {
            var si = input.IndexOf('(') + 1;
            var ei = input.LastIndexOf(')');
            if (si > 0 && (ei - si) > 6)
            {
                var split = input[si..ei].Split(new[] { ',' });
                if (split.Length > 1)
                {
                    var vertices = ImmutableArray.CreateBuilder<Vector>(split.Length);
                    foreach (var data in split)
                    {
                        if (!Vector.TryParse(data, out var vector))
                        {
                            lineString = default;
                            return false;
                        }
                        vertices.Add(vector);
                    }
                    lineString = new LineString(vertices, isLinearRing);
                    return true;
                }
            }
            lineString = default;
            return false;
        }

        public static bool TryParseWkt(in string input, out LineString lineString, in bool isLinearRing = false)
        {
            lineString = default;
            var wi = input.IndexOf(WKTNames.LineString, StringComparison.InvariantCultureIgnoreCase);
            return wi >= 0 && TryParse(input[(wi + WKTNames.LineString.Length)..], out lineString, isLinearRing);
        }
    }
}
