using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DE9IM_Test.Data;

namespace DE9IM_Test.GeometryLibraries
{
    abstract class GeometryLibrary
    {
        public string Version;
        public abstract void GetConnection(string connString);
        public abstract void GetVersion();
        public abstract void CheckDE9IM(List<GeometryComparison> geometryComparisons);
    }
}
