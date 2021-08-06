using System;

namespace TopologyLib.Data.D2.Geometry
{

    public readonly struct IntersectionMatrix
    {
        private static readonly IntersectionMatrix inValid = new IntersectionMatrix(
            Dim.None, Dim.None,
            IMValue.None, IMValue.None, IMValue.None,
            IMValue.None, IMValue.None, IMValue.None,
            IMValue.None, IMValue.None, IMValue.None);

        /// <summary>
        /// Zero length Vector
        /// </summary>
        public static ref readonly IntersectionMatrix InValid => ref inValid;

        public Dim DimensionA { get; }

        public Dim DimensionB { get; }

        public IMValue II { get; }
        public IMValue IB { get; }
        public IMValue IE { get; }
        public IMValue BI { get; }
        public IMValue BB { get; }
        public IMValue BE { get; }
        public IMValue EI { get; }
        public IMValue EB { get; }
        public IMValue EE { get; }

        public IntersectionMatrix(Dim dimensionA, Dim dimensionB,
            IMValue ii, IMValue ib, IMValue ie,
            IMValue bi, IMValue bb, IMValue be,
            IMValue ei, IMValue eb, IMValue ee)
        {
            DimensionA = dimensionA;
            DimensionB = dimensionB;
            II = ii; IB = ib; IE = ie;
            BI = bi; BB = bb; BE = be;
            EI = ei; EB = eb; EE = ee;
        }


        public static IMValue FromChar(char value) =>
            value switch
            {
                '*' => IMValue.None,
                '0' => IMValue.Dim0,
                '1' => IMValue.Dim1,
                '2' => IMValue.Dim2,
                'T' => IMValue.True,
                _ => IMValue.False
            };

        public static bool TryParse(Dim dimensionA, Dim dimensionB, in string matrix, out IntersectionMatrix intersectionMatrix)
        {
            string mat = matrix.ToUpperInvariant().Trim();
            if (mat.Length != 9)
            {
                intersectionMatrix = InValid;
                return false;
            }

            intersectionMatrix = new IntersectionMatrix(dimensionA, dimensionB, 
                FromChar(mat[0]), FromChar(mat[1]), FromChar(mat[2]),
                FromChar(mat[3]), FromChar(mat[4]), FromChar(mat[5]),
                FromChar(mat[6]), FromChar(mat[7]), FromChar(mat[8])
            );

            return true;
        }
 
        public bool IsEquals => IsWithin && EI.IsFalse() && EB.IsFalse();

        public bool IsDisjoint => IMValue.False == II && IMValue.False == IB && IMValue.False == BI && IMValue.False == BB;

        public bool IsTouches => IMValue.False == II && (IMValue.True.HasFlag(IB) || IMValue.True.HasFlag(BI) || IMValue.True.HasFlag(BB));

        public bool IsContains => IMValue.True.HasFlag(II) && IMValue.False == EI && IMValue.False == EB;

        public bool IsCovers => IMValue.False == EI && IMValue.False == EB
            && (IMValue.True.HasFlag(II) || IMValue.True.HasFlag(IB) || IMValue.True.HasFlag(BI) || IMValue.True.HasFlag(BB));

        public bool IsIntersects => IMValue.True.HasFlag(II) || IMValue.True.HasFlag(IB) || IMValue.True.HasFlag(BI) || IMValue.True.HasFlag(BB);

        public bool IsWithin => IMValue.True.HasFlag(II) && IMValue.False == IE && IMValue.False == BE;

        public bool IsCoveredBy => IMValue.False == IE && IMValue.False == BE
            && (IMValue.True.HasFlag(II) || IMValue.True.HasFlag(IB) || IMValue.True.HasFlag(BI) || IMValue.True.HasFlag(BB));

        public bool IsCrosses =>
            (II == IMValue.Dim0 && (DimensionA == Dim.Dim1 || DimensionB == Dim.Dim1))
            || (IMValue.True.HasFlag(II)
            && ((DimensionA < DimensionB && IMValue.True.HasFlag(IE))
            || (DimensionA > DimensionB && IMValue.True.HasFlag(EI))));

        public bool IsOverlaps => DimensionA == DimensionB && IMValue.True.HasFlag(IE) && IMValue.True.HasFlag(EI)
            && (DimensionA == Dim.Dim1 ? II == IMValue.Dim1 : IMValue.True.HasFlag(II));



        //public GeomPredicates Predicates =>
        //      IsDisjoint ? GeomPredicates.Disjoint
        //    : IsEquals ? GeomPredicates.Equals
        //    : IsTouches ? GeomPredicates.Touches
        //    : IsOverlaps ? GeomPredicates.Overlaps
        //    : IsContains ? GeomPredicates.Contains
        //    : IsWithin ? GeomPredicates.Within
        //    : IsCovers ? GeomPredicates.Covers
        //    : IsCoveredBy ? GeomPredicates.CoveredBy
        //    : IsCrosses ? GeomPredicates.Crosses
        //    : IsIntersects ? GeomPredicates.Intersects
        //    : GeomPredicates.None;

        public GeomPredicates Predicates =>
            (II.ToBool(), IE.ToBool(), BE.ToBool(), EI.ToBool(), EB.ToBool()) switch
            {
                (false, _    , _    , _    , _) => IB.IsTrue() || BI.IsTrue() || BB.IsTrue() 
                                                        ? GeomPredicates.Touches : (int)IB + (int) BI + (int)BB == 0 
                                                        ? GeomPredicates.Disjoint : GeomPredicates.None,
                (true , false, false, false, false) => GeomPredicates.Equals,
                (true , _    , _    , false, false) => GeomPredicates.Contains,
                (_    , _    , _    , false, false) => IB.IsTrue() || BI.IsTrue() || BB.IsTrue() 
                                                        ? GeomPredicates.Covers : GeomPredicates.None,
                (true , false, false, _    , _    ) => GeomPredicates.Within,
                (_    , false, false, _    , _    ) => IB.IsTrue() || BI.IsTrue() || BB.IsTrue() 
                                                            ? GeomPredicates.CoveredBy : GeomPredicates.None,
                (true , true , _    , true , _    ) => DimensionA == DimensionB && (DimensionA != Dim.Dim1 || II == IMValue.Dim1) ? GeomPredicates.Overlaps : GeomPredicates.Intersects,
                (true , true , _    , _    , _    ) => DimensionA < DimensionB ? GeomPredicates.Crosses : GeomPredicates.Intersects,
                (true , _    , _    , true , _    ) => DimensionA > DimensionB ? GeomPredicates.Crosses : GeomPredicates.Intersects,
                (true , _    , _    , _    , _    ) => DimensionA != DimensionB && II == IMValue.Dim0 && (DimensionA == Dim.Dim1 || DimensionB == Dim.Dim1) ? GeomPredicates.Crosses: GeomPredicates.Intersects,
                _ => IB.IsTrue() || BI.IsTrue() || BB.IsTrue() ? GeomPredicates.Intersects : GeomPredicates.None,
            };


        public override string ToString() => new string(
            new[]
            {
                II.ToChar(), IB.ToChar(), IE.ToChar(),
                BI.ToChar(), BB.ToChar(), BE.ToChar(),
                EI.ToChar(), EB.ToChar(), EE.ToChar()
            });

        public string ToPrettyString() =>
             II.ToChar().ToString() + " " + IB.ToChar().ToString() + " " + IE.ToChar().ToString() + Environment.NewLine
           + BI.ToChar().ToString() + " " + BB.ToChar().ToString() + " " + BE.ToChar().ToString() + Environment.NewLine
           + EI.ToChar().ToString() + " " + EB.ToChar().ToString() + " " + EE.ToChar().ToString();

        public bool Relate(in IntersectionMatrix other) => 
                   II.EqualsIgnoreIsTrue(other.II) && IB.EqualsIgnoreIsTrue(other.IB) && IE.EqualsIgnoreIsTrue(other.IE)
                && BI.EqualsIgnoreIsTrue(other.BI) && BB.EqualsIgnoreIsTrue(other.BB) && BE.EqualsIgnoreIsTrue(other.BE)
                && EI.EqualsIgnoreIsTrue(other.EI) && EB.EqualsIgnoreIsTrue(other.EB) && EE.EqualsIgnoreIsTrue(other.EE);

        public static Dim GetDimension(IGeometry geometry) =>
            geometry is Region ? Dim.Dim2
            : geometry is Line ? Dim.Dim1 : Dim.Dim0;

        public static IntersectionMatrix Create(in IGeometry a, in IGeometry b) =>
            (a, b) switch
            {
                (Point pa, Point pb) => new IntersectionMatrix(Dim.Dim0, Dim.Dim0,
                    pa.GetII(pb),  IMValue.False, pa.GetIE(pb),
                    IMValue.False, IMValue.False, IMValue.False,
                    pb.GetIE(pa),  IMValue.False, IMValue.Dim2),
                (Point pa, Line lb) => new IntersectionMatrix(Dim.Dim0, Dim.Dim1,
                    pa.GetII(lb),  pa.GetIB(lb),  pa.GetIE(lb),
                    IMValue.False, IMValue.False, IMValue.False,
                    lb.GetIE(pa),  lb.GetBE(pa),  IMValue.Dim2),
                (Point pa, Region ab) => new IntersectionMatrix(Dim.Dim0, Dim.Dim2,
                    pa.GetII(ab),  pa.GetIB(ab),  pa.GetIE(ab),
                    IMValue.False, IMValue.False, IMValue.False,
                    ab.GetIE(pa),  ab.GetBE(pa),  IMValue.Dim2),
                (Line la, Point pb) => Create(pb, la).Transpose(),
                (Line la, Line lb) => new IntersectionMatrix(Dim.Dim1, Dim.Dim1,
                    la.GetII(lb), la.GetIB(lb), la.GetIE(lb),
                    lb.GetIB(la), la.GetBB(lb), la.GetBE(lb),
                    lb.GetIE(la), lb.GetBE(la), IMValue.Dim2),
                (Line la, Region ab) => new IntersectionMatrix(Dim.Dim1, Dim.Dim2,
                    la.GetII(ab), la.GetIB(ab), la.GetIE(ab),
                    ab.GetIB(la), la.GetBB(ab), la.GetBE(ab),
                    ab.GetIE(la), ab.GetBE(la), IMValue.Dim2),
                (Region aa, Point pb) => Create(pb, aa).Transpose(),
                (Region aa, Line lb) => Create(lb, aa).Transpose(),
                (Region aa, Region ab) => new IntersectionMatrix(Dim.Dim2, Dim.Dim2,
                    aa.GetII(ab), aa.GetIB(ab), aa.GetIE(ab),
                    ab.GetIB(aa), aa.GetBB(ab), aa.GetBE(ab),
                    ab.GetIE(aa), ab.GetBE(aa), IMValue.Dim2),
                _ => throw new Exception()
            };

        public IntersectionMatrix Transpose() => new IntersectionMatrix(
            DimensionB, DimensionA,
            II, BI, EI,
            IB, BB, EB,
            IE, BE, EE);


    }
}
