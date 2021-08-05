using System;

namespace GeometryLib.Int32
{
    public interface IIntegerPoint:IEquatable<IIntegerPoint>
    {
        double DoubleX { get; }

        double DoubleY { get; }

        int x { get; }
        int y { get; }
    }
}
