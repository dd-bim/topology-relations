using GeometryLib;
using Decimal = GeometryLib.Decimal.D2;
using GeometryLib.Int64;
using GeometryLib.Int64.D2;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

using System.Globalization;

using TopologyLib.Data.D2.Geometry;

namespace TopologyLib.Int64.D2
{
    public class Mesh
    {
        private Face? _faces;
        private HalfEdge? _halfEdges;

        public BBox BBox { get; }

        public Dictionary<IIntegerPoint, Vertex> PointVertices { get; }

        public Dictionary<Vertex, ImmutableHashSet<int>> VertexIds { get; }

        public Dictionary<HalfEdge, ImmutableHashSet<int>> HalfEdgeIds { get; }

        public Dictionary<Face, ImmutableHashSet<int>> FaceIds { get; }

        public Vertex? Vertices { get; internal set; }

        public HalfEdge? HalfEdges
        {
            get => _halfEdges;
            internal set
            {
                _halfEdges = value;
                _faces = null;
                Vertices = value?.Orig ?? null;
            }
        }

        public Face? Faces
        {
            get => _faces;
            internal set
            {
                _faces = value;
                if (value != null)
                {
                    _halfEdges = value.RefEdge;
                    Vertices = _halfEdges.Orig;
                }
                else
                {
                    _halfEdges = null;
                    Vertices = null;
                }
            }
        }

        private Mesh(in BBox box, in Dictionary<IIntegerPoint, Vertex> pointVertices,
            in Face? faces, in HalfEdge? halfEdges, in Vertex? vertices)
        {
            PointVertices = pointVertices;
            BBox = box;
            VertexIds = new Dictionary<Vertex, ImmutableHashSet<int>>();
            HalfEdgeIds = new Dictionary<HalfEdge, ImmutableHashSet<int>>();
            FaceIds = new Dictionary<Face, ImmutableHashSet<int>>();
            if (faces != null)
            {
                _faces = faces;
                _halfEdges = faces.RefEdge;
                Vertices = _halfEdges.Orig;
            }
            else if (halfEdges != null)
            {
                _faces = null;
                _halfEdges = halfEdges;
                Vertices = _halfEdges.Orig;
            }
            else
            {
                _faces = null;
                _halfEdges = null;
                Vertices = vertices;
            }
        }

        internal bool AddGeometry(in Vector v, int id, ref Dictionary<int, IGeometry> geometries)
        {
            geometries.Remove(id);
            if (!PointVertices.TryGetValue(v, out var vtx))
                return false;
            geometries[id] = new Point(vtx);
            if (VertexIds.TryGetValue(vtx, out var ids))
                ids = ids.Add(id);
            else
                ids = ImmutableHashSet.Create(id);
            VertexIds[vtx] = ids;
            return true;
        }

        internal bool AddGeometry(in LineString lineString, in int id, ref Dictionary<int, IGeometry> geometries)
        {
            geometries.Remove(id);

            if (!GetLineStringHalfEdges(lineString, out var edges))
                return false;

            var line = new Line(edges.ToImmutable());
            // Daten übernehmen
            foreach (var he in line.Interior1)
            {
                HalfEdgeIds[he] = HalfEdgeIds.TryGetValue(he, out var ids)
                    ? ids.Add(id) : ImmutableHashSet.Create(id);
            }
            geometries.Add(id, line);

            return true;
        }

        internal bool AddGeometry(in Polygon polygon, in int id, ref Dictionary<int, IGeometry> geometries)
        {
            geometries.Remove(id);

            // Umring setzen
            if (!GetLinearRingFaces(polygon[0], out var interior2))
                return false;

            // Löcher
            for (int i = 1; i < polygon.Count; i++)
            {
                if (!GetLinearRingFaces(polygon[i], out var region)
                    || !interior2!.IsProperSupersetOf(region!))
                    return false;
                interior2!.ExceptWith(region!);
            }

            var area = new Region(interior2!.ToImmutable());

            // Daten übernehmen
            geometries.Add(id, area);
            foreach (var face in area.Interior2)
            {
                FaceIds[face] = FaceIds.TryGetValue(face, out var ids)
                    ? ids.Add(id) : ImmutableHashSet.Create(id);
            }
            return true;
        }

