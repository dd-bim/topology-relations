using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLib.Data
{
    class GeometryComparison
    {
        public string BaseGeometry { get; set; }
        public string ComparativeGeometry { get; set; }
        public string Transformation { get; set; }
        public List<PatternMatrix> PatternMatrices { get; set; }
        public List<PatternMatrix> TransposedPatternMatrices { get; set; }
        public int ComparisonNumber { get; set; }//für Auswertung und Analyse

        public GeometryComparison()
        {
            BaseGeometry = "";
            ComparativeGeometry = "";
            Transformation = "";
            PatternMatrices = new List<PatternMatrix>();
            TransposedPatternMatrices = new List<PatternMatrix>();
        }

        public GeometryComparison(string baseGeometry, string comparativeGeometry, string transformation, List<PatternMatrix> patternMatrices, List<PatternMatrix> transposedPatternMatrices)
        {
            BaseGeometry = baseGeometry;
            ComparativeGeometry = comparativeGeometry;
            Transformation = transformation;
            PatternMatrices = new List<PatternMatrix>();
            foreach (PatternMatrix item in patternMatrices)
            {
                PatternMatrix newMatrix = new PatternMatrix(item.Matrix, item.Results);
                PatternMatrices.Add(newMatrix);
            }
            TransposedPatternMatrices = new List<PatternMatrix>();
            foreach (PatternMatrix item in transposedPatternMatrices)
            {
                PatternMatrix newMatrix = new PatternMatrix(item.Matrix, item.Results);
                TransposedPatternMatrices.Add(newMatrix);
            }
        }

        public void CopyPatternMatrices(List<PatternMatrix> newList)
        {
            foreach (PatternMatrix item in PatternMatrices)
            {
                PatternMatrix newMatrix = new PatternMatrix(item.Matrix, item.Results);
                newList.Add(newMatrix);
            }
        }

        public void CopyTransposedPatternMatrices(List<PatternMatrix> newList)
        {
            foreach (PatternMatrix item in TransposedPatternMatrices)
            {
                PatternMatrix newMatrix = new PatternMatrix(item.Matrix, item.Results);
                newList.Add(newMatrix);
            }
        }
    }

    class GeometryComparison2
    {
        public string BaseGeometry { get; set; }

        public string ComparativeGeometry { get; set; }

        public string Transformation { get; set; }

        public HashSet<string> FailedLibraries { get; set; }

        public HashSet<string> ApproximatedLibraries { get; set; }

        public int ComparisonNumber { get; set; }//für Auswertung und Analyse

        public GeometryComparison2(in GeometryComparison old)
        {
            ComparisonNumber = old.ComparisonNumber;
            BaseGeometry = old.BaseGeometry;
            ComparativeGeometry = old.ComparativeGeometry;
            Transformation = old.Transformation;
            FailedLibraries = new HashSet<string>();
            ApproximatedLibraries = new HashSet<string>();

            foreach (var pm in old.PatternMatrices)
            {
                foreach (var r in pm.Results)
                {
                    if (r.Approx)
                    {
                        ApproximatedLibraries.Add(r.Library);
                    }
                    if (!r.ResultBool)
                    {
                        FailedLibraries.Add(r.Library);
                    }
                }
            }


            foreach (var pm in old.TransposedPatternMatrices)
            {
                foreach (var r in pm.Results)
                {
                    if (r.Approx)
                    {
                        ApproximatedLibraries.Add(r.Library);
                    }
                    if (!r.ResultBool)
                    {
                        FailedLibraries.Add(r.Library);
                    }
                }
            }
        }
    }

}
