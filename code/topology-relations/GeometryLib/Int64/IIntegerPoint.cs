using System;

namespace GeometryLib.Int64
{
    public interface IIntegerPoint:IEquatable<IIntegerPoint>
    {
        decimal DecimalX { get; }

        decimal DecimalY { get; }

        long x { get; }
        long y { get; }

        Decimal.D2.Vector ToDecimal();
    }
}
