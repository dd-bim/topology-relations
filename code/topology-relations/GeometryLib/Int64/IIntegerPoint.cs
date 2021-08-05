using System;

namespace GeometryLib.Int64
{
    public interface IIntegerPoint:IEquatable<IIntegerPoint>
    {
        double DoubleX { get; }

        double DoubleY { get; }

        long x { get; }
        long y { get; }
    }
}
