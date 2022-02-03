using System;
using System.Diagnostics;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Utils {

	public static class Compression {

		public const uint LZSS_ID   = 'L' | ('Z' << 8) | ('S' << 16) | ('S' << 24);
		public const uint SNAPPY_ID = 'S' | ('N' << 8) | ('A' << 16) | ('P' << 24);


		public static unsafe byte[] Decompress(ref BitStreamReader bsr, int compSize) {
			uint type = bsr.ReadUInt();
			int decompSize = bsr.ReadSInt();
			switch (type) {
				case LZSS_ID:
					Span<byte> dataIn = compSize < 100000 ? stackalloc byte[compSize] : new byte[compSize];
					bsr.ReadToSpan(dataIn);
					byte[] arrOut = new byte[decompSize];
					fixed (byte* pDataIn = dataIn)
					fixed (byte* pDataOut = arrOut)
						DecompressLZSS(pDataIn, pDataOut);
					return arrOut;
				case SNAPPY_ID:
					Debug.Assert(false);
					throw new NotImplementedException("snappy decompression not implemented");
				default:
					throw new InvalidOperationException("unknown compression method");
			}
		}


		private static unsafe void DecompressLZSS(byte* pDataIn, byte* pDataOut) {
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