        internal bool AddGeometry(in MultiLineString multiLineString, in int id, ref Dictionary<int, IGeometry> geometries)
        {
            geometries.Remove(id);

            var interior1 = ImmutableHashSet.CreateBuilder<HalfEdge>();

            foreach (var lineString in multiLineString)
            {
                if (!GetLineStringHalfEdges(lineString, out var edges))
                    return false;
                interior1.UnionWith(edges);
            }

            var line = new Line(interior1.ToImmutable());

            foreach (var he in line.Interior1)
            {
                HalfEdgeIds[he] = HalfEdgeIds.TryGetValue(he, out var ids)
                    ? ids.Add(id) : ImmutableHashSet.Create(id);
            }
            geometries.Add(id, line);

            return true;
        }

        internal static ImmutableHashSet<HalfEdge> GetBoundaryOfFaces(in ImmutableHashSet<Face>.Builder faces)
        {
            var bound = ImmutableHashSet.CreateBuilder<HalfEdge>();
            foreach (var face in faces)
            {
                foreach (var he in face.Boundary())
                    if (he.Twin.RefFace is null || !faces.Contains(he.Twin.RefFace))
                        _ = bound.Add(he.Twin);

            }
            return bound.ToImmutable();
        }

        internal static bool GetFacesInside(in ImmutableHashSet<HalfEdge> boundary,
            out ImmutableHashSet<Face>.Builder faces)
        {
            faces = ImmutableHashSet.CreateBuilder<Face>();
            if (boundary.Count < 3)
                return false;

            foreach (var he in boundary)
            {
                if (he.Twin.RefFace is null)
                    return false;

                _ = faces.Add(he.Twin.RefFace);
            }
            var queue = ImmutableQueue.CreateRange(faces);
            faces.Clear();
            while (!queue.IsEmpty)
            {
                queue = queue.Dequeue(out var face);
                if (!faces.Add(face))
                    continue;
                var neighs = ImmutableHashSet.CreateBuilder<Face>();
                foreach (var he in face.Boundary())
                {
                    if (he.Twin.RefFace is null || boundary.Contains(he.Twin))
                        continue;
                    if (boundary.Contains(he))
                        return false;
                    if (!faces.Contains(he.Twin.RefFace) && neighs.Add(he.Twin.RefFace))
                        queue = queue.Enqueue(he.Twin.RefFace);
                }
            }
            return true;
        }

        internal bool GetLineStringHalfEdges(in LineString lineString, out ImmutableHashSet<HalfEdge>.Builder edges)
        {
            edges = ImmutableHashSet.CreateBuilder<HalfEdge>();
            if (!PointVertices.TryGetValue(lineString[0], out var vtx))
                return false;

            // alle Kanten suchen
            for (int i = 1; i < lineString.Count; i++)
            {
                if (!vtx.CollinearEdgesTo(lineString[i], out var edgeHalfEdges))
                    return false;

                edges.UnionWith(edgeHalfEdges);
                vtx = edgeHalfEdges[^1].Dest;
            }
            return true;
        }

        internal bool GetLinearRingFaces(in LineString lineString, out ImmutableHashSet<Face>.Builder? region)
        {
            region = null;
            if (!lineString.IsLinearRing || !PointVertices.TryGetValue(lineString[0], out var vtx))
                return false;

            // alle Kanten suchen
            var edges = ImmutableHashSet.CreateBuilder<HalfEdge>();
            var twins = ImmutableHashSet.CreateBuilder<HalfEdge>();
            var vertices = ImmutableHashSet.CreateBuilder<Vertex>();
            for (int i = 1; i < lineString.Count; i++)
            {
                if (!vtx.CollinearEdgesTo(lineString[i], out var edgeHalfEdges))
                    return false;

                foreach (var he in edgeHalfEdges)
                {
                    if (!edges.Add(he) || !twins.Add(he.Twin) || !vertices.Add(he.Orig))
                        return false;
                }

                vtx = edgeHalfEdges[^1].Dest;
            }

            return GetFacesInside((lineString.Area2 > 0 ? twins : edges).ToImmutable(), out region);
        }

        //internal void CombineFacesInBoundary(in ImmutableHashSet<HalfEdge> boundary,
        //    ref ImmutableHashSet<Face>.Builder faces)
        //{
        //    var remain = boundary.ToBuilder();
        //    foreach (var he in boundary)
        //    {
        //        if (!remain.Contains(he))
        //            continue;
        //        // Zusammenhängenden Linienzug suchen
        //        var cur = he;
        //        var regionBoundary = ImmutableArray.CreateBuilder<HalfEdge>();
        //        do
        //        {
        //            foreach (var he2 in cur.Twin.StarCcw())
        //            {
        //                if (!remain.Remove(he2))
        //                    continue;
        //                regionBoundary.Add(he2);
        //                cur = he2;
        //                break;
        //            }
        //        } while (cur != he);
        //        if (!GetFacesInside(regionBoundary.ToImmutableHashSet(), out var region)
        //            || !faces.IsSupersetOf(region) || region.Count < 2)
        //            continue;

