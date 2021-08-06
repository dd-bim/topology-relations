using GeometryLib;


using GeometryLib.Int32;
using GeometryLib.Int32.D2;

using System.Collections.Generic;
using GeometryLib.Numbers;

using System;
using System.Collections.Immutable;

using Dec2 = GeometryLib.Decimal.D2;
using Fra2 = GeometryLib.Int32.Fraction.D2;
using TopologyLib.Data.D2_32.Geometry;
using System.Linq;

using GeometryLib.Interfaces;
using GeometryLib.Interfaces.D2;


namespace TopologyLib.Int32.D2
{
    public static class MeshBuilder
    {

        public static bool CreateFromGeometryOgc(in IReadOnlyList<IGeometryOgc2<decimal>?> geometries,
            out Mesh? mesh, out Dec2.Conversion conversion,
            out ImmutableDictionary<int, IGeometry> meshGeometries)
        {
            // Punkte sammeln
            var points = new List<Dec2.Vector>();
            var box = Dec2.BBox.Empty;
            int nullCount = 0;
            foreach (var geometry in geometries)
            {
                switch (geometry)
                {
                    case Dec2.Vector v:
                        box = box.Extend(v);
                        points.Add(v);
                        break;
                    case Dec2.LineString l:
                        box = box.Combine(l.BBox);
                        for (int i = l.IsClosed ? 1 : 0; i < l.Count; i++)
                            points.Add(l[i]);
                        break;
                    case Dec2.Polygon p:
                        box = box.Combine(p.BBox);
                        foreach (var r in p)
                            for (int i = 1; i < r.Count; i++)
                                points.Add(r[i]);
                        break;
                    case Dec2.MultiLineString c:
                        box = box.Combine(c.BBox);
                        foreach (var ls in c)
                            for (int i = ls.IsClosed ? 1 : 0; i < ls.Count; i++)
                                points.Add(ls[i]);
                        break;
                    default:
                        nullCount++;
                        break;
                }

            }

            if (nullCount == geometries.Count)
            {
                mesh = null;
                meshGeometries = ImmutableDictionary<int, IGeometry>.Empty;
                conversion = default;
                return false;
            }

            conversion = new Dec2.Conversion(box);

            // Punkte in Ganzzahlen umwandeln
            var iPoints = new List<Vector>(points.Count);
            var lineStrings = new List<LineString>();
            var iGeometries = new List<IGeometryOgc2<int>?>(geometries.Count);
            foreach (var geometry in geometries)
            {
                switch (geometry)
                {
                    case Dec2.Vector v:
                        var tv = v.ToInteger(conversion);
                        iPoints.Add(tv);
                        iGeometries.Add(tv);
                        break;
                    case Dec2.LineString l:
                        var tl = l.ToInteger(conversion);
                        iPoints.AddRange(tl);
                        lineStrings.Add(tl);
                        iGeometries.Add(tl);
                        break;
                    case Dec2.Polygon p:
                        var tp = p.ToInteger(conversion);
                        iGeometries.Add(tp);
                        foreach (var r in tp)
                        {
                            iPoints.AddRange(r);
                            lineStrings.Add(r);
                        }
                        break;
                    case Dec2.MultiLineString c:
                        var tc = c.ToInteger(conversion);
                        iGeometries.Add(tc);
                        foreach (var r in tc)
                        {
                            iPoints.AddRange(r);
                            lineStrings.Add(r);
                        }
                        break;
                    default:
                        iGeometries.Add(null);
                        break;
                }

            }

            // Triangulation erzeugen
            mesh = Mesh.CreateTriangulation(iPoints);


#if false
            if (!mesh.IsValid())
            {
                Console.WriteLine();
            }
 #endif

            if (mesh.Faces != null)
            {
                var builder = new Builder(mesh);
                // Kanten der LineStrings einfügen
                foreach (var lineString in lineStrings)
                {
                    var orig = mesh.PointVertices[lineString[0]];
                    for (int i = 1; i < lineString.Count; i++)
                    {
                        var dest = mesh.PointVertices[lineString[i]];
                        builder.AddEdge(orig, dest);
                        orig = dest;
                    }
                }
                // Schnitte innerhalb der Dreiecke bilden
                builder.SplitTriangles();

                // unnütze Halbkanten bereinigen
                builder.RemovePureMeshEdges();
            }

            // Faces zuweisen
            var geom = new Dictionary<int, IGeometry>();
            var areas = new List<(int idx, Region area)>();
            for (int i = 0; i < iGeometries.Count; i++)
            {
                var (valid, isArea) = iGeometries[i] switch
                {
                    Vector v => (mesh.AddGeometry(v, i, ref geom), false),
                    LineString l => (mesh.AddGeometry(l, i, ref geom), false),
                    Polygon p => (mesh.AddGeometry(p, i, ref geom), true),
                    MultiLineString m => (mesh.AddGeometry(m, i, ref geom), false),
                    _ => (false, false)
                };
                if (!valid)
                    iGeometries[i] = null;
                else if (isArea)
                    areas.Add((i, (Region)geom[i]));
            }

            meshGeometries = geom.ToImmutableDictionary();

            return true;
        }

