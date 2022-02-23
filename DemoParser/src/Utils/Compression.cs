using System;
using DemoParser.Parser;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Utils {

	public static class Compression {

		public const uint LZSS_ID   = 'L' | ('Z' << 8) | ('S' << 16) | ('S' << 24);
		public const uint SNAPPY_ID = 'S' | ('N' << 8) | ('A' << 16) | ('P' << 24);


		public static unsafe bool Decompress(SourceDemo dRef, ref BitStreamReader bsr, int compSize, out byte[] decompArr) {
			decompArr = Array.Empty<byte>();
			uint type = bsr.ReadUInt();
			int decompSize = bsr.ReadSInt();
			if (compSize < 0 || decompSize < 0 || compSize < bsr.BitsRemaining / 8 || bsr.HasOverflowed)
				return false;
			switch (type) {
				case LZSS_ID:
					Span<byte> dataIn = compSize < 100000 ? stackalloc byte[compSize] : new byte[compSize];
					bsr.ReadToSpan(dataIn);
					decompArr = new byte[decompSize];
					fixed (byte* pDataIn = dataIn)
					fixed (byte* pDataOut = decompArr)
						DecompressLzss(pDataIn, pDataOut);
					return true;
				case SNAPPY_ID:
					dRef.LogError("snappy decompression not implemented");
					return false;
				default:
					dRef.LogError("unknown compression method");
					return false;
			}
		}


		private static unsafe void DecompressLzss(byte* pDataIn, byte* pDataOut) {
			int cmdByte = 0;
			int getCmdByte = 0;
			for (;;) {
				if (getCmdByte == 0)
					cmdByte = *pDataIn++;
				getCmdByte = (getCmdByte + 1) & 0x07;
				if ((cmdByte & 0x01) != 0) {
					int position = *pDataIn++ << 4;
					position |= *pDataIn >> 4;
					int count = (*pDataIn++ & 0x0F) + 1;
					if (count == 1)
						break;
					byte* pSource = pDataOut - position - 1;
					for (int i = 0; i < count; i++)
						*pDataOut++ = *pSource++;
				} else {
					*pDataOut++ = *pDataIn++;
				}
				cmdByte >>= 1;
			}
		}
	}
}
