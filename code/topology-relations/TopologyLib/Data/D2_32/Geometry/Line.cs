using System;
using System.Collections.Immutable;

using TopologyLib.Int32.D2;

namespace TopologyLib.Data.D2_32.Geometry
{
    public readonly struct Line : IGeometry
    {

        public ImmutableHashSet<Vertex> Interior0 { get; }
        public ImmutableHashSet<HalfEdge> Interior1 { get; }

        public ImmutableHashSet<Vertex>? Boundary0 { get; }

        public ImmutableHashSet<Vertex> InteriorBoundary0 { get; }

        internal Line(in ImmutableHashSet<HalfEdge> interior1, in ImmutableHashSet<Vertex> interior0, in ImmutableHashSet<Vertex>? boundary0 = null)
        {

            Interior1 = interior1;
            Interior0 = interior0;
            Boundary0 = boundary0;
            InteriorBoundary0 = boundary0 is null ? interior0 : interior0.Union(boundary0);
        }


        public Line(ImmutableHashSet<HalfEdge> interior1)
        {
            var interior1b = interior1.ToBuilder();
            foreach (var he in interior1)
                _ = interior1b.Add(he.Twin);

            var interior0 = ImmutableHashSet.CreateBuilder<Vertex>();
            var interiorBoundary0 = ImmutableHashSet.CreateBuilder<Vertex>();
            foreach (var he in interior1b)
                if (interiorBoundary0.Add(he.Orig) && interior1b.Overlaps(he.StarCcwWithOut()))
                    _ = interior0.Add(he.Orig);

            Interior1 = interior1b.ToImmutable();
            Interior0 = interior0.ToImmutable();
            InteriorBoundary0 = interiorBoundary0.ToImmutable();
            Boundary0 = Interior0.Count == InteriorBoundary0.Count ? null : InteriorBoundary0.Except(Interior0);
        }


        public IMValue GetII(in Line l) =>
            Interior1.Overlaps(l.Interior1)
            ? IMValue.Dim1
            : Interior0.Overlaps(l.Interior0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetII(in Region a) =>
            Interior1.Overlaps(a.Interior1)
            ? IMValue.Dim1
            : Interior0.Overlaps(a.Interior0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetIB(in Line l) =>
            l.Boundary0 != null && Interior0.Overlaps(l.Boundary0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetIB(in Region a) =>
            Interior1.Overlaps(a.Boundary1) ? IMValue.Dim1
            : Interior0.Overlaps(a.Boundary0) ? IMValue.Dim0 : IMValue.False;

        public IMValue GetIE(in Point _) => IMValue.Dim1;

        public IMValue GetIE(in Line l) =>
            Interior1.IsSubsetOf(l.Interior1)
            ? IMValue.False : IMValue.Dim1;

        public IMValue GetIE(in Region a) =>
            Interior1.IsSubsetOf(a.InteriorBoundary1)
            ? IMValue.False : IMValue.Dim1;

        public IMValue GetBB(in Line l) =>
            Boundary0 != null && l.Boundary0 != null && Boundary0.Overlaps(l.Boundary0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetBB(in Region a) =>
            Boundary0 != null && Boundary0.Overlaps(a.Boundary0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetBE(in Point p) =>
            Boundary0 is null || (Boundary0.Count == 1 && Boundary0.Contains(p.Interior0)) ? IMValue.False : IMValue.Dim0;

        public IMValue GetBE(in Line l) =>
            Boundary0 is null
            || Boundary0.IsSubsetOf(l.InteriorBoundary0)
            ? IMValue.False : IMValue.Dim0;

        public IMValue GetBE(in Region a) =>
            Boundary0 is null || Boundary0.IsSubsetOf(a.InteriorBoundary0)
            ? IMValue.False : IMValue.Dim0;

    }
}
