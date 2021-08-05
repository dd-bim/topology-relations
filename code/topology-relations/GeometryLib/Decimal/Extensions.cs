using System;

namespace GeometryLib.Decimal
{
    public static class Extensions
    {
        private const int SignScaleIdx = 3;
        private const int SignScaleShift = 16;
        private const int ScaleMask = 0x00FF;
        private const int SignMask =  0x0800;
        private const int SignShift = 11;
        private const decimal Max10 = decimal.MaxValue / 10m;


        public static int ScaleExp(this decimal value)
        {
            int[] parts = decimal.GetBits(value);
            return (parts[SignScaleIdx] >> SignScaleShift) & ScaleMask;
        }

        //public static decimal Scale(this decimal value)
        //{
        //    int[] parts = decimal.GetBits(value);
        //    byte scale = (byte)((parts[SignScaleIdx] >> 16) & 0x7F);
        //    return new decimal(10, 0, 0, false, scale);
        //}

        public static int Sign(this decimal value)
        {
            int[] parts = decimal.GetBits(value);
            return parts[2] == 0 && parts[1] == 0 && parts[0] == 0 ? 0 : (parts[SignScaleIdx] & 0x80000000) != 0 ? -1 : 1;
        }

        public static bool UnScaleBy(this decimal value, in int otherScale, out decimal ans)
        {
            int[] parts = decimal.GetBits(value);
            var p3 = parts[SignScaleIdx] >> SignScaleShift;
            var scale = p3 & ScaleMask;
            var sign = p3 & ~ScaleMask;
            if (otherScale > scale)
            {
                parts[SignScaleIdx] = (sign | 0) << SignScaleShift;
                ans =  new decimal(parts);
                while(otherScale > scale)
                {
                    if (ans > Max10)
                        return false;
                    ans *= 10;
                    scale++;
                }
                return true;
            }
            scale -= otherScale;
            parts[SignScaleIdx] = (sign | scale) << SignScaleShift;
            ans = new decimal(parts);
            return true;
        }

        public static decimal ScaleBy(this decimal value, in int otherScale)
        {
            int[] parts = decimal.GetBits(value);
            var p3 = parts[SignScaleIdx] >> SignScaleShift;
            var scale = p3 & ScaleMask;
            var sign = p3 & ~ScaleMask;
            scale += otherScale;
            parts[SignScaleIdx] = (sign | scale) << SignScaleShift;
            return new decimal(parts);
        }
        //public static int GetDecimalPlaces(this decimal dec, bool countTrailingZeros)
        //{
        //    const int signMask = unchecked((int)0x80000000);
        //    const int scaleMask = 0x00FF0000;
        //    const int scaleShift = 16;

        //    int[] bits = decimal.GetBits(dec);
        //    var result = (bits[3] & scaleMask) >> scaleShift;  // extract exponent

        //    // Return immediately for values without a fractional portion or if we're counting trailing zeros
        //    if (countTrailingZeros || (result == 0)) return result;

        //    // Get a raw version of the decimal's integer
        //    bits[3] = bits[3] & ~unchecked(signMask | scaleMask); // clear out exponent and negative bit
        //    var rawValue = new decimal(bits);

        //    // Account for trailing zeros
        //    while ((result > 0) && ((rawValue % 10) == 0))
        //    {
        //        result--;
        //        rawValue /= 10;
        //    }

        //    return result;
        //}

        public static int GetSignificantDigitCount(this decimal value)
        {
            // https://stackoverflow.com/questions/3683718/is-there-a-way-to-get-the-significant-figures-of-a-decimal
            /* So, the decimal type is basically represented as a fraction of two
             * integers: a numerator that can be anything, and a denominator that is 
             * some power of 10.
             * 
             * For example, the following numbers are represented by
             * the corresponding fractions:
             * 
             * VALUE    NUMERATOR   DENOMINATOR
             * 1        1           1
             * 1.0      10          10
             * 1.012    1012        1000
             * 0.04     4           100
             * 12.01    1201        100
             * 
             * So basically, if the magnitude is greater than or equal to one,
             * the number of digits is the number of digits in the numerator.
             * If it's less than one, the number of digits is the number of digits
             * in the denominator.
             */

            int[] bits = decimal.GetBits(value);

            if (value >= 1M || value <= -1M)
            {
                int highPart = bits[2];
                int middlePart = bits[1];
                int lowPart = bits[0];

                decimal num = new decimal(lowPart, middlePart, highPart, false, 0);

                int exponent = (int)Math.Ceiling(Math.Log10((double)num));

                return exponent;
            }
            else
            {
                int scalePart = bits[3];

                // Accoring to MSDN, the exponent is represented by
                // bits 16-23 (the 2nd word):
                // http://msdn.microsoft.com/en-us/library/system.decimal.getbits.aspx
                int exponent = (scalePart & 0x00FF0000) >> 16;

                return exponent + 1;
            }
        }
    }
}
