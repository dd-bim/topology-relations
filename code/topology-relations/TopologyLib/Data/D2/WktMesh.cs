using GeometryLib;
using GeometryLib.Decimal.D2;
using GeometryLib.Int64;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Xml;

using TopologyLib.Data.D2.Geometry;
using TopologyLib.Int64.D2;


using I = GeometryLib.Int64.D2;
using D = GeometryLib.Decimal.D2;
using GeometryLib.Interfaces;

namespace TopologyLib.Data.D2
{
    public class WktMesh
    {
        public Mesh Mesh { get; }

        public Conversion64 Conversion { get; }

        public ImmutableDictionary<int, IGeometry> MeshGeometries { get; }

        private ImmutableArray<IGeometryOgc2<decimal>?> Geometries { get; }

        public bool IsValidGeometry(int index) => MeshGeometries.ContainsKey(index);

        private WktMesh(Mesh mesh, Conversion64 conversion, ImmutableDictionary<int, IGeometry> meshGeometries, ImmutableArray<IGeometryOgc2<decimal>?> geometries)
        {
            Mesh = mesh;
            Conversion = conversion;
            MeshGeometries = meshGeometries;
            Geometries = geometries;
        }

        public static bool Create(in IReadOnlyList<string> wktGeometries, out bool approx, out WktMesh? wktMesh)
        {
            var geom = ImmutableArray.CreateBuilder<IGeometryOgc2<decimal>?>(wktGeometries.Count);
            foreach (string wktGeometry in wktGeometries)
            {
                if (Vector.TryParseWkt(wktGeometry, out var vector))
                    geom.Add(vector);
                else if (LineString.TryParseWkt(wktGeometry, out var lineString))
                    geom.Add(lineString);
                else if (Polygon.TryParseWkt(wktGeometry, out var polygon))
                    geom.Add(polygon);
                else if (MultiLineString.TryParseWkt(wktGeometry, out var multi))
                    geom.Add(multi);
                else
                    geom.Add(null);
            }
            return Create(geom, out approx, out wktMesh);
        }

        public static bool Create(in IReadOnlyList<IGeometryOgc2<decimal>?> geometries, out bool approx, out WktMesh? wktMesh)
        {
            if (MeshBuilder.CreateFromGeometryOgc(geometries, out var mesh, out var conversion, out var meshGeometries))
            {
                approx = conversion.Approx;
                wktMesh = new WktMesh(mesh!, conversion, meshGeometries, geometries.ToImmutableArray());
                return true;
            }

            approx = false;
            wktMesh = null;
            return false;
        }

        public IntersectionMatrix Relate(in int wktA, in int wktB)
        {
            return MeshGeometries.TryGetValue(wktA, out var a)
                && MeshGeometries.TryGetValue(wktB, out var b)
            ? IntersectionMatrix.Create(a, b)
            : IntersectionMatrix.InValid;
        }

        public bool Relate(in int wktA, in int wktB, in string matrix)
        {
            return Relate(wktA, wktB, matrix, out _, out _);
        }

        public bool Relate(in int wktA, in int wktB, in IntersectionMatrix matrix)
        {
            return Relate(wktA, wktB, matrix, out _);
        }

        public bool Relate(in int wktA, in int wktB, in IntersectionMatrix matrix, out IntersectionMatrix abMatrix)
        {
            if (MeshGeometries.TryGetValue(wktA, out var a) && MeshGeometries.TryGetValue(wktB, out var b))
            {
                abMatrix = IntersectionMatrix.Create(a, b);
                return matrix.Relate(abMatrix);
            }
            else
            {
                abMatrix = IntersectionMatrix.InValid;
                return false;
            }
        }

        public bool Relate(in int wktA, in int wktB, in string matrix, out IntersectionMatrix abMatrix, out IntersectionMatrix parsedMatrix)
        {
            parsedMatrix = IntersectionMatrix.InValid;
            if (MeshGeometries.TryGetValue(wktA, out var a) && MeshGeometries.TryGetValue(wktB, out var b)
                && IntersectionMatrix.TryParse(IntersectionMatrix.GetDimension(a), IntersectionMatrix.GetDimension(b), matrix, out parsedMatrix))
            {
                abMatrix = IntersectionMatrix.Create(a, b);
                return parsedMatrix.Relate(abMatrix);
            }
            else
            {
                abMatrix = IntersectionMatrix.InValid;
                return false;
            }
        }

