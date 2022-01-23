using System;
using static DemoParser.Utils.BitStreams.PropDecodeConsts;

namespace DemoParser.Utils.BitStreams {

	// todo
	public partial class BitStreamWriter {


		public void WriteBitCoord(float f) {
			bool signBit = f <= -CoordRes;
			int intVal = (int)Math.Abs(f);
			int fractVal = Math.Abs((int)(f * CoordDenom)) & (CoordDenom - 1);
			WriteBool(intVal != 0);
			WriteBool(fractVal != 0);
			if (intVal != 0 || fractVal != 0) {
				WriteBool(signBit);
				if (intVal != 0) {
					intVal--;
					WriteBitsFromSInt(intVal, CoordIntBits);
				}
				if (fractVal != 0)
					WriteBitsFromSInt(fractVal, CoordFracBits);
			}
		}
	}
}
