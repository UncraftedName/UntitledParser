using System;

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
		

		/* replacement for dotnetcore equivalent in BitConverter */
		public static float Int32BitsToSingle(int i) {
			unsafe { return *(float *)(&i); }
		}
		
		
		public static ulong ToUInt64(Span<byte> bytes, bool little) {
			if (little)
				return ((ulong)bytes[0] << 0) | ((ulong)bytes[1] << 8) | ((ulong)bytes[2] << 16) |
					   ((ulong)bytes[3] << 24) | ((ulong)bytes[4] << 32) | ((ulong)bytes[5] << 40) |
					   ((ulong)bytes[6] << 48) | ((ulong)bytes[7] << 56);
			else
				return ((ulong)bytes[7] << 0) | ((ulong)bytes[6] << 8) | ((ulong)bytes[5] << 16) |
					   ((ulong)bytes[4] << 24) | ((ulong)bytes[3] << 32) | ((ulong)bytes[2] << 40) |
					   ((ulong)bytes[1] << 48) | ((ulong)bytes[0] << 56);
		}
		

		public static uint ToUInt32(Span<byte> bytes, bool little) {
			if (little) return (uint)(bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24));
			/* big: */	return (uint)(bytes[3] | (bytes[2] << 8) | (bytes[1] << 16) | (bytes[0] << 24));
		}
		

		public static ushort ToUInt16(Span<byte> bytes, bool little) {
			if (little) return (ushort)(bytes[0] | (bytes[1] << 8));
			/* big: */	return (ushort)(bytes[1] | (bytes[0] << 8));
		}
	}
}
