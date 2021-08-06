using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DE9IM_Test.Data
{
    class Result
    {
        public bool ResultBool { get; set; }

        public bool Approx { get; set; } = false;

        public string Library { get; set; }

        public string LibraryMatrix { get; set; }

        public Result()
        {
            Library = "";
            LibraryMatrix = "";
        }

        public Result(bool resultMatrix, string library)
        {
            ResultBool = resultMatrix;
            Library = library;
            LibraryMatrix = "";
        }

        public Result(bool resultMatrix, string library, string de9imResultMatrix)
        {
            ResultBool = resultMatrix;
            Library = library;
            LibraryMatrix = de9imResultMatrix;
        }


        public Result(bool resultMatrix, bool approx, string library, string de9imResultMatrix)
        {
            ResultBool = resultMatrix;
            Library = library;
            LibraryMatrix = de9imResultMatrix;
            Approx = approx;
        }
    }
}