        //        // zu entfernende Punkte
        //        var pointsToRemove = ImmutableHashSet.CreateBuilder<Vertex>();
        //        foreach (var f in region)
        //            foreach (var he2 in f.Boundary())
        //                _ = pointsToRemove.Add(he2.Orig);

        //        // Face bilden
        //        var prev = regionBoundary[0].Twin;
        //        var face = prev.RefFace!;
        //        _ = pointsToRemove.Remove(cur.Orig);
        //        for (int i = regionBoundary.Count - 1; i >= 0; i--)
        //        {
        //            cur = regionBoundary[i].Twin;
        //            prev.MakeNext(cur);
        //            face.RefEdge = cur;
        //            cur.RefFace = face;
        //            prev = cur;
        //            _ = pointsToRemove.Remove(cur.Orig);
        //        }
        //        foreach (var v in pointsToRemove)
        //            PointVertices.Remove(v.Point);

        //        region.Remove(face);
        //        if (region.Contains(Faces!))
        //            Faces = face;
        //        faces.ExceptWith(region);
        //    }
        //}

        //internal bool RegionBoundaryToFace(in ImmutableArray<HalfEdge> regionBoundary, ref ImmutableHashSet<Face>.Builder faces)
        //{
        //    if (!GetFacesInside(regionBoundary.ToImmutableHashSet(), out var region)
        //        || !faces.IsSupersetOf(region) || region.Count < 2)
        //        return false;

        //    // zu entfernende Punkte
        //    var pointsToRemove = ImmutableHashSet.CreateBuilder<Vertex>();
        //    foreach (var f in region)
        //        foreach (var he2 in f.Boundary())
        //            _ = pointsToRemove.Add(he2.Orig);

        //    // Face bilden
        //    var prev = regionBoundary[0].Twin;
        //    var face = prev.RefFace!;
        //    _ = pointsToRemove.Remove(cur.Orig);
        //    for (int i = regionBoundary.Count - 1; i >= 0; i--)
        //    {
        //        cur = regionBoundary[i].Twin;
        //        prev.MakeNext(cur);
        //        face.RefEdge = cur;
        //        cur.RefFace = face;
        //        prev = cur;
        //        _ = pointsToRemove.Remove(cur.Orig);
        //    }
        //    foreach (var v in pointsToRemove)
        //        PointVertices.Remove(v.Point);

        //    region.Remove(face);
        //    if (region.Contains(Faces!))
        //        Faces = face;
        //    faces.ExceptWith(region);
        //} 

