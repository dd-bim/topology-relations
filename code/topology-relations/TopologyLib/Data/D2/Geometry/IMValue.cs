using System;

namespace TopologyLib.Data.D2.Geometry
{
    [Flags]
    public enum IMValue : sbyte
    {
        None = -1,
        False = 0,
        Dim0 = 1,
        Dim1 = 2,
        Dim2 = 4,
        True = 8 | Dim0 | Dim1 | Dim2
    }

    public static class IntersectionValuesMethods
    {
        public static char ToChar(this IMValue iv) => 
            iv == IMValue.None ? '*'
            : iv switch
                {
                    IMValue.None => '*',
                    IMValue.Dim0 => '0',
                    IMValue.Dim1 => '1',
                    IMValue.Dim2 => '2',
                    IMValue.True => 'T',
                    _ => 'F'
                };

        public static bool EqualsIgnoreIsTrue(this IMValue a, in IMValue b) => a < 0 || b < 0 || a > 0 == b > 0;
        
        public static bool? ToBool(this IMValue iv) =>
            IMValue.None == iv ? (bool?)null : IMValue.False != iv;

        public static bool IsTrue(this IMValue iv) => iv > 0;

        public static bool IsFalse(this IMValue iv) => iv == 0;
    }
}
