using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeometryLib.Decimal
{
    public static class Constants
    {
        public const decimal THIRD = 1m / 3m;

        /// <summary>
        ///  kleinstmöglicher Trig/Det Wert
        /// </summary>
        public const decimal TRIGTOL = 1.0e-11m;

        /// <summary>
        ///  kleinstmöglicher Schnitt-Winkel Wert, bei Ebene
        /// </summary>
        public const decimal PLANETOL = 1.0e-3m;

        /// <summary>
        /// Quadrierter kleinstmöglicher Trig/Det Wert
        /// </summary>
        public const decimal TRIGTOL_SQUARED = TRIGTOL * TRIGTOL;

        public const decimal QUARTPI = 0.7853981633974483096156608458m;

        public const decimal HALFPI =  1.5707963267948966192313216916m;

        public const decimal PI =      3.1415926535897932384626433832m;

        public const decimal TWOPI =   6.2831853071795864769252867664m;

        public const decimal EPS = 0.0000000000000000000000000001m;

        public const decimal SMALL = 0.0000000000000000000000000002m;

        public const decimal SQRT2 = 1.4142135623730950488016887242097m;

        public const decimal SQRT3 = 1.7320508075688772935274463415059m;

        public const decimal RSQRT2 = 0.70710678118654752440084436210485m;

        public const decimal RSQRT3 = 0.57735026918962576450914878050196m;
    }
}
