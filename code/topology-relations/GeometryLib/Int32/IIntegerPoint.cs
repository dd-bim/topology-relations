using System;

namespace GeometryLib.Int32
{
    public interface IIntegerPoint:IEquatable<IIntegerPoint>
    {
        decimal DecimalX { get; }

        decimal DecimalY { get; }

        int x { get; }
        int y { get; }

        Decimal.D2.Vector ToDecimal();
    }
}
