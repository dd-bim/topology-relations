using System;
using System.Collections.Immutable;

using TopologyLib.Int32.D2;

namespace TopologyLib.Data.D2_32.Geometry
{
    public readonly struct Region : IGeometry
    {
        public ImmutableHashSet<Vertex> Interior0 { get; }
        public ImmutableHashSet<HalfEdge> Interior1 { get; }
        public ImmutableHashSet<Face> Interior2 { get; }

        public ImmutableHashSet<Vertex> Boundary0 { get; }
        public ImmutableHashSet<HalfEdge> Boundary1 { get; }

        public ImmutableHashSet<Vertex> InteriorBoundary0 { get; }
        public ImmutableHashSet<HalfEdge> InteriorBoundary1 { get; }

        internal Region(in ImmutableHashSet<Face> interior2, in ImmutableHashSet<HalfEdge> interior1, in ImmutableHashSet<Vertex> interior0, in ImmutableHashSet<HalfEdge> boundary1, in ImmutableHashSet<Vertex> boundary0)
        {
            Interior2 = interior2;
            Interior1 = interior1;
            Interior0 = interior0;
            Boundary1 = boundary1;
            Boundary0 = boundary0;
            InteriorBoundary1 = interior1.Union(boundary1);
            InteriorBoundary0 = interior0.Union(boundary0);
        }

        public Region(in ImmutableHashSet<Face> interior2)
        {
            Interior2 = interior2;

            var interior0 = ImmutableHashSet.CreateBuilder<Vertex>();
            var interior1 = ImmutableHashSet.CreateBuilder<HalfEdge>();
            var boundary0 = ImmutableHashSet.CreateBuilder<Vertex>();
            var boundary1 = ImmutableHashSet.CreateBuilder<HalfEdge>();

            foreach (var face in interior2)
            {
                foreach (var he in face.Boundary())
                {
                    if(he.Twin.RefFace is null || !interior2.Contains(he.Twin.RefFace))
                    {
                        _ = boundary1.Add(he);
                        _ = boundary1.Add(he.Twin);
                        _ = boundary0.Add(he.Orig);
                    }
                    else
                    {
                        _ = interior1.Add(he);
                        _ = interior1.Add(he.Twin);
                        _ = interior0.Add(he.Orig);
                        _ = interior0.Add(he.Dest);
                    }
                }
            }

            interior0.ExceptWith(boundary0);
            Interior1 = interior1.ToImmutable();
            Interior0 = interior0.ToImmutable();
            Boundary1 = boundary1.ToImmutable();
            Boundary0 = boundary0.ToImmutable();
            InteriorBoundary1 = Interior1.Union(Boundary1);
            InteriorBoundary0 = Interior0.Union(Boundary0);
        }

        public bool Union(in Region other, out Region unifiedArea)
        {
            if (!IntersectsMinDimension1(other))
            {
                unifiedArea = default;
                return false;
            }
            var interior2 = Interior2.Union(other.Interior2);
            var interior1 = Interior1.Union(other.Interior1).ToBuilder();
            var interior0 = Interior0.Union(other.Interior0).ToBuilder();
            foreach (var he in Boundary1.Intersect(other.Boundary1))
            {
                if (he.RefFace != null && he.Twin.RefFace != null && interior2.Contains(he.RefFace) && interior2.Contains(he.Twin.RefFace))
                {
                    interior1.Add(he);
                    interior0.Add(he.Orig);
                }
            }
            var boundary1 = Boundary1.Union(other.Boundary1).Except(interior1);
            
            if (boundary1.Count < 6 || interior2.IsEmpty || (boundary1.Count % 2) == 1)
            {
                unifiedArea = default;
                return false;
            }

            var boundary0 = ImmutableHashSet.CreateBuilder<Vertex>();
            foreach (var he in boundary1)
                boundary0.Add(he.Orig);
            interior0.ExceptWith(boundary0);

            unifiedArea = new Region(interior2, interior1.ToImmutable(), interior0.ToImmutable(), boundary1, boundary0.ToImmutable());
            return true;
        }

        // II >= 1 || IB == 1 || BI == 1 || BB == 1
        public bool IntersectsMinDimension1(in Region a) =>
            Interior2.Overlaps(a.Interior2)
            || Interior1.Overlaps(a.Interior1) 
            || Interior1.Overlaps(a.Boundary1) 
            || Boundary1.Overlaps(a.Interior1) 
            || Boundary1.Overlaps(a.Boundary1);


        public IMValue GetII(in Point p) => Interior0.Contains(p.Interior0) ? IMValue.Dim0 : IMValue.False;

        public IMValue GetII(in Line l) => Interior1.Overlaps(l.Interior1) ? IMValue.Dim1
                    : Interior0.Overlaps(l.Interior0) ? IMValue.Dim0 : IMValue.False;

        public IMValue GetII(in Region a) => Interior2.Overlaps(a.Interior2) ? IMValue.Dim2
                    : Interior1.Overlaps(a.Interior1) ? IMValue.Dim1
                    : Interior0.Overlaps(a.Interior0) ? IMValue.Dim0 : IMValue.False;

        public IMValue GetIB(in Line l) => l.Boundary0 != null && Interior0.Overlaps(l.Boundary0) ? IMValue.Dim0 : IMValue.False;

        public IMValue GetIB(in Region a) => Interior1.Overlaps(a.Boundary1) ? IMValue.Dim1
                    : Interior0.Overlaps(a.Boundary0) ? IMValue.Dim0 : IMValue.False;


        public IMValue GetIE(in Point _) => IMValue.Dim2;

        public IMValue GetIE(in Line _) => IMValue.Dim2;

        public IMValue GetIE(in Region a) =>
            Interior2.IsSubsetOf(a.Interior2)
            ? IMValue.False : IMValue.Dim2;

        public IMValue GetBB(in Region a) => 
            Boundary1.Overlaps(a.Boundary1) ? IMValue.Dim1
                    : Boundary0.Overlaps(a.Boundary0) ? IMValue.Dim0 : IMValue.False;

        public IMValue GetBE(in Point p) => IMValue.Dim1;

        public IMValue GetBE(in Line l) => Boundary1.IsSubsetOf(l.Interior1) ? IMValue.False : IMValue.Dim1;

        public IMValue GetBE(in Region a) => 
            Boundary1.IsSubsetOf(a.InteriorBoundary1) 
            ? IMValue.False : IMValue.Dim1;


    }
}
