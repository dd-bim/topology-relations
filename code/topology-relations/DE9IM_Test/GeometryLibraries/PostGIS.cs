using DE9IM_Test.Data;
using Npgsql;
using System;
using System.Collections.Generic;

namespace DE9IM_Test.GeometryLibraries
{
    internal class PostGIS : GeometryLibrary
    {
        private NpgsqlConnection Connection;
        //public string Version;

        public PostGIS(string connString)
        {
            GetConnection(connString);
            CreateDB();
            GetVersion();
        }

        public override void GetConnection(string connString)
        {
            try
            {
                //Zuweisung
                this.Connection = new NpgsqlConnection(connString);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void CreateDB()
        {
            try
            {
                //neue DB erstellen in der Tests abgefragt werden
                if (!CheckDBExists("check_postgis"))
                {
                    var m_createdb_cmd = new NpgsqlCommand(@"
                    CREATE DATABASE check_postgis
                    WITH OWNER = postgres
                    ENCODING = 'UTF8'
                    CONNECTION LIMIT = -1;
                    ", Connection);
                    Connection.Open();
                    m_createdb_cmd.ExecuteNonQuery();
                    Connection.Close();
                    //Connection auf DB aktualisieren
                    string newConnection = Connection.ConnectionString + ";Password=password;Database=check_postgis";
                    Connection = new NpgsqlConnection(newConnection);
                    Connection.Open();
                    var command = new NpgsqlCommand("Create extension postgis;", Connection);
                    var dataReader = command.ExecuteNonQuery();
                    Connection.Close();
                }
                else
                {
                    //Connection auf DB aktualisieren
                    string newConnection = Connection.ConnectionString + ";Password=password;Database=check_postgis";
                    Connection = new NpgsqlConnection(newConnection);
                    Connection.Open();
                    Connection.Close();
                }
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
                //SQL-Befehl zur PostgreSQL-Version
                var command = new NpgsqlCommand("SELECT version();", Connection);
                //SQL-Befehl abgeben
                var dataReader = command.ExecuteReader();
                dataReader.Read();
                //Antwort der Datenbank auslesen, anpassen und speichern
                Version = dataReader.GetString(0).Split(',')[0];
                Connection.Close();

                Connection.Open();
                //SQL-Befehl zur PostGIS-Version
                var command2 = new NpgsqlCommand("SELECT PostGIS_Version();", Connection);
                //SQL-Befehl abgeben
                var dataReader2 = command2.ExecuteReader();
                dataReader2.Read();
                //Antwort der Datenbank auslesen, anpassen und speichern
                Version = Version + " PostGIS " + dataReader2.GetString(0).Split(' ')[0];
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
                //Jedes Objektpaar soll mit seiner Mustermatrix verglichen werden
                foreach (GeometryComparison item in geometryComparisons)
                {
                    foreach (PatternMatrix matrix in item.PatternMatrices)
                    {
                        Connection.Open();
                        //SQL-Befehl für den Vergleich der Objekte mit entsprechenden Attributen
                        var command = new NpgsqlCommand("SELECT ST_Relate(@pa,@pi,@pm)", Connection);
                        command.Parameters.AddWithValue("pa", item.BaseGeometry);
                        command.Parameters.AddWithValue("pi", item.ComparativeGeometry);
                        command.Parameters.AddWithValue("pm", matrix.Matrix);
                        //SQL-Befehl abgeben
                        var dataReader = command.ExecuteReader();
                        //Antwort auslesen
                        dataReader.Read();
                        bool relate = dataReader.GetBoolean(0);
                        Connection.Close();

                        Connection.Open();
                        //SQL-Befehl für den Vergleich der Objekte mit entsprechenden Attributen
                        var command2 = new NpgsqlCommand("SELECT ST_Relate(@pa,@pi)", Connection);
                        command2.Parameters.AddWithValue("pa", item.BaseGeometry);
                        command2.Parameters.AddWithValue("pi", item.ComparativeGeometry);
                        //SQL-Befehl abgeben
                        var dataReader2 = command2.ExecuteReader();
                        //Antwort auslesen
                        dataReader2.Read();
                        string de9imMatrix = dataReader2.GetString(0);
                        Connection.Close();

                        //Antwort und dem Objekt-Paar hinzufügen
                        Result result = new Result(relate, Version, de9imMatrix);
                        matrix.Results.Add(result);
                    }
                    foreach (PatternMatrix matrix in item.TransposedPatternMatrices)
                    {
                        Connection.Open();
                        //SQL-Befehl für den Vergleich der Objekte mit entsprechenden Attributen
                        var command = new NpgsqlCommand("SELECT ST_Relate(@pa,@pi,@pm)", Connection);
                        command.Parameters.AddWithValue("pa", item.ComparativeGeometry);
                        command.Parameters.AddWithValue("pi", item.BaseGeometry);
                        command.Parameters.AddWithValue("pm", matrix.Matrix);
                        //SQL-Befehl abgeben
                        var dataReader = command.ExecuteReader();
                        //Antwort auslesen
                        dataReader.Read();
                        bool relate = dataReader.GetBoolean(0);
                        Connection.Close();

                        Connection.Open();
                        //SQL-Befehl für den Vergleich der Objekte mit entsprechenden Attributen
                        var command2 = new NpgsqlCommand("SELECT ST_Relate(@pa,@pi)", Connection);
                        command2.Parameters.AddWithValue("pa", item.ComparativeGeometry);
                        command2.Parameters.AddWithValue("pi", item.BaseGeometry);
                        //SQL-Befehl abgeben
                        var dataReader2 = command2.ExecuteReader();
                        //Antwort auslesen
                        dataReader2.Read();
                        string de9imMatrix = dataReader2.GetString(0);
                        Connection.Close();

                        //Antwort und dem Objekt-Paar hinzufügen
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

        private bool CheckDBExists(string dbname)
        {
            using (NpgsqlCommand command = new NpgsqlCommand($"SELECT DATNAME FROM pg_catalog.pg_database WHERE DATNAME = '{dbname}'", Connection))
            {
                Connection.Open();
                object i = new object();
                i = command.ExecuteScalar();
                Connection.Close();
                if (i == null)
                {
                    return false;
                }
                else
                {
                    if (i.ToString().Equals(dbname)) //always 'true' (if it exists) or 'null' (if it doesn't)
                        return true;
                    else
                        return false;
                }
            }
        }
    }
}