namespace DemoParser.Utils {
	
	public static class BitUtils {
		
		public static byte ReverseBitOrder(byte b) {
			b = (byte)((b & 0xF0) >> 4 | (b & 0x0F) << 4);
			b = (byte)((b & 0xCC) >> 2 | (b & 0x33) << 2);
			b = (byte)((b & 0xAA) >> 1 | (b & 0x55) << 1);
			return b;
		}


		public static uint SetHighestBit(uint i) { // https://www.geeksforgeeks.org/find-significant-set-bit-number/
			i |= i >> 1;
			i |= i >> 2;
			i |= i >> 4;
			i |= i >> 8;
			i |= i >> 16;
			return ++i >> 1;
		}


		public static int HighestBitIndex(uint i) {
			if (i == 0)
				return 0;
			int bit = 31;
			while (((1 << bit) & i) == 0)
				bit--;
			return bit;
		}


		public static int HighestBitIndex(int i) => HighestBitIndex((uint)i);
	}
}