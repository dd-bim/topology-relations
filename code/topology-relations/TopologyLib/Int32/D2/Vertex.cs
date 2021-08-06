using GeometryLib.Int32;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using I = GeometryLib.Int32.D2;


namespace TopologyLib.Int32.D2
{
    public class Vertex : IEquatable<Vertex?>
    {
        public IIntegerPoint Point { get; internal set; }

        public HalfEdge RefEdge { get; internal set; }

        internal Vertex(in IIntegerPoint point)
        {
            Point = point;
            RefEdge = new HalfEdge(this);
        }

        public override string ToString() => $"({Point})";

        public IEnumerable<Vertex> StarCcw()
        {
            foreach (var h in RefEdge.StarCcw())
            {
                yield return h.Dest;
            }
        }

        public IEnumerable<Vertex> StarCw()
        {
            foreach (var h in RefEdge.StarCw())
            {
                yield return h.Dest;
            }
        }

        public bool EdgeTo(Vertex dest, out HalfEdge? halfEdge)
        {
            halfEdge = null;
            foreach (var he in RefEdge.StarCcw())
            {
                if (he.Dest.Equals(dest))
                {
                    halfEdge = he;
                    return true;
                }
            }
            return false;
        }

        public bool CollinearEdgesTo(in I.Vector dest, out ImmutableArray<HalfEdge> halfEdges)
        {
            if (Point.Equals(dest))
            {
                halfEdges = ImmutableArray<HalfEdge>.Empty;
                return false;
            }

            var cur = RefEdge;
            HalfEdge? last = null;

            // evtl. schon fertig
            bool hasDest = cur.Dest.Point.Equals(dest);
            if (hasDest)
            {
                halfEdges = ImmutableArray.Create(cur);
                return true;
            }

            var halfEdgesList = ImmutableArray.CreateBuilder<HalfEdge>();
            while (!hasDest)
            {
                // erste Kante in Richtung finden
                if (!cur.CollinearEdgeTo(dest, out cur))
                {
                    halfEdges = ImmutableArray<HalfEdge>.Empty;
                    return false;
                }

                // evtl. neues Collinear setzen
                if (last != null)
                {
                    last.Collinear = cur;
                    cur.Twin.Collinear = last.Twin;
                }

                halfEdgesList.Add(cur);
                last = cur;
                hasDest = cur.Dest.Point.Equals(dest);

                // alle geraden Verlängerungen einfügen
                while (!hasDest && cur.Collinear != null)
                {
                    cur = cur.Collinear;
                    halfEdgesList.Add(cur);
                    last = cur;
                    hasDest = cur.Dest.Point.Equals(dest);
                }

                // neue Kante wählen
                cur = cur.Next;
            }

            halfEdges = halfEdgesList.ToImmutable();
            return true;
        }

        public HashSet<Vertex> All()
        {
            var vertices = new HashSet<Vertex>();
            var queue = new Queue<Vertex>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                var vtx = queue.Dequeue();
                if (vertices.Add(vtx))
                {
                    foreach (var dest in vtx.StarCcw())
                    {
                        if (!vertices.Contains(dest))
                        {
                            queue.Enqueue(dest);
                        }
                    }
                }
            }
            return vertices;
        }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Vertex);
        }

        public bool Equals(Vertex? other)
        {
            return other is not null && (ReferenceEquals(this, other) ||
                   EqualityComparer<IIntegerPoint>.Default.Equals(Point, other.Point));
        }

        public override int GetHashCode()
        {
            return Point.GetHashCode();
        }

        public static bool operator ==(Vertex left, Vertex right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vertex left, Vertex right)
        {
            return !(left == right);
        }
    }
}
