using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TopologyLib;
using TopologyLib.Data.D2_32;
using TestLib.Data;


namespace TestLib.Libraries
{
    class Topoloco32 : GeometryLibrary
    {
        //public string Version;

        public Topoloco32()
        {
            GetVersion();
        }

        public override void GetConnection(string connString)
        {
            //Wird bei dieser Bibliothek nicht benötigt
        }

        public override void GetVersion()
        {
            //Version der Bibliothek abfragen
            this.Version = "Algorithm 32Bit";
        }

        public override void CheckDE9IM(List<GeometryComparison> geometryComparisons)
        {
            try
            {
                //Jedes Objektpaar soll mit seiner Mustermatrix verglichen werden
                foreach (GeometryComparison item in geometryComparisons)
                {
                    //Liste aus zu vergleichenden Objekten erzeugen die Eingabeparameter des wktMesh sind
                    List<string> wktGeometries = new List<string>();
                    wktGeometries.Add(item.BaseGeometry);
                    wktGeometries.Add(item.ComparativeGeometry);
                    //Wkt Mesh erzeugen
                    if (WktMesh.Create(wktGeometries, out bool approx, out var wktMesh))
                    {
                        foreach (PatternMatrix matrix in item.PatternMatrices)
                        {
                            //DE9IM Test in Bibliothek abfragen
                            bool relate = wktMesh.Relate(0, 1, matrix.Matrix, out var intersectionMatrix, out var parsedMatrix);
                            //Resultat speichern und dem Objekt-Paar hinzufügen
                            Result result = new Result(relate, approx, Version, intersectionMatrix.ToString());
                            matrix.Results.Add(result);
                        }
                        foreach (PatternMatrix matrix in item.TransposedPatternMatrices)
                        {
                            //DE9IM Test in Bibliothek abfragen
                            bool relate = wktMesh.Relate(1, 0, matrix.Matrix, out var intersectionMatrix, out var parsedMatrix);
                            //Resultat speichern und dem Objekt-Paar hinzufügen
                            Result result = new Result(relate, approx, Version, intersectionMatrix.ToString());
                            matrix.Results.Add(result);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Couldn't create WktMesh!");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