        private class MeshEdge : SortedList<Fraction, IIntegerPoint>
        {
            public bool IsMeshEdge { get; }

            public bool IsUsed { get; set; }

            public Vertex Orig { get; }

            public Vertex Dest { get; }

            public SortedList<Fraction, Vertex> CachedVertices;

            public Dictionary<IIntegerPoint, HalfEdge> HalfEdges;

            public MeshEdge(in Vertex orig, in Vertex dest, in bool isMeshEdge = false)
            {
                if (!(orig.Point is Vector) || !(dest.Point is Vector))
                {
                    throw new Exception();
                }
                IsMeshEdge = isMeshEdge;
                Orig = orig;
                Dest = dest;
                if (!(isMeshEdge && orig.EdgeTo(dest, out var he)))
                {
                    he = new HalfEdge(orig) { Twin = new HalfEdge(dest) };
                    he.Twin.Twin = he;
                }
                Add(Fraction.Zero, orig.Point);
                Add(Fraction.One, dest.Point);
                HalfEdges = new Dictionary<IIntegerPoint, HalfEdge> { { orig.Point, he! } };
                CachedVertices = new SortedList<Fraction, Vertex>();
                IsUsed = !isMeshEdge;
            }

            public void Split(in Fraction pos, in Vertex vertex)
            {
                Add(pos, vertex.Point);

                CachedVertices.Add(pos, vertex);

                if (!IsMeshEdge)
                {
                    int index = IndexOfValue(vertex.Point);
                    var old = HalfEdges[Values[index - 1]];
                    var he = new HalfEdge(vertex) { Twin = new HalfEdge(old.Dest) };
                    he.Twin.Twin = he;
                    old.Split(he, true);
                    HalfEdges[vertex.Point] = he;
                }
            }

            public void SplitHalfEdge()
            {
                if (!IsMeshEdge) return;
                var cur = HalfEdges[Orig.Point];
                foreach (var v in CachedVertices.Values)
                {
                    var he = new HalfEdge(v) { Twin = new HalfEdge(Dest) };
                    he.Twin.Twin = he;
                    cur.Split(he, true);
                    cur = HalfEdges[v.Point] = he;
                }

            }

            public Vertex OtherEnd(in Vertex end) => Orig == end ? Dest : Orig;

            public Vertex? AddIntersection(in Builder builder, in MeshEdge other)
            {
                var a1 = (Vector)Orig.Point;
                var b1 = (Vector)Dest.Point;
                var a2 = (Vector)other.Orig.Point;
                var b2 = (Vector)other.Dest.Point;
                if (!Vector.Intersect(a1, b1, a2, b2, out var thisPos, out var otherPos))
                    throw new Exception();

                if (!TryGetValue(thisPos, out var p))
                {
                    if (!other.TryGetValue(otherPos, out p))
                    {
                        p = Fra2.Vector.Create(a1, b1, thisPos);
                        var v = new Vertex(p);
                        Split(thisPos, v);
                        other.Split(otherPos, v);

                        builder.VertexEdges.Add(v, new HashSet<MeshEdge> { this, other });
                        builder.Mesh.PointVertices.Add(p, v);
                        return v;
                    }
                    else
                    {
                        var v = other.IsMeshEdge ? other.CachedVertices[otherPos] : other.HalfEdges[p].Orig;
                        Split(thisPos, v);

                        _ = builder.VertexEdges[v].Add(this);
                    }
                }
                else if (!other.CachedVertices.TryGetValue(otherPos, out var otherv))
                {
                    var v = IsMeshEdge ? CachedVertices[thisPos] : HalfEdges[p].Orig;
                    other.Split(otherPos, v);

                    _ = builder.VertexEdges[v].Add(other);
                }
                else if (!CachedVertices[thisPos].Equals(otherv))
                    throw new Exception();
                return null;
            }

        }