        public static Mesh CreateTriangulation(in IReadOnlyCollection<Vector> points)
        {
            // einfacher Sweepline Algorithmus zum Erzeugen einer Delaunay Triangulation
            // funktioniert (theoretisch) immer, da im Notfall Punkte hinzugefügt werden

            // Punkte sortieren (von minx -> maxx (miny -> maxy)) und Duplikate entfernen
            var sorted = points.ToImmutableSortedSet().ToImmutableArray();

            // Spezialfälle
            switch (sorted.Length)
            {
                case 0:
                    return new Mesh(new BBox(Vector.Zero), new Dictionary<IIntegerPoint, Vertex>(), null, null, null);
                case 1:
                    var v0 = new Vertex(sorted[0]);
                    return new Mesh(new BBox(sorted[0]),
                        new Dictionary<IIntegerPoint, Vertex> { { sorted[0], v0 } },
                        null, null, v0);
                case 2:
                    v0 = new Vertex(sorted[0]);
                    var v1 = new Vertex(sorted[1]);
                    var h = new HalfEdge(v0) { Twin = new HalfEdge(v1) };
                    h.Twin.Twin = h;
                    h.MakeNext(h.Twin);
                    h.Twin.MakeNext(h);
                    return new Mesh(new BBox(sorted[0]).Extend(sorted[1]),
                        new Dictionary<IIntegerPoint, Vertex> { { sorted[0], v0 }, { sorted[1], v1 } },
                        null, h, v0);

            }

            // Startdreieck definieren
            var triangle = Face.CreateTriangle(Decimal.Conversion64.MinXMaxY, Decimal.Conversion64.MinXMinY, sorted[0], out var vtx1, out var vtx2, out var vtx);
#if false
            var ab = triangle.RefEdge;
            var bc = ab.Next;
            var ca = bc.Next;
            var a = ab.Orig;
            var b = bc.Orig;
            var c = ca.Orig;
            if (((Vector)a.Point).Det((Vector)b.Point,(Vector)c.Point) <= 0
                || ab.RefFace != triangle
                || bc.RefFace != triangle
                || ca.RefFace != triangle
                || ca != ab.Prev || bc != ca.Prev || ab != bc.Prev
                || ab.Dest != b || bc.Dest != c || ca.Dest != a
                || a.refEdge != ab || b.refEdge != bc || c.refEdge != ca
                || !triangle.IsValid())
                Console.WriteLine("Fehler");
#endif
            var first = triangle.RefEdge.Twin;
            var pointVertices = new Dictionary<IIntegerPoint, Vertex>(sorted.Length) { { sorted[0], vtx } };
            var miny = sorted[0].y;
            var maxy = miny;

            var toFlip = new HashSet<HalfEdge>();
            // Hilfsfunktionen um evtl. Dreieck zu bilden
            bool NewTriangleBetweenNext(ref HalfEdge edge)
            {
                if (edge.VectorOrig.Det(edge.VectorDest, edge.Next.VectorDest) > 0)
                {
                    triangle = Face.CreateTriangle(edge);
                    _ = toFlip.Add(edge);
                    _ = toFlip.Add(edge.Next);
                    edge = edge.Prev.Twin; // obere Kante
                    return true;
                }
                return false;
            }
            void makeDelaunay()
            {
                // lokale Delaunay mit neuen Dreiecken
                while (toFlip.Count > 0)
                {
                    var flip = new List<HalfEdge>(toFlip);
                    toFlip.Clear();
                    foreach (var e in flip)
                        if (!toFlip.Contains(e))
                            e.FlipEdge(ref toFlip);
                }
            }

            HalfEdge curEdge;
            // Punkte nach und nach einfügen
            for (int vi = 1, sign = -1; vi < sorted.Length; vi++)
            {
                var v = sorted[vi];

                if (v.y < miny)
                    miny = v.y;
                else if (v.y > maxy)
                    maxy = v.y;


                toFlip.Clear();

                bool NewTriangleWithV(ref HalfEdge edge, bool isChecked = true)
                {
                    if (isChecked || v.Det(edge.VectorOrig, edge.VectorDest) > 0)
                    {
                        triangle = Face.CreateTriangle(edge, v, out vtx);
#if false
                        var ab = triangle.RefEdge;
                        var bc = ab.Next;
                        var ca = bc.Next;
                        var a = ab.Orig;
                        var b = bc.Orig;
                        var c = ca.Orig;
                        if (((Vector)a.Point).Det((Vector)b.Point, (Vector)c.Point) <= 0
                            || ab.RefFace != triangle
                            || bc.RefFace != triangle
                            || ca.RefFace != triangle
                            || ca != ab.Prev || bc != ca.Prev || ab != bc.Prev
                            || ab.Dest != b || bc.Dest != c || ca.Dest != a
                            || a.refEdge != ab || b.refEdge != bc || c.refEdge != ca
                || !triangle.IsValid())
                            Console.WriteLine("Fehler");
#endif
                        _ = toFlip.Add(edge);
                        pointVertices.Add(v, vtx);
                        edge = edge.Prev.Twin; // obere Kante
                        return true;
                    }
                    return false;
                }

                // gegenüberliegende Kante suchen
                curEdge = first;
                do
                {
                    curEdge = curEdge.Prev;
                    var y = ((Vector)curEdge.Orig.Point).y;
                    sign = v.y.CompareTo(y);
                } while (sign == 1);

                if (sign == 0)
                {
                    if (!NewTriangleWithV(ref curEdge, false))
                    {
                        curEdge = curEdge.Prev;
                        if (!NewTriangleWithV(ref curEdge, false))
                        {
                            curEdge = curEdge.Next.Next;
                            _ = !NewTriangleWithV(ref curEdge, false);
                        }
                    }
                }
                else
                {
                    _ = !NewTriangleWithV(ref curEdge);
                }

                // prüfen ob mehr Dreiecke möglich (erst nach unten)
                curEdge = curEdge.Next;
                while (NewTriangleBetweenNext(ref curEdge)) { }

                // nach oben
                curEdge = curEdge.Prev.Prev;
                while (NewTriangleBetweenNext(ref curEdge))
                    curEdge = curEdge.Prev;

                makeDelaunay();
#if false
                if (!triangle.IsValid())
                    Console.WriteLine("Fehler");
#endif
            }

            // Faces der Startpunkte entfernen
            foreach (var v in new[] { vtx1, vtx2 })
                foreach (var he in v.RefEdge.StarCcw())
                {
                    he.Twin.Prev.MakeNext(he.Next);
                    he.Next.RefFace = null;
                    he.Twin.Prev.RefFace = null;
                }

            // Umring konvex machen
            vtx1 = pointVertices[sorted[0]];
            first = vtx1.RefEdge.RefFace is null ? vtx1.RefEdge : vtx1.RefEdge.Twin.Next;
            curEdge = first.Prev;
            toFlip.Clear();
            bool change = true;
            while (change)
            {
                change = false;
                curEdge = curEdge.Next;
                while (curEdge.Dest != vtx1)
                {
                    while (NewTriangleBetweenNext(ref curEdge)) { change = true; }
                    curEdge = curEdge.Next;
                }
            }
            makeDelaunay();

            var box = new BBox(new Vector(sorted[0].x, miny), new Vector(sorted[^1].x, maxy));
            return new Mesh(box,
                 pointVertices, vtx1.RefEdge.RefFace is null ? vtx1.RefEdge.Twin.RefFace : vtx1.RefEdge.RefFace, vtx1.RefEdge, vtx1);
        }

