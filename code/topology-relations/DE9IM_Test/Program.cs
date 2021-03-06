using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DE9IM_Test.Data;
using DE9IM_Test.GeometryLibraries;

namespace DE9IM_Test
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            string[] args = new[]
            {
                @"Geometries.txt",
                @"Transformations.txt",
                "results.csv"
            };
            //Einlesen der Ausgangswerte, die vergleichenden Objekte und Muster-Matrix
            FileProcess.ReadData(args[0], out var geometryComparisons);
            //Anzahl der Ausgangsvergleiche
            int countInitialComparisons = geometryComparisons.Count();

            //Skalieren der Daten
            TransformationCalculation.Scaling(args[1], geometryComparisons, countInitialComparisons);
            //Rotation der Daten
            //TransformationCalculation.Rotation(args[2], geometryComparisons, countInitialComparisons);
            //Translation der Daten
            TransformationCalculation.Translation(args[1], geometryComparisons, countInitialComparisons);

            //Einzelne Bibliotheken aufrufen und DE9IM Test durchführen
            //PostGIS postgis = new PostGIS("");
            //postgis.CheckDE9IM(geometryComparisons);

            Topoloco64 topoloco = new Topoloco64();
            topoloco.CheckDE9IM(geometryComparisons);

            Topoloco32 topoloco32 = new Topoloco32();
            topoloco32.CheckDE9IM(geometryComparisons);

            NTS nts = new NTS();
            nts.CheckDE9IM(geometryComparisons);

            SpatiaLite spatialite = new SpatiaLite("Data Source=database.sqlite;Version=3;New=True;Compress=True;");
            spatialite.CheckDE9IM(geometryComparisons);

            FileProcess.WriteComparisonFile(geometryComparisons, args[2]);
            FileProcess.Analyse(geometryComparisons);
            //Console.WriteLine("Press key to exit");
            //Console.ReadKey(true); //So the user close the console
        }
    }
}
