using GeometryLib;
using GeometryLib.Decimal.D2;
using GeometryLib.Int32;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Xml;

using TopologyLib.Data.D2_32.Geometry;
using TopologyLib.Int32.D2;


using I = GeometryLib.Int32.D2;
using D = GeometryLib.Decimal.D2;
using GeometryLib.Interfaces;

namespace TopologyLib.Data.D2_32
{
    public class WktMesh
    {
        public Mesh Mesh { get; }

        public Conversion Conversion { get; }

        public ImmutableDictionary<int, IGeometry> MeshGeometries { get; }

        private ImmutableArray<IGeometryOgc2<decimal>?> Geometries { get; }

        public bool IsValidGeometry(int index) => MeshGeometries.ContainsKey(index);

        private WktMesh(Mesh mesh, Conversion conversion, ImmutableDictionary<int, IGeometry> meshGeometries, ImmutableArray<IGeometryOgc2<decimal>?> geometries)
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

    }
}