        private static readonly string black = "black";
        private static readonly string gray = "gray";
        private static readonly string[] palette = new[] { "red", "green", "blue", "cyan", "magenta", "yellow" };
        private static readonly string[] palette2 = new[] { "darkred", "darkgreen", "darkblue", "darkcyan", "darkmagenta", "goldenrod" };


        public bool WriteSvg(in string fileName, int[]? focusGeometries = null,
            int margin = 10,
            int width = 1200,
            int height = 900,
            string vertexRadius = "4", string vertexHoverRadius = "8",
            string edgeWidth = "2", string edgeHoverWidth = "4",
            string faceBoundaryWidth = "1", string faceOpacity = "0.3")
        {

            var box = D.BBox.Empty;
            var geom = new List<int>(Geometries.Length);
            if (focusGeometries != null && focusGeometries.Length > 0)
                geom.AddRange(focusGeometries);
            else
                geom.AddRange(Enumerable.Range(0, Geometries.Length));


            for (int i = 0; i < geom.Count; i++)
            {
                if (MeshGeometries.TryGetValue(geom[i], out var geomi))
                    switch (geomi)
                    {
                        case Point p:
                            box = box.Extend(p.Interior0.Point.ToDecimal());
                            break;
                        case Line l:
                            foreach (var v in l.Boundary0 is null ? l.Interior0 : l.Interior0.Union(l.Boundary0))
                                box = box.Extend(v.Point.ToDecimal());
                            break;
                        case Region a:
                            foreach (var v in a.Boundary0)
                                box = box.Extend(v.Point.ToDecimal());
                            break;
                    }
                else
                {
                    geom.RemoveAt(i);
                    i--;
                    break;
                }
            }

            if (geom.Count < 1)
                return false;

            var min = box.Min;
            var range = box.Range;
            decimal xScale = (width - 2 * margin) / range.x;
            decimal yScale = (height - 2 * margin) / range.y;
            decimal scale = Math.Min(xScale, yScale);
            var ipoints = new Dictionary<IIntegerPoint, (string x, string y)>();

            (string x, string y) ToMap(in IIntegerPoint p)
            {
                if (ipoints.TryGetValue(p, out var xy))
                    return (xy.x, xy.y);

                var x = (margin + scale * (p.DecimalX - min.x)).ToString(CultureInfo.InvariantCulture);
                var y = (height - scale * (p.DecimalY - min.y) - margin).ToString(CultureInfo.InvariantCulture);
                ipoints[p] = (x, y);
                return (x, y);
            }

            var document = new XmlDocument();
            var root = document.CreateElement("svg");
            _ = document.AppendChild(root);
            root.SetAttribute("xmlns", "http://www.w3.org/2000/svg");
            root.SetAttribute("width", width.ToString());
            root.SetAttribute("height", height.ToString());

            var style = document.CreateElement("style");
            style.SetAttribute("type", "text/css");
            style.InnerText = $"polygon:hover {{fill-opacity:1;}} circle:hover {{r:{vertexHoverRadius};}} line:hover {{stroke-width:{edgeHoverWidth};}}";
            _ = root.AppendChild(style);

            //Punkte
            var gPoint = document.CreateElement("g");
            gPoint.SetAttribute("stroke", "none");
            gPoint.SetAttribute("fill", black);

            //Kanten
            var gEdge = document.CreateElement("g");
            gEdge.SetAttribute("stroke-width", edgeWidth);
            gEdge.SetAttribute("stroke", black);

            // Faces
            var gFace = document.CreateElement("g");
            gFace.SetAttribute("stroke", "none");
            gFace.SetAttribute("fill-opacity", faceOpacity);

            // Facekanten
            var gBoundary = document.CreateElement("g");
            gBoundary.SetAttribute("stroke-width", faceBoundaryWidth);
            gBoundary.SetAttribute("stroke", gray);

            var points = new Dictionary<Vertex, (XmlElement svg, HashSet<int> ids)>();
            var lines = new Dictionary<HalfEdge, (XmlElement svg, HashSet<int> ids)>();
            var bound = new Dictionary<HalfEdge, HashSet<int>>();
            var colors = new Dictionary<int, int>();
            var rand = new Random();
            foreach (var g in MeshGeometries)
            {
                switch (g.Value)
                {
                    case Point p:
                        if (points.TryGetValue(p.Interior0, out var gi))
                            gi.ids.Add(g.Key);
                        else
                        {
                            var (x, y) = ToMap(p.Interior0.Point);
                            var c = document.CreateElement("circle");
                            c.SetAttribute("cx", x);
                            c.SetAttribute("cy", y);
                            c.SetAttribute("r", vertexRadius);
                            _ = gPoint.AppendChild(c);
                            points[p.Interior0] = (c, new HashSet<int> { g.Key });
                        }
                        break;
                    case Line l:
                        var done = new HashSet<HalfEdge>();
                        foreach (var he in l.Interior1)
                        {
                            if (lines.TryGetValue(he, out gi) || lines.TryGetValue(he.Twin, out gi))
                                gi.ids.Add(g.Key);
                            else
                            {
                                if (done.Contains(he.Twin))
                                    continue;
                                done.Add(he);
                                var (ax, ay) = ToMap(he.Orig.Point);
                                var (bx, by) = ToMap(he.Dest.Point);

                                var e = document.CreateElement("line");
                                e.SetAttribute("x1", ax);
                                e.SetAttribute("y1", ay);
                                e.SetAttribute("x2", bx);
                                e.SetAttribute("y2", by);

                                _ = gEdge.AppendChild(e);

                                lines[he] = (e, new HashSet<int> { g.Key });
                            }
                        }
                        break;
                    case Region a:
                        done = new HashSet<HalfEdge>();
                        var neigh = new HashSet<int>();
                        foreach (var he in a.Boundary1)
                        {
                            if (!lines.ContainsKey(he) && !lines.ContainsKey(he.Twin))
                            {
                                if (done.Contains(he.Twin))
                                    continue;
                                done.Add(he);
                                if (bound.TryGetValue(he, out var ids) || bound.TryGetValue(he.Twin, out ids))
                                {
                                    foreach (var ai in ids)
                                        neigh.Add(colors[ai]);
                                    ids.Add(g.Key);
                                }
                                else
                                    bound[he] = new HashSet<int> { g.Key };
                                var (ax, ay) = ToMap(he.Orig.Point);
                                var (bx, by) = ToMap(he.Dest.Point);

                                var e = document.CreateElement("line");
                                e.SetAttribute("x1", ax);
                                e.SetAttribute("y1", ay);
                                e.SetAttribute("x2", bx);
                                e.SetAttribute("y2", by);

                                _ = gBoundary.AppendChild(e);
                            }
                        }
                        foreach (var face in a.Interior2)
                            neigh.UnionWith(Mesh.FaceIds[face]);
                        neigh.Remove(g.Key);
                        int coli;
                        if (neigh.Count > 0)
                        {
                            coli = -1;
                            for (int i = 0; coli < 0 && i < palette.Length; i++)
                            {
                                int ti = rand.Next(palette.Length);
                                if (neigh.Contains(ti))
                                    continue;
                                coli = ti;
                            }
                            if (coli < 0)
                                coli = -(rand.Next(palette2.Length) + 1);
                        }
                        else
                            coli = rand.Next(palette.Length);
                        colors[g.Key] = coli;
                        break;
                }
            }

            foreach (var item in points.Values.ToImmutableArray().AddRange(lines.Values))
            {
                var t = document.CreateElement("title");
                t.InnerText = "Ids: " + string.Join(" ,", item.ids);
                _ = item.svg.AppendChild(t);
            }

            foreach (var kv in Mesh.FaceIds)
            {
                var ppoints = new List<string>();
                foreach (var he in kv.Key.Boundary())
                {
                    var mp = ToMap(he.Orig.Point);
                    ppoints.Add($"{mp.x},{mp.y}");
                }

                XmlElement? poly = null;
                foreach (var id in kv.Value)
                {
                    poly = document.CreateElement("polygon");
                    poly.SetAttribute("points", string.Join(" ", ppoints));
                    int coli = colors[id];
                    var col = coli < 0 ? palette2[-1 - coli] : palette[coli];
                    poly.SetAttribute("fill", col);
                    _ = gFace.AppendChild(poly);
                }
                var t = document.CreateElement("title");
                t.InnerText = "Ids: " + string.Join(" ,", kv.Value);
                _ = poly!.AppendChild(t);
            }


            _ = root.AppendChild(gFace);
            _ = root.AppendChild(gBoundary);
            _ = root.AppendChild(gEdge);
            _ = root.AppendChild(gPoint);

            document.Save(fileName + ".svg");
            return true;
        }

    }
}
