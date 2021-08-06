namespace TopologyLib.Data.D2.Geometry
{
    public enum Dim
    {
        None, Dim0, Dim1, Dim2
    }


    public static class DimMethods
    {
        public static Dim FromInt(this Dim d, in int dim) =>
            dim switch
            {
                0 => Dim.Dim0,
                1 => Dim.Dim1,
                2 => Dim.Dim2,
                _ => Dim.None
            };

        public static Dim FromInt(in int dim) =>
             dim switch
             {
                 0 => Dim.Dim0,
                 1 => Dim.Dim1,
                 2 => Dim.Dim2,
                 _ => Dim.None
             };
    }
}
