using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using TestLib.Data;
using System.IO;
using System.Reflection;

namespace TestLib.Libraries
{
    class SpatiaLite : GeometryLibrary
    {
        private SQLiteConnection Connection;
        //public string Version;
        private static bool _haveSetPath;

        public SpatiaLite(string connString)
        {
            GetConnection(connString);
            GetVersion();
        }

        public override void GetConnection(string connString)
        {
            try
            {
                Connection = new SQLiteConnection(connString);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public override void GetVersion()
        {
            try
            {
                Connection.Open();
                LoadSpatialite();
                //SQL-Befehl zur SQLite-Version
                SQLiteCommand command = Connection.CreateCommand();
                command.CommandText = "SELECT sqlite_version();";
                SQLiteDataReader datareader = command.ExecuteReader();
                //SQL-Befehl abgeben
                datareader.Read();
                //Antwort der Datenbank auslesen, anpassen und speichern
                Version = "SQLite " + datareader.GetString(0);
                //SQL-Befehl zur SpatiaLite-Version
                SQLiteCommand command2 = Connection.CreateCommand();
                command2.CommandText = "SELECT spatialite_version();";
                SQLiteDataReader datareader2 = command2.ExecuteReader();
                //SQL-Befehl abgeben
                datareader2.Read();
                Version = "SpatiaLite " + datareader2.GetString(0);
                Connection.Close();
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
                Connection.Open();
                LoadSpatialite();
                //Jedes Objektpaar soll mit seiner Mustermatrix verglichen werden
                foreach (GeometryComparison item in geometryComparisons)
                {
                    foreach (PatternMatrix matrix in item.PatternMatrices)
                    {
                        //SQL-Befehl für den Vergleich der Objekte mit entsprechenden Attributen
                        SQLiteCommand command = Connection.CreateCommand();
                        command.CommandText = "SELECT ST_Relate(GeomFromText(@pa),GeomFromText(@pi),@pm); ";
                        command.Parameters.AddWithValue("pa", item.BaseGeometry);
                        command.Parameters.AddWithValue("pi", item.ComparativeGeometry);
                        command.Parameters.AddWithValue("pm", matrix.Matrix);
                        //SQL-Befehl abgeben
                        var dataReader = command.ExecuteReader();
                        //Antwort auslesen
                        dataReader.Read();
                        bool relate = dataReader.GetBoolean(0);

                        //SQL-Befehl für den Vergleich der Objekte mit entsprechenden Attributen
                        SQLiteCommand command2 = Connection.CreateCommand();
                        command2.CommandText = "SELECT ST_Relate(GeomFromText(@pa),GeomFromText(@pi)); ";
                        command2.Parameters.AddWithValue("pa", item.BaseGeometry);
                        command2.Parameters.AddWithValue("pi", item.ComparativeGeometry);
                        //SQL-Befehl abgeben
                        var dataReader2 = command2.ExecuteReader();
                        //Antwort auslesen
                        dataReader2.Read();
                        string de9imMatrix = dataReader2.GetString(0);

                        //Antwort dem Objekt-Paar hinzufügen
                        Result result = new Result(relate, Version, de9imMatrix);
                        matrix.Results.Add(result);
                    }

                    foreach (PatternMatrix matrix in item.TransposedPatternMatrices)
                    {
                        //SQL-Befehl für den Vergleich der Objekte mit entsprechenden Attributen
                        SQLiteCommand command = Connection.CreateCommand();
                        command.CommandText = "SELECT ST_Relate(GeomFromText(@pa),GeomFromText(@pi),@pm); ";
                        command.Parameters.AddWithValue("pa", item.ComparativeGeometry);
                        command.Parameters.AddWithValue("pi", item.BaseGeometry);
                        command.Parameters.AddWithValue("pm", matrix.Matrix);
                        //SQL-Befehl abgeben
                        var dataReader = command.ExecuteReader();
                        //Antwort auslesen
                        dataReader.Read();
                        bool relate = dataReader.GetBoolean(0);

                        //SQL-Befehl für den Vergleich der Objekte mit entsprechenden Attributen
                        SQLiteCommand command2 = Connection.CreateCommand();
                        command2.CommandText = "SELECT ST_Relate(GeomFromText(@pa),GeomFromText(@pi)); ";
                        command2.Parameters.AddWithValue("pa", item.ComparativeGeometry);
                        command2.Parameters.AddWithValue("pi", item.BaseGeometry);
                        //SQL-Befehl abgeben
                        var dataReader2 = command2.ExecuteReader();
                        //Antwort auslesen
                        dataReader2.Read();
                        string de9imMatrix = dataReader2.GetString(0);

                        //Antwort dem Objekt-Paar hinzufügen
                        Result result = new Result(relate, Version, de9imMatrix);
                        matrix.Results.Add(result);
                    }
                }
                Connection.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void LoadSpatialite()
        {
            //Pfad anpasssen, sodass alle dlls gefunden werden
            if (!_haveSetPath)
            {
                var spatialitePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Spatialite_dlls");

                Environment.SetEnvironmentVariable("PATH", spatialitePath + ";" + Environment.GetEnvironmentVariable("PATH"));

                _haveSetPath = true;
            }
            Connection.EnableExtensions(true);
            //Erweiterunghinzufügen
            Connection.LoadExtension("mod_spatialite");
        }
    }
}