        private class Builder
        {
            public Mesh Mesh { get; }

            public Dictionary<HalfEdge, MeshEdge> HalfEdgeEdges { get; }

            public Dictionary<Vertex, HashSet<MeshEdge>> VertexEdges { get; }

            public Builder(Mesh mesh)
            {
                Mesh = mesh;
                VertexEdges = new Dictionary<Vertex, HashSet<MeshEdge>>();
                HalfEdgeEdges = new Dictionary<HalfEdge, MeshEdge>();
                foreach (var halfEdge in mesh.HalfEdges!.AllEdges())
                {
                    var me = new MeshEdge(halfEdge.Orig, halfEdge.Dest, true);
                    HalfEdgeEdges.Add(halfEdge, me);
                    AddEdgeToVertex(halfEdge.Orig, me);
                    AddEdgeToVertex(halfEdge.Dest, me);
                }
            }

            private void AddEdgeToVertex(in Vertex vertex, in MeshEdge meshEdge)
            {
                if (!VertexEdges.TryGetValue(vertex, out var edges))
                {
                    edges = new HashSet<MeshEdge>();
                    VertexEdges[vertex] = edges;
                }
                _ = edges.Add(meshEdge);
            }

            private MeshEdge EdgeOfHalfEdge(in HalfEdge halfEdge) =>
                HalfEdgeEdges.TryGetValue(halfEdge, out var edge) ? edge : HalfEdgeEdges[halfEdge.Twin];

            public void AddEdge(in Vertex orig, in Vertex dest)
            {
                var destVector = (Vector)dest.Point;
                var cur = orig;

                for (; ; )
                {
                    // Endpunkt gefunden
                    if (cur == dest)
                        return;

                    var curVector = (Vector)cur.Point;

                    // prüfen ob Teil der Kante schon vorhanden, bzw. Endpunkt erreicht
                    var curEdges = VertexEdges[cur];
                    bool found = false;
                    foreach (var curEdge in curEdges)
                    {
                        var other = curEdge.OtherEnd(cur);
                        if (dest == other)
                        {
                            curEdge.IsUsed = true;
                            return;
                        }
                        else if (((Vector)other.Point).InDirection(curVector, destVector))
                        {
                            cur = other;
                            curEdge.IsUsed = true;
                            found = true;
                            break;
                        }
                    }
                    // bereits neue Kante gefunden?
                    if (found)
                        continue;

                    // Neue MeshEdge suchen
                    var curHalfEdge = cur.RefEdge;
                    // erste Schnittkante suchen (0 kann als det nicht mehr auftauchen!)
                    long lastDet = destVector.Det(curVector, (Vector)curHalfEdge.Right.Dest.Point);
                    foreach (var halfEdge in curHalfEdge.StarCcw())
                    {
                        var curDestVector = (Vector)halfEdge.Dest.Point;
                        long det = destVector.Det(curVector, curDestVector);
                        if (lastDet > 0 && det < 0)
                        {
                            curHalfEdge = halfEdge.Twin.Left;
                            lastDet = det;
                            break;
                        }
                        lastDet = det;
                    }
                    var crossings = new List<MeshEdge> { EdgeOfHalfEdge(curHalfEdge) };

                    // weiter suchen bis Vertex
                    for (; ; )
                    {
                        var opp = curHalfEdge.Next.Dest;
                        long det = destVector.Det(curVector, (Vector)opp.Point);
                        if (det == 0)
                        {
                            // neue Kante erzeugen und zuordnen
                            var me = new MeshEdge(cur, opp);
                            _ = VertexEdges[cur].Add(me);
                            _ = VertexEdges[opp].Add(me);
                            foreach (var crossing in crossings)
                                me.AddIntersection(this, crossing);

                            cur = opp;
                            break;
                        }
                        else
                        {
                            curHalfEdge = det < 0 ? curHalfEdge.Next.Twin : curHalfEdge.Left;
                            crossings.Add(EdgeOfHalfEdge(curHalfEdge));
                        }
                    }
                }
            }

