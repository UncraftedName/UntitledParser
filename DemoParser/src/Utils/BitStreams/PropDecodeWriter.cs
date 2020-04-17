using System;

namespace DemoParser.Utils.BitStreams {
    
    // todo
    public partial class BitStreamWriter {
        public void WriteBitCoord(float f) {
            const int COORD_INTEGER_BITS		= 14;
            const int COORD_FRACTIONAL_BITS	= 5;
            const int COORD_DENOMINATOR		= (1 << (COORD_FRACTIONAL_BITS));
            const double COORD_RESOLUTION		= (1.0 / (COORD_DENOMINATOR));
			
            bool signBit = f <= -COORD_RESOLUTION;
            int intval = (int)Math.Abs(f);
            int fractval = Math.Abs((int)(f * COORD_DENOMINATOR)) & (COORD_DENOMINATOR - 1);
			
            WriteBool(intval != 0);
            WriteBool(fractval != 0);
            //bsw.WriteBitsFromInt(intval & 1, 1);
            //bsw.WriteBitsFromInt(fractval & 1, 1);

            if (intval != 0 || fractval != 0) {
                WriteBool(signBit);
                if (intval != 0) {
                    intval--;
                    WriteBitsFromInt(intval, COORD_INTEGER_BITS);
                }
                if (fractval != 0)
                    WriteBitsFromInt(fractval, COORD_FRACTIONAL_BITS);
            }
        }
    }
}