        public bool IsValid()
        {
            if (Vertices is null)
            {
                return false;
            }
            HalfEdge? bound = null;
            int boundCount = 0;
            int faceCount = 0;
            int hedgeCount = 0;

            if (Faces != null)
            {
                var allFaces = ImmutableArray.CreateRange(Faces.All());

                for (int i = 0; i < allFaces.Length; i++)
                {
                    Face? face = allFaces[i];
                    faceCount++;
                    var boundary = ImmutableArray.CreateRange(face.Boundary());
                    foreach (var he in boundary)
                    {
                        hedgeCount++;
                        if (he.Twin.RefFace is null)
                        {
                            hedgeCount++;
                            boundCount++;
                            bound = he.Twin;
                        }
                        if (he.Twin.Twin != he
                            || he.RefFace != face
                            || he.Twin.RefFace == face)
                        //      || (!(he.Dest.Point is Vector) && he.Collinear is null))
                        {
                            return false;
                        }
                        for (int j = i + 1; j < allFaces.Length; j++)
                        {
                            if (ImmutableHashSet.CreateRange(allFaces[j].Boundary()).SetEquals(boundary))
                                return false;
                        }
                    }
                    if (boundary.Length == 3)
                    {
                        if (boundary[0].VectorOrig.Det(boundary[1].VectorOrig, boundary[2].VectorOrig) <= 0)
                            return false;
                    }
                    //else 
                    //    return false;
                }
                if (bound is null)
                {
                    return false;
                }
                var first = bound;
                int boundCount2 = 0;
                do
                {
                    bound = bound.Next;
                    boundCount2++;
                } while (bound != first);
                if (boundCount != boundCount2)
                {
                    return false;
                }
            }

            if (HalfEdges != null)
            {
                var halfEdges = new HashSet<HalfEdge>(HalfEdges.All());
                if (Faces != null && hedgeCount != halfEdges.Count)
                {
                    return false;
                }
                hedgeCount = halfEdges.Count;
                foreach (var he in halfEdges)
                {
                    foreach (var he2 in he.StarCcw())
                    {
                        if (he2.Orig != he.Orig || he2.Next.Orig != he2.Dest || he2.Orig == he2.Dest)
                        {
                            return false;
                        }
                    }
                    foreach (var he2 in halfEdges)
                    {
                        if (he.Equals(he2)) continue;
                        if (he.SameEnds(he2))
                            return false;
                    }
                }
            }


            int vertexCount = 0;
            foreach (var vtx in Vertices.All())
            {
                vertexCount++;
                if (vtx.RefEdge.Orig != vtx)
                {
                    return false;
                }
            }


            //int edgeCount = halfEdges.Count;
            //if (edgeCount * 2 != hedgeCount)
            //{
            //    return false;
            //}
            if (vertexCount - hedgeCount / 2 + faceCount != 1)
            {
                return false;
            }

            return true;
        }

    }
}
