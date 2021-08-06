using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Immutable;

namespace TestLib.Data
{
    class FileProcess
    {
        public static void ReadData(string geometriesFilePath, out List<GeometryComparison> geometryComparisons)
        {
            try
            {
                //Zuweisung
                geometryComparisons = new List<GeometryComparison>();

                //Lesen der Inhalte
                string line;
                //Geometrie und Matrizen speichern
                StreamReader geometriesFile = new StreamReader(geometriesFilePath);
                //String Halter und Zähler erzeugen
                List<string> holder = new List<string>();
                int counter = 1;
                int cn = 1; //Auswertung und Analyse
                //Zeilen der Datei lesen
                while ((line = geometriesFile.ReadLine()) != null)
                {
                    holder.Add(line);
                    //aller 4 zeilen ein Test-Objekt bilden und speichern
                    if (counter % 4 == 0)
                    {
                        List<PatternMatrix> patternMatrices = new List<PatternMatrix>();
                        List<PatternMatrix> transposedPatternMatrices = new List<PatternMatrix>();
                        var matrixArray = holder[2].Split(' ');
                        var transposedMatrixArray = holder[3].Split(' ');
                        foreach (string matrix in matrixArray)
                        {
                            if (matrix != "")
                            {
                                PatternMatrix newMatrix = new PatternMatrix(matrix);
                                patternMatrices.Add(newMatrix);
                            }
                        }
                        foreach (string matrix in transposedMatrixArray)
                        {
                            if (matrix != "")
                            {
                                PatternMatrix newMatrix = new PatternMatrix(matrix);
                                transposedPatternMatrices.Add(newMatrix);
                            }
                        }
                        GeometryComparison geometryComparison = new GeometryComparison(holder[0], holder[1], "Initial", patternMatrices, transposedPatternMatrices);
                        geometryComparison.ComparisonNumber = cn; //für Auswertung und Analyse
                        cn++;//für Auswertung und Analyse
                        geometryComparisons.Add(geometryComparison);

                        holder.Clear();
                    }
                    counter++;
                }
                geometriesFile.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        //public static void WriteComparisonFile(List<GeometryComparison> geometryComparisons)
        //{
        //    try
        //    {
        //        //Speicher-Dialog erstellen und anpassen
        //        SaveFileDialog sfd = new SaveFileDialog();
        //        sfd.Filter = "Comma-separated file (*.csv)|*.csv|Text files (*.txt)|*.txt|All files (*.*)|*.*";
        //        sfd.FilterIndex = 3;
        //        //sfd.InitialDirectory = @"c:\temp\";
        //        sfd.RestoreDirectory = true;
        //        sfd.DefaultExt = "csv";
        //        sfd.Title = "Save Comparison File";
        //        sfd.FileName = "DE9IMComparison";
        //        //Stream zum Schreiben einer Datei erstellen
        //        Stream myStream;
        //        if (sfd.ShowDialog() == DialogResult.OK)
        //        {
        //            if ((myStream = sfd.OpenFile()) != null)
        //            {
        //                StreamWriter sw = new StreamWriter(myStream);
        //                using (sw)
        //                {
        //                    //Schreiben der Datei
        //                    //Schreiben der erste Zeile welche extra ist
        //                    sw.WriteLine("Geometry1 WKT;Geometry2 WKT;DE-9IM Pattern-Matrix;Result Actual-Matrix;DE-9IM Actual-Matrix;Library(Version);Transformation;ComparisonNumber");
        //                    //Schreiben der Werte
        //                    foreach (GeometryComparison item in geometryComparisons)
        //                    {
        //                        foreach (PatternMatrix matrix in item.PatternMatrices)
        //                        {
        //                            foreach (Result result in matrix.Results)
        //                            {
        //                                //Nur die falschen Ergebnisse schreiben damit sie interpretiert werden
        //                                //if (!result.ResultBool)
        //                                //{
        //                                sw.WriteLine(string.Join(";", item.BaseGeometry, item.ComparativeGeometry, matrix.Matrix, result.ResultBool, result.LibraryMatrix, result.Library, item.Transformation, item.ComparisonNumber));
        //                                //}
        //                            }
        //                        }
        //                        foreach (PatternMatrix matrix in item.TransposedPatternMatrices)
        //                        {
        //                            foreach (Result result in matrix.Results)
        //                            {
        //                                //Nur die falschen Ergebnisse schreiben damit sie interpretiert werden
        //                                //if (!result.ResultBool)
        //                                //{
        //                                sw.WriteLine(string.Join(";", item.ComparativeGeometry, item.BaseGeometry, matrix.Matrix, result.ResultBool, result.LibraryMatrix, result.Library, item.Transformation, item.ComparisonNumber));
        //                                //}
        //                            }
        //                        }
        //                    }
        //                }
        //                myStream.Close();
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        throw;
        //    }
        //}

        public static void Analyse(in List<GeometryComparison> geometryComparisons)
        {
            var transValues = new List<string>();
            var tests = new SortedList<int, string>();
            var translations = new Dictionary<string, int>();
            var scalings = new Dictionary<string, int>();
            var appro = new Dictionary<string, int>();

            foreach (PatternMatrix matrix in geometryComparisons[0].PatternMatrices)
                foreach (Result result in matrix.Results)
                {
                    translations[result.Library] = 0;
                    scalings[result.Library] = 0;
                    appro[result.Library] = 0;
                }
            var alllibs = ImmutableArray.CreateRange(translations.Keys).Sort();
            var total = geometryComparisons.Count;
            var totalT = 0;
            var totalS = 0;
            var failS = new Dictionary<int, Dictionary<string, SortedSet<int>>>();
            var failT = new Dictionary<int, Dictionary<string, SortedSet<int>>>();
            var approxS = new Dictionary<int, Dictionary<string, SortedSet<int>>>();
            var approxT = new Dictionary<int, Dictionary<string, SortedSet<int>>>();

            foreach (var item in geometryComparisons)
            {
                var gc = new GeometryComparison2(item);
                if (item.Transformation == "Initial")
                {
                    tests.Add(item.ComparisonNumber,
                        $" & {item.BaseGeometry}" +
                        $" & {item.ComparativeGeometry}" +
                        $" & {item.PatternMatrices[0].Matrix}");
                }
                if (!transValues.Contains(item.Transformation.Substring(2)))
                {
                    transValues.Add(item.Transformation.Substring(2));
                }
                var num = gc.ComparisonNumber;
                if (!failS.ContainsKey(gc.ComparisonNumber))
                {
                    failS[num] = alllibs.ToDictionary(l => l, _ => new SortedSet<int>());
                    failT[num] = alllibs.ToDictionary(l => l, _ => new SortedSet<int>());
                    approxS[num] = alllibs.ToDictionary(l => l, _ => new SortedSet<int>());
                    approxT[num] = alllibs.ToDictionary(l => l, _ => new SortedSet<int>());
                }
                switch (gc.Transformation[0])
                {
                    case 'T':
                        totalT++;
                        break;
                    case 'S':
                        totalS++;
                        break;
                }
                foreach (var l in gc.ApproximatedLibraries)
                {
                    appro[l]++;
                    switch (gc.Transformation[0])
                    {
                        case 'T':
                            approxT[num][l].Add(transValues.IndexOf(gc.Transformation.Substring(2)));
                            break;
                        case 'S':
                            approxS[num][l].Add(transValues.IndexOf(gc.Transformation.Substring(2)));
                            break;
                    }
                }
                foreach (var l in gc.FailedLibraries)
                {
                    switch (gc.Transformation[0])
                    {
                        case 'T':
                            failT[num][l].Add(transValues.IndexOf(gc.Transformation.Substring(2)));
                            translations[l]++;
                            if (l.StartsWith("A") && approxT[num][l].Count == 0)
                                throw new Exception();

                            break;
                        case 'S':
                            failS[num][l].Add(transValues.IndexOf(gc.Transformation.Substring(2)));
                            scalings[l]++;
                            if (l.StartsWith("A") && approxS[num][l].Count == 0)
                                throw new Exception();
                            break;
                    }
                }


            }

            File.WriteAllLines("statistik.txt", new[]
            {
                $"Number of tested Comparisons: {tests.Count}",
                $"Total Number of Tests: {total}",
                $"Total Number of Translation Tests: {totalT}",
                $"Total Number of Scaling Tests: {totalS}",
            });

            var lines = tests.Select(kv => kv.Key.ToString() + tests[kv.Key] + " \\\\").ToList();
            lines.Insert(0, "Test \\# & \\textbf{Comparison} & \\textbf{DE-9IM Matrix} \\\\");
            File.WriteAllLines("Comparisons.csv", lines);

            lines = alllibs.Select(l => $"{l},{Math.Round((translations[l] / (double)totalT) * 100, 1)}").ToList();
            lines.Insert(0, "Library,Failed Translations");
            File.WriteAllLines("Translations.csv", lines);

            lines = alllibs.Select(l => $"{l},{Math.Round((scalings[l] / (double)totalS) * 100, 1)}").ToList();
            lines.Insert(0, "Library,Failed Scalings");
            File.WriteAllLines("Scalings.csv", lines);

            lines = alllibs.Select(l => $"{l}," +
            $"{Math.Round((appro[l] / (double)total) * 100, 1)}," +
            $"{Math.Round(((translations[l] + scalings[l]) / (double)total) * 100, 1)}").ToList();
            lines.Insert(0, "Library,Approximated,Failed");
            File.WriteAllLines("Approximations.csv", lines);

            var lstr = string.Join(",", alllibs);
            var tval = (double)(transValues.Count - 1);

            lines = failT.Select(c =>
                c.Value.Aggregate(c.Key.ToString(), (s, l) => s + $",{Math.Round(l.Value.Count / tval * 100, 1)}")).ToList();
            lines.Insert(0, "Test," + lstr);
            File.WriteAllLines("FailedTestTranslations.csv", lines);

            lines = failS.Select(c =>
                c.Value.Aggregate(c.Key.ToString(), (s, l) => s + $",{Math.Round(l.Value.Count / tval * 100, 1)}")).ToList();
            lines.Insert(0, "Test," + lstr);
            File.WriteAllLines("FailedTestScalingss.csv", lines);

            lines = approxT.Select(c =>
                c.Value.Aggregate(c.Key.ToString(), (s, l) => s + $",{Math.Round(l.Value.Count / tval * 100, 1)}")).ToList();
            lines.Insert(0, "Test," + lstr);
            File.WriteAllLines("ApproxTestTranslations.csv", lines);

            lines = approxS.Select(c =>
                c.Value.Aggregate(c.Key.ToString(), (s, l) => s + $",{Math.Round(l.Value.Count / tval * 100, 1)}")).ToList();
            lines.Insert(0, "Test," + lstr);
            File.WriteAllLines("ApproxTestScalingss.csv", lines);

            //lines = failT.Select(c =>
            //    c.Key.ToString()
            //    +      c.Value.Aggregate("", (s, l) => s + $" & {(l.Value.Count == 0 ? approxT[c.Key][l.Key].Count > 0 ? "\\textcolor{yellow}{" + Math.Round(approxT[c.Key][l.Key].Count / tval * 100) + "}" : "\\textcolor{green}{0}" : "\\textcolor{red}{" + Math.Round(l.Value.Count / tval * 100) + "}")}")
            //    + failS[c.Key].Aggregate("", (s, l) => s + $" & {(l.Value.Count == 0 ? approxS[c.Key][l.Key].Count > 0 ? "\\textcolor{yellow}{" + Math.Round(approxS[c.Key][l.Key].Count / tval * 100) + "}" : "\\textcolor{green}{0}" : "\\textcolor{red}{" + Math.Round(l.Value.Count / tval * 100) + "}")}")
            //    + " \\\\").ToList();
            lines = failT.Select(c =>
                c.Key.ToString()
                + c.Value.Aggregate("", (s, l) => s + 
                    $" & {(l.Value.Count == 0 ? approxT[c.Key][l.Key].Count > 0 ? "\\textcolor{yellow}{" + Math.Round(approxT[c.Key][l.Key].Count / tval * 100) + "}" : "\\textcolor{green}{0}" : "\\textcolor{red}{" + Math.Round(l.Value.Count / tval * 100) + "}")}"
                    + $" & {(failS[c.Key][l.Key].Count == 0 ? approxS[c.Key][l.Key].Count > 0 ? "\\textcolor{yellow}{" + Math.Round(approxS[c.Key][l.Key].Count / tval * 100) + "}" : "\\textcolor{green}{0}" : "\\textcolor{red}{" + Math.Round(failS[c.Key][l.Key].Count / tval * 100) + "}")}")
                + " \\\\").ToList();
            lstr = "& \\textbf{" + string.Join("} & \\textbf{", alllibs) + "}";
            lines.Insert(0, "\\textbf{\\#}" + lstr + lstr + " \\\\");
            lines.Insert(0, "\\textbf{Test} & \\multicolumn{4}{c}{\\textbf{Failed translations [\\%]}} & \\multicolumn{4}{c}{\\textbf{Failed scalings [\\%]}} \\\\");
            File.WriteAllLines("FailedTransformations.csv", lines);
        }

        static string toRangeStr(in SortedSet<int> set)
        {
            var arr = set.ToArray();
            if (arr.Length < 1)
                return "";
            var last = arr[0] - 1;
            var all = "";
            var tmp = new List<string>();
            for (int i = 0; i < arr.Length; i++)
            {
                if(arr[i] - last != 1)
                {
                    if(tmp.Count > 0)
                    {
                        all += (all.Length == 0 ? "" : ";") 
                            + (tmp.Count == 1 ? tmp[0] : '[' + tmp[0] + "..." + tmp[tmp.Count - 1] + ']');
                    }
                    tmp.Clear();
                }
                tmp.Add(arr[i].ToString());
                last = arr[i];
            }
            all += (all.Length == 0 ? "" : ";")
                            + (tmp.Count == 1 ? tmp[0] : '[' + tmp[0] + "..." + tmp[tmp.Count - 1] + ']');
            return all;
        }

    }
}