            public void SplitTriangles()
            {
#if false
                if (!Mesh.IsValid())
                {
                    Console.WriteLine("");
                }
#endif
                // Originale Dreiecke
                var triangles = ImmutableArray.CreateRange(Mesh.Faces!.All());

                // Kanten der Dreiecke an Schnittpunkten Teilen
                foreach (var me in HalfEdgeEdges.Values)
                    me.SplitHalfEdge();

#if false
                if (!Mesh.IsValid())
                {
                    Console.WriteLine("");
                }
                Mesh.WriteSvg("meshTest2");
#endif

                // Dreieck Inneres teilen
                foreach (var triangle in triangles)
                {
                    // Rand Vertices
                    var boundary = ImmutableArray.CreateRange(triangle.Boundary());
                    var boundaryEdges = new HashSet<MeshEdge>();
                    foreach (var he in boundary)
                        if (HalfEdgeEdges.TryGetValue(he, out var edge) || HalfEdgeEdges.TryGetValue(he.Twin, out edge))
                            boundaryEdges.Add(edge);

#if DEBUG
                    if (boundaryEdges.Count != 3)
                        throw new Exception();
#endif

                    // kreuzende Kanten
                    var adjacentEdges = new SortedList<int, SortedList<int, MeshEdge>>();
                    var crossingEdges = new Dictionary<MeshEdge, (int orig, int dest)>();
                    for (int i = 0; i < boundary.Length; i++)
                    {
                        var iOrig = boundary[i].Orig;
                        var iEdges = VertexEdges[iOrig];
                        if (!adjacentEdges.TryGetValue(i, out var iDests))
                            iDests = new SortedList<int, MeshEdge>();
                        for (int j = i + 1; j < boundary.Length; j++)
                        {
                            var jOrig = boundary[j].Orig;
                            var jEdges = VertexEdges[jOrig];
                            if (!adjacentEdges.TryGetValue(j, out var jDests))
                            {
                                jDests = new SortedList<int, MeshEdge>();
                                adjacentEdges[j] = jDests;
                            }
                            foreach (var edge in jEdges)
                            {
                                if (!iEdges.Contains(edge) || boundaryEdges.Contains(edge)) continue;
                                iDests[j] = edge;
                                jDests[i] = edge;
                                int iPos = edge.IndexOfValue(iOrig.Point);
                                int jPos = edge.IndexOfValue(jOrig.Point);
                                var (io, jo, ti, tj) = iPos < jPos
                                    ? (iOrig, jOrig, i, j)
                                    : (jOrig, iOrig, j, i);

                                crossingEdges.Add(edge, (ti, tj));
                                break;
                            }
                        }
                        if (iDests.Count > 0)
                            adjacentEdges[i] = iDests;
                    }

                    if (adjacentEdges.Count < 1)
                        continue;

#if false
                    if (!Mesh.IsValid())
                    {
                        Console.WriteLine("");
                    }
                    Mesh.WriteSvg("meshTest3");
#endif

                    // Halbkanten mit Boundary verknüpfen
                    foreach (var edge in adjacentEdges)
                    {
                        var next = boundary[edge.Key];
                        var prev = next.Prev;
                        // Ziele neu sortieren
                        var dests = new SortedList<int, MeshEdge>();
                        foreach (var dest in edge.Value)
                        {
                            dests.Add((dest.Key - edge.Key + boundary.Length) % boundary.Length, dest.Value);
                        }
                        foreach (var dest in dests.Values)
                        {
                            var cur = dest.HalfEdges[boundary[crossingEdges[dest].orig].Orig.Point];
                            if (cur.Orig.Equals(next.Orig))
                                cur = cur.Twin;

                            cur.Next = next;
                            next.Prev = cur;
                            next = cur.Twin;
                        }

                        next.Prev = prev;
                        prev.Next = next;
                    }

                    // Schnittpunkte rechnen
                    var vertices = new List<Vertex>();
                    foreach (var ibe in adjacentEdges)
                    {
                        int iOrig = ibe.Key;
                        foreach (var ibe2 in ibe.Value)
                        {
                            int iDest = ibe2.Key;
                            var iMe = ibe2.Value;
                            foreach (var jbe in adjacentEdges)
                            {
                                int jOrig = jbe.Key;
                                if (jOrig <= iOrig) continue;
                                if (jOrig >= iDest) break;
                                foreach (var jbe2 in jbe.Value)
                                {
                                    if (jbe2.Key > iDest)
                                    {
                                        var v = iMe.AddIntersection(this, jbe2.Value);
                                        if (v != null)
                                            vertices.Add(v);
                                    }
                                }
                            }
                        }
                    }

                    // innere Halbkanten verknüpfen
                    foreach (var vtx in vertices)
                    {
                        var edges = new SortedList<int, HalfEdge>();
                        foreach (var me in VertexEdges[vtx])
                        {
                            (int o, int d) = crossingEdges[me];
                            var he = me.HalfEdges[vtx.Point];

                            edges.Add(d, he);
                            edges.Add(o, he.Prev.Twin);
                        }
                        HalfEdge? prev = null, first = null;
                        foreach (var he in edges.Values)
                        {
                            if (prev is null)
                                first = he;
                            else
                            {
                                he.Twin.Next = prev;
                                prev.Prev = he.Twin;
                            }

                            prev = he;
                        }
                        first!.Twin.Next = prev!;
                        prev!.Prev = first!.Twin;
                    }

                    // Faces erzeugen
                    var faces = new HashSet<Face>();
                    foreach (var he in boundary)
                    {
                        if (he.RefFace != null && faces.Contains(he.RefFace))
                            continue;
                        var face = (he.RefFace != null && he.RefFace == Mesh.Faces)
                            ? Mesh.Faces = new Face(he) : new Face(he);
                        _ = faces.Add(face);
                        foreach (var the in face.Boundary())
                            the.RefFace = face;
                    }
                    foreach (var vtx in vertices)
                    {
                        foreach (var he in vtx.RefEdge.StarCcw())
                        {
                            if (he.RefFace != null && faces.Contains(he.RefFace))
                                continue;
                            var face = (he.RefFace != null && he.RefFace == Mesh.Faces)
                                ? Mesh.Faces = new Face(he) : new Face(he);
                            _ = faces.Add(face);
                            foreach (var the in face.Boundary())
                                the.RefFace = face;
                        }
                    }
#if false
                    if (!Mesh.IsValid())
                    {
                        Console.WriteLine("");
                    }
                    else
                    {
                        //Mesh.WriteSvg("meshTest3");
                    }
#endif

                }

            }

            public void RemovePureMeshEdges()
            {
                foreach (var me in HalfEdgeEdges.Values)
                {
                    if (!me.IsMeshEdge || me.IsUsed)
                        continue;
                    foreach (var he in me.HalfEdges.Values)
                    {
                        if (me.Count < 3 && (he.Next == he.Twin || he.Prev == he.Twin || he.RefFace == he.Twin.RefFace))
                            continue;

                        if (!he.DisconnectOrig() || !he.Twin.DisconnectOrig(false))
                            continue;
                        var face = he.Twin.RefFace!;
                        if (he.RefFace == Mesh.Faces)
                            Mesh.Faces = face;
                        var cur = face.RefEdge;
                        while (cur.RefFace != face)
                        {
                            cur.RefFace = face;
                            cur = cur.Next;
                        }
                        if (he.Orig != me.Orig && he.Prev.Next.RemoveCollinearOrig())
                        {
                            Mesh.PointVertices.Remove(he.Orig.Point);
                        }

#if false
                        if (!Mesh.IsValid())
                            Console.WriteLine("");
#endif
                    }
                }
            }
        }
    }
}
