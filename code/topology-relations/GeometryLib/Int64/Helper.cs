namespace GeometryLib.Int64
{
    internal static class Helper
    {
        public static void Sort(ref long a, ref long b) => (a, b) = a > b ? (b, a) : (a, b);

        public static void Sort(ref long a, ref long b, ref long c)
        {
            Sort(ref a, ref b);
            Sort(ref b, ref c);
            Sort(ref a, ref b);
        }
    }
}
