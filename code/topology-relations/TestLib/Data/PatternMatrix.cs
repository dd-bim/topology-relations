using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestLib.Data
{
    class PatternMatrix
    {
        public string Matrix { get; set; }
        public List<Result> Results { get; set; }

        public PatternMatrix()
        {
            Matrix = "";
            Results = new List<Result>();
        }
        public PatternMatrix(string matrix)
        {
            Matrix = matrix;
            Results = new List<Result>();
        }
        public PatternMatrix(string matrix, List<Result> results)
        {
            Matrix = matrix;
            Results = new List<Result>();
            //Liste kopieren
            foreach (Result result in results)
            {
                Result newResult = new Result();
                newResult.ResultBool = result.ResultBool;
                newResult.Library = result.Library;
                newResult.LibraryMatrix = result.LibraryMatrix;
                Results.Add(newResult);
            }

        }
    }
}
