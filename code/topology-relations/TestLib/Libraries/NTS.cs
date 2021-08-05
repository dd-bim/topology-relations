using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestLib.Data;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace TestLib.Libraries
{
    class NTS : GeometryLibrary
    {
        //public string Version;

        public NTS()
        {
            GetVersion();
        }

        public override void GetConnection(string connString)
        {
            //Wird bei dieser Bibliothek nicht benötigt
        }

        public override void GetVersion()
        {
            try
            {
                Version = "NTS 2.3.0";
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public override void CheckDE9IM(List<GeometryComparison> geometryComparisons)
        {
            try
            {
                //Jedes Objektpaar soll mit seiner Mustermatrix verglichen werden
                foreach (GeometryComparison item in geometryComparisons)
                {
                    Geometry baseGeometry = WktToGeometry(item.BaseGeometry);
                    Geometry comparativeGeometry = WktToGeometry(item.ComparativeGeometry);
                    
                    foreach (PatternMatrix matrix in item.PatternMatrices)
                    {
                        bool relate = baseGeometry.Relate(comparativeGeometry, matrix.Matrix);
                        string de9imMatrix = baseGeometry.Relate(comparativeGeometry).ToString();
                        Result result = new Result(relate, Version, de9imMatrix);
                        matrix.Results.Add(result);
                    }

                    foreach (PatternMatrix matrix in item.TransposedPatternMatrices)
                    {
                        bool relate = comparativeGeometry.Relate(baseGeometry, matrix.Matrix);
                        string de9imMatrix = comparativeGeometry.Relate(baseGeometry).ToString();
                        Result result = new Result(relate, Version, de9imMatrix);
                        matrix.Results.Add(result);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private Geometry WktToGeometry(string stringGeometry)
        {
            try
            {
                GeometryFactory geometryFactory = GeometryFactory.Default;
                WKTReader wktReader = new WKTReader(geometryFactory);
                Geometry geometry = wktReader.Read(stringGeometry);
                return geometry;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
