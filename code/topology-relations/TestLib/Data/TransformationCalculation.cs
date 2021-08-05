using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using GeometryLib;
using GeometryLib.Decimal.D2;
using GeometryLib.Interfaces;

namespace TestLib.Data
{
    class TransformationCalculation
    {
        public static void Scaling(string scalesPath, List<GeometryComparison> geometryComparisons, int countInitialComparisons)
        {
            try
            {
                //Auslesen der Datei mit den Skalierungswerten
                var scales = ReadValues(scalesPath);

                //Die Ausgangsdaten skalieren
                //Mit jedem Skalierungswert soll gerechnet werden
                foreach (var scalingValue in scales)
                {
                    //Jeder Objektwert soll neu berechnet werden
                    for (int i = 0; i < countInitialComparisons; i++)
                    {
                        //neue Objektwerte Speicher mit Matrix aus Ausgangsvergleich
                        GeometryComparison scaleData = new GeometryComparison();
                        geometryComparisons[i].CopyPatternMatrices(scaleData.PatternMatrices);
                        geometryComparisons[i].CopyTransposedPatternMatrices(scaleData.TransposedPatternMatrices);
                        scaleData.ComparisonNumber = geometryComparisons[i].ComparisonNumber; //für Auswertung und Analyse

                        //Skalierung wird hier berechnet
                        scaleData.BaseGeometry = ScaleCalculation(geometryComparisons[i].BaseGeometry, scalingValue);
                        scaleData.ComparativeGeometry = ScaleCalculation(geometryComparisons[i].ComparativeGeometry, scalingValue);
                        //Transformationsinformation hinzufügen
                        scaleData.Transformation = "S " + scalingValue.ToString().Replace(',', '.');
                        geometryComparisons.Add(scaleData);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void Translation(string translationPath, List<GeometryComparison> geometryComparisons, int countInitialComparisons)
        {
            try
            {
                //Auslesen der Datei mit den Skalierungswerten
               var translateValues = ReadValues(translationPath);

                //Die Ausgangsdaten skalieren
                //Mit jedem Skalierungswert soll gerechnet werden
                foreach (var translateValue in translateValues)
                {
                    //Jeder Objektwert soll neu berechnet werden
                    for (int i = 0; i < countInitialComparisons; i++)
                    {
                        //neue Objektwerte Speicher mit Matrix aus Ausgangsvergleich
                        GeometryComparison translateData = new GeometryComparison();
                        geometryComparisons[i].CopyPatternMatrices(translateData.PatternMatrices);
                        geometryComparisons[i].CopyTransposedPatternMatrices(translateData.TransposedPatternMatrices);
                        translateData.ComparisonNumber = geometryComparisons[i].ComparisonNumber; //für Auswertung und Analyse

                        //Skalierung wird hier berechnet
                        translateData.BaseGeometry = TranslationCalculation(geometryComparisons[i].BaseGeometry, translateValue);
                        translateData.ComparativeGeometry = TranslationCalculation(geometryComparisons[i].ComparativeGeometry, translateValue);
                        //Transformationsinformation hinzufügen
                        translateData.Transformation = "T " + translateValue.ToString().Replace(',', '.');
                        geometryComparisons.Add(translateData);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static void Rotation(string anglesPath, List<GeometryComparison> geometryComparisons, int countInitialComparisons)
        {
            try
            {
                //Auslesen der Datei mit den Winkeln
                var angles = ReadValues(anglesPath);

                //Die Ausgangsdaten rotieren
                //Mit jedem Winkel soll gerechnet werden
                foreach (var angleValue in angles)
                {
                    //Jeder Objektwert soll neu berechnet werden
                    for (int i = 0; i < countInitialComparisons; i++)
                    {
                        //neue Objektwerte Speicher mit Matrix aus Ausgangsvergleich
                        GeometryComparison rotatedData = new GeometryComparison();
                        geometryComparisons[i].CopyPatternMatrices(rotatedData.PatternMatrices);
                        geometryComparisons[i].CopyTransposedPatternMatrices(rotatedData.TransposedPatternMatrices);
                        rotatedData.ComparisonNumber = geometryComparisons[i].ComparisonNumber; //für Auswertung und Analyse

                        //Liste für die zu rotierenden Geometrien
                        List<string> wktGeometries = new List<string>();
                        wktGeometries.Add(geometryComparisons[i].BaseGeometry);
                        wktGeometries.Add(geometryComparisons[i].ComparativeGeometry);
                        //Rotationswinkel zwischen 0° und 91° 
                        if (angleValue > 0 && angleValue < 91)
                        {
                            //Rotation wird hier berechnet
                            RotationCalculation("90", wktGeometries, out var rotatedGeometries);
                            rotatedData.BaseGeometry = rotatedGeometries["Rotate:" + angleValue.ToString()][0];
                            rotatedData.ComparativeGeometry = rotatedGeometries["Rotate:" + angleValue.ToString()][1];
                        }
                        //Rotationswinkel zwischen 90° und 181° 
                        else if (angleValue > 90 && angleValue < 181)
                        {
                            //Rotation wird hier berechnet
                            RotationCalculation("90", wktGeometries, out var rotatedGeometries);
                            //da RotationCalculation nur um 90° verschiebt muss diese funktion wiederholt werden um über 90° zu verschieben
                            List<string> rG = new List<string>();
                            rG.Add(rotatedGeometries["Rotate:90"][0]);
                            rG.Add(rotatedGeometries["Rotate:90"][1]);
                            RotationCalculation("90", rG, out var rotatedGeometries2);
                            rotatedData.BaseGeometry = rotatedGeometries2["Rotate:" + (angleValue-90).ToString()][0];
                            rotatedData.ComparativeGeometry = rotatedGeometries2["Rotate:" + (angleValue - 90).ToString()][1];
                        }
                        //Rotationswinkel zwischen 180° und 271° 
                        else if (angleValue > 180 && angleValue < 271)
                        {
                            //Rotation wird hier berechnet
                            RotationCalculation("90", wktGeometries, out var rotatedGeometries);
                            //da RotationCalculation nur um 90° verschiebt muss diese funktion wiederholt werden um über 90° zu verschieben
                            List<string> rG = new List<string>();
                            rG.Add(rotatedGeometries["Rotate:90"][0]);
                            rG.Add(rotatedGeometries["Rotate:90"][1]);
                            RotationCalculation("90", rG, out var rotatedGeometries2);
                            List<string> rG2 = new List<string>();
                            rG2.Add(rotatedGeometries2["Rotate:90"][0]);
                            rG2.Add(rotatedGeometries2["Rotate:90"][1]);
                            RotationCalculation("90", rG2, out var rotatedGeometries3);
                            rotatedData.BaseGeometry = rotatedGeometries3["Rotate:" + (angleValue - 180).ToString()][0];
                            rotatedData.ComparativeGeometry = rotatedGeometries3["Rotate:" + (angleValue - 180).ToString()][1];
                        }
                        //Rotationswinkel zwischen 270° und 360° 
                        else if (angleValue > 270 && angleValue < 360)
                        {
                            //Rotation wird hier berechnet
                            RotationCalculation("90", wktGeometries, out var rotatedGeometries);
                            //da RotationCalculation nur um 90° verschiebt muss diese funktion wiederholt werden um über 90° zu verschieben
                            List<string> rG = new List<string>();
                            rG.Add(rotatedGeometries["Rotate:90"][0]);
                            rG.Add(rotatedGeometries["Rotate:90"][1]);
                            RotationCalculation("90", rG, out var rotatedGeometries2);
                            List<string> rG2 = new List<string>();
                            rG2.Add(rotatedGeometries2["Rotate:90"][0]);
                            rG2.Add(rotatedGeometries2["Rotate:90"][1]);
                            RotationCalculation("90", rG2, out var rotatedGeometries3);
                            List<string> rG3 = new List<string>();
                            rG3.Add(rotatedGeometries3["Rotate:90"][0]);
                            rG3.Add(rotatedGeometries3["Rotate:90"][1]);
                            RotationCalculation("90", rG3, out var rotatedGeometries4);
                            rotatedData.BaseGeometry = rotatedGeometries4["Rotate:" + (angleValue - 270).ToString()][0];
                            rotatedData.ComparativeGeometry = rotatedGeometries4["Rotate:" + (angleValue - 270).ToString()][1];
                        }
                        //Rotationswinkel ist nicht richtig
                        else
                        {
                            Console.WriteLine("Der Winkel " + angleValue + "° liegt nicht zwischen 0° und 360°. 0° und 360° sind ebenfalls nicht sinnvoll.\nBitte ändern Sie den Winkel und starten das Programm neu.");
                            Console.ReadKey(true); //So the user close the console
                        }
                        //Transformationsinformation hinzufügen
                        rotatedData.Transformation = "Rotation Angle: " + angleValue.ToString().Replace(',', '.') + "Grad";
                        geometryComparisons.Add(rotatedData);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static string ScaleCalculation(string geometry, decimal scalingValue)
        {
            //Werte der Geometrie werden geteilt
            CutWkt(geometry, out var numbers, out var wktBasicString);
            //einzelnen Werte werden nun skaliert 
            var scaledNumbers = new List<decimal>();
            foreach (var number in numbers)
            {
                scaledNumbers.Add(number * scalingValue);
            }
            //neuer WKT mit skalierten Werte erzeugen
            var wktString = WriteWkt(scaledNumbers, wktBasicString);
            return wktString;
        }

        private static string TranslationCalculation(string geometry, decimal translateValue)
        {
            //Werte der Geometrie werden geteilt
            CutWkt(geometry, out var numbers, out var wktBasicString);
            //einzelnen Werte werden nun skaliert 
            var translatedNumbers = new List<decimal>();
            foreach (var number in numbers)
            {
                translatedNumbers.Add(number + translateValue);
            }
            //neuer WKT mit skalierten Werte erzeugen
            var wktString = WriteWkt(translatedNumbers, wktBasicString);
            return wktString;
        }

        private static void CutWkt(string geometry, out List<decimal> numbers, out string wktString)
        {
            try
            {
                numbers = new List<decimal>();
                wktString = "";
                string stringNumbers;
                string geometryType = geometry.Split('(')[0];
                //Den String teilen und gucken ob es ein Polygon ist und dessen WKT-Format hat oder nicht
                if (geometryType == "POLYGON")
                {
                    //Da WKT-Polygon zwei () hat muss hier im Array der dritte Wert genommen werden; nicht implementiert ist der Sonderfall wenn das Polygon einen inneren Kreis besitzt
                    stringNumbers = geometry.Split('(')[2];
                    wktString = geometryType + "((x))";
                }
                else
                {
                    //Da WKT-Point und Line nur ein () hat muss hier im Array der zweite Wert genommen werden
                    stringNumbers = geometry.Split('(')[1];
                    wktString = geometryType + "(x)";
                }
                //Zahlen werden als Einzelnes geteilt
                var arrayNumbers = stringNumbers.Split(')')[0].Replace(',', ' ').Split(' ');
                //einzelne Zahlen als String werden in Double konvertiert und in eine Liste geschrieben
                foreach (string number in arrayNumbers)
                {
                    if (number != "")
                    {
                        numbers.Add(Convert.ToDecimal(number));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static string WriteWkt(List<decimal> calculatedValues, string wktBasicString)
        {
            try
            {
                //neuer String in dem alle Werte zusammengüfgt werden
                string zusatzString = "";
                //alle Werte betrachten
                for (int j = 0; j < calculatedValues.Count; j++)
                {
                    //Wert in Decimal konvertieren damit die Ausgabe iner einer Dezimalzahl ist und nicht bsp. 1E-04
                    string scaleString = calculatedValues[j].ToString().Replace(',', '.');

                    if (j == calculatedValues.Count - 1)
                    {
                        //der letzte Wert steht alleine ohne Anhang
                        zusatzString = zusatzString + scaleString;
                    }
                    else if (j % 2 != 0)
                    {
                        //die Zahlen an ungerader Stelle in der Liste erhalten ein Komma hinter sich
                        zusatzString = zusatzString + scaleString + ", ";
                    }
                    else
                    {
                        //die Zahlen an gerader Stelle in der Liste erhalten ein Leerzeichen hinter sich
                        zusatzString = zusatzString + scaleString + " ";
                    }
                }
                //zusammengefügte Werte ersetzen im Hauptstring ihren Platzhalter(x)
                var wktString = wktBasicString.Replace("x", zusatzString);
                return wktString;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private static List<decimal> ReadValues(string valuesPath)
        {
            //Auslesen der Datei mit den Skalierungswerten
            string line;
            var values = new List<decimal>();
            StreamReader valuesFile = new StreamReader(valuesPath);
            while ((line = valuesFile.ReadLine()) != null)
            {
                //Konvertierung von String zu Double, hierür muss das DezimalTrennzeichen festgelegt werden
                NumberFormatInfo provider = new NumberFormatInfo();
                provider.NumberDecimalSeparator = ".";
                values.Add(Convert.ToDecimal(line, provider));
            }
            return values;
        }

        private static bool RotationCalculation(string stepsfor90Degree, List<string> wktGeometries, out Dictionary<string, string[]> rotatedGeometries)
        {
            rotatedGeometries = new Dictionary<string, string[]>();
            if (!int.TryParse(stepsfor90Degree, out var steps))
                return false;
            var geom = new IGeometryOgc2<decimal>[wktGeometries.Count];
            var box = BBox.Empty;
            for (int i = 0; i < wktGeometries.Count; i++)
            {
                string wktGeometry = wktGeometries[i];
                if (Vector.TryParseWkt(wktGeometry, out var vector))
                {
                    geom[i] = vector;
                    box = box.Extend(vector);
                }
                else if (LineString.TryParseWkt(wktGeometry, out var lineString))
                {
                    geom[i] = lineString;
                    box = box.Combine(lineString.BBox);
                }
                else if (Polygon.TryParseWkt(wktGeometry, out var polygon))
                {
                    geom[i] = polygon;
                    box = box.Combine(polygon.BBox);
                }
                else if (MultiLineString.TryParseWkt(wktGeometry, out var multi))
                {
                    geom[i] = multi;
                    box = box.Combine(multi.BBox);
                }
                else
                    return false;
            }

            var maxValue = decimal.MaxValue / Math.Max(Math.Abs(box.Max.x), Math.Max(Math.Abs(box.Max.y),
                Math.Max(Math.Abs(box.Min.x), Math.Abs(box.Min.y))));
            double tol = Math.PI / 2.0e6 / steps;
            int halfSteps = steps / 2 + 1;
            var values = new (decimal, decimal)[steps + 1];
            values[0] = (1m, 0m);
            values[steps] = (0m, 1m);
            for (int i = 1; i < halfSteps; i++)
            {
                double rad = i * Math.PI / (2 * steps);
                var (cos, sin) = values[i] = CosSin(rad, maxValue, tol);
                values[steps - i] = (sin, cos);
            }
            for (int i = 0; i <= steps; i++)
            {
                var rotated = new string[wktGeometries.Count];
                for (int j = 0; j < geom.Length; j++)
                {
                    IGeometryOgc2<decimal> geometry = geom[j];
                    switch (geometry)
                    {
                        case GeometryLib.Decimal.D2.Vector v:
                            rotated[j] = Transform(v, values[i]).ToWktString();
                            break;
                        case LineString l:
                            var vecs = new List<Vector>(l.Count);
                            foreach (var lv in l)
                            {
                                vecs.Add(Transform(lv, values[i]));
                            }
                            rotated[j] = new LineString(vecs, l.IsLinearRing).ToWktString();
                            break;
                        case Polygon p:
                            var rings = new List<LineString>(p.Count);
                            foreach (var ring in p)
                            {
                                var rring = new List<Vector>(ring.Count);
                                foreach (var rv in ring)
                                {
                                    rring.Add(Transform(rv, values[i]));
                                }
                                rings.Add(new LineString(rring, true));
                            }
                            if (!Polygon.Create(rings, out var rp))
                                return false;
                            rotated[j] = rp.ToWktString();
                            break;
                        case MultiLineString m:
                            var lines = new List<LineString>(m.Count);
                            foreach (var line in m)
                            {
                                var mline = new List<Vector>(line.Count);
                                foreach (var lv in line)
                                {
                                    mline.Add(Transform(lv, values[i]));
                                }
                                lines.Add(new LineString(mline, line.IsLinearRing));
                            }
                            rotated[j] = new MultiLineString(lines).ToWktString();
                            break;
                    }
                }
                rotatedGeometries.Add($"Rotate:{i * 90.0 / steps}", rotated);
            }
            return true;
        }

        private static Vector Transform(Vector v, (decimal cos, decimal sin) cossin)
        {
            var (cos, sin) = cossin;
            decimal rx = v.x * cos - v.y * sin;
            decimal ry = v.x * sin + v.y * cos;
            return new Vector(rx, ry);
        }

        private static (decimal cos, decimal sin) CosSin(double rad, decimal maxValue, double tol)
        {
            decimal tan = (decimal)Math.Tan(rad / 2);
            decimal tan2 = tan * tan;
            decimal ptan2 = 1m + tan2;
            decimal cos = (1m - tan2) / ptan2;
            decimal sin = 2m * tan / ptan2;
            int round = 27;
            while (IsValidTrig(cos, maxValue) && IsValidTrig(sin, maxValue) && (cos * cos + sin * sin) != 1m && round >= 0)
            {
                decimal ttan = Math.Round(tan, round);
                tan2 = ttan * ttan;
                ptan2 = 1m + tan2;
                decimal oldSin = sin;
                decimal oldCos = cos;
                cos = (1m - tan2) / ptan2;
                sin = 2m * ttan / ptan2;
                round--;
            }
            if (Math.Abs(Math.Atan2((double)sin, (double)cos) - rad) > tol)
                return CosSin(rad + tol, maxValue, tol);
            return (cos, sin);
        }

        private static bool IsValidTrig(in decimal trig, in decimal maxValue)
        {
            try
            {
                decimal trigm = trig * maxValue;
                return trig != 0m && trigm / trig == maxValue;
            }
            catch
            {
                return false;
            }
        }
    }
}
