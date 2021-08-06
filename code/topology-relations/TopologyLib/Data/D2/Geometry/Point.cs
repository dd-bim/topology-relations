
using TopologyLib.Int64.D2;

namespace TopologyLib.Data.D2.Geometry
{
    public readonly struct Point : IGeometry
    {
 
        public Vertex Interior0 { get; }
 
        public Point(in Vertex vertex)
        {
            Interior0 = vertex;
        }

        public IMValue GetII(in Point p) => 
            Interior0.Equals(p.Interior0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetII(in Line l) =>
            l.Interior0.Contains(Interior0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetII(in Region a) =>
            a.Interior0.Contains(Interior0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetIB(in Line l) =>
            l.Boundary0 != null && l.Boundary0.Contains(Interior0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetIB(in Region a) =>
            a.Boundary0.Contains(Interior0)
            ? IMValue.Dim0 : IMValue.False;

        public IMValue GetIE(in Point p) =>
            Interior0.Equals(p.Interior0)
            ? IMValue.False : IMValue.Dim0;

        public IMValue GetIE(in Line l) =>
            l.InteriorBoundary0.Contains(Interior0)
            ? IMValue.False : IMValue.Dim0;

        public IMValue GetIE(in Region a) =>
            a.InteriorBoundary0.Contains(Interior0)
            ? IMValue.False
            : IMValue.Dim0;

    }
}
