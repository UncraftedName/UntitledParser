using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace UncraftedDemoParser.Utils {
	
	// Used to read the "optional" fields, allows reading individual bits
	// and reading fields that don't start on a byte boundary.
	// The pointer is incremented automatically.
	public class BitFieldReader {

		public byte[] Data {get; private set;}
		public int Length {get; private set;}
		public int Pointer; // total bit counter
		private byte CurrentByte => Data[Pointer >> 3]; 	// same as Pointer / 8
		private int BitIndex => Pointer & 0x07; 			// same as Pointer % 8
		private int RemainingBitMask => 0xff << BitIndex;	// mask to get remaining bits in this byte
		private bool IsByteAligned => BitIndex == 0;
		public bool IsBigEndian;
		
		
		public BitFieldReader(byte[] data, int bitLength, bool isBigEndian = false) {
			Data = data;
			Pointer = 0;
			Length = bitLength;
			IsBigEndian = isBigEndian;
		}


		public BitFieldReader(byte[] data, bool isBigEndian = false) : this(data, data.Length * 8, isBigEndian) {}


		// only for the current byte
		private int BitMask(int bitCount) {
			return RemainingBitMask & (0xff >> (8 - bitCount - BitIndex));
		}
		

		public bool ReadBool() {
			bool result = (CurrentByte & BitMask(1)) != 0;
			Pointer++;
			return result;
		}
		

		public byte ReadByte() {
			if (IsByteAligned) {
				byte result = CurrentByte;
				Pointer += 8;
				return result;
			} else {
				int bitsToGetInNextByte = BitIndex;
				int bitsToGetInCurrentByte = 8 - bitsToGetInNextByte;
				int output = (CurrentByte & RemainingBitMask) >> bitsToGetInNextByte;
				Pointer += bitsToGetInCurrentByte;
				output |= (CurrentByte & BitMask(bitsToGetInNextByte)) << bitsToGetInCurrentByte;
				Pointer += bitsToGetInNextByte;
				return (byte) output;
			}
		}

		public byte[] ReadBytes(int byteCount) {
			if (IsByteAligned) {
				byte[] result = Data.SubArray(Pointer >> 3, byteCount);
				Pointer += byteCount << 3;
				return result;
			} else {
				byte[] output = new byte[byteCount];
				for (int i = 0; i < byteCount; i++)
					output[i] = ReadByte();
				return output;
			}
		}
		

		public byte[] ReadBits(int bitCount) {
			int byteCount = bitCount >> 3;
			byte[] bytes = ReadBytes(byteCount);
			bitCount &= 0x07; // remaining bits
			if (bitCount > 0) {
				Array.Resize(ref bytes, bytes.Length + 1);
				int lastByte;
				int bitsLeft = 8 - BitIndex;
				if (bitCount > bitsLeft) { // if remaining bits stretch over 2 bytes
					lastByte = CurrentByte & RemainingBitMask; // get bits from current byte
					lastByte >>= BitIndex;
					Pointer += bitsLeft;
					bitCount -= bitsLeft;
					lastByte |= (CurrentByte & BitMask(bitCount)) << bitsLeft; // get bits from remaining byte
				} else {
					lastByte = CurrentByte & BitMask(bitCount) >> BitIndex;
				}
				Pointer += bitCount;
				bytes[bytes.Length - 1] = (byte)lastByte;
			}
			return bytes;
		}


		public Tuple<byte[], int> ReadRemainingBits() {
			int count = Length - Pointer;
			return Tuple.Create(ReadBits(count), count);
		}


		public int ReadBitsAsInt(int bitCount) {
			if (bitCount > 32 || bitCount < 0)
				throw new ArgumentException();
			byte[] bytes = new byte[4];
			Array.Copy(ReadBits(bitCount), 0, bytes, 0, (int)Math.Ceiling(bitCount / 8.0f));
			if (!BitConverter.IsLittleEndian ^ IsBigEndian)
				Array.Reverse(bytes);
			return BitConverter.ToInt32(bytes, 0);
		}
		

		public string ReadNullTerminatedString() {
			List<byte> bytes = new List<byte>();
			byte nextByte = ReadByte();
			while (nextByte != 0) {
				bytes.Add(nextByte);
				nextByte = ReadByte();
			}
			return Encoding.ASCII.GetString(bytes.ToArray());
		}


		public string ReadStringOfLength(int strLength) {
			return Encoding.ASCII.GetString(ReadBytes(strLength));
;		}

		
		public void DiscardPreviousBits() {
			/*int bitsToRead = Pointer + 1;
			Pointer = 0;
			byte[] discardedBits = ReadBits(bitsToRead);*/
			(byte[] bytes, int bitCount) = ReadRemainingBits();
			Data = bytes;
			Length = bitCount;
			Pointer = 0;
			//return discardedBits;
		}
		

		private T ReadPrimitive<T>(Func<byte[], int, T> bitConverterFunc, int sizeOfType) {
			byte[] bytes = ReadBytes(sizeOfType);
			if (!BitConverter.IsLittleEndian ^ IsBigEndian)
				Array.Reverse(bytes);
			return bitConverterFunc(bytes, 0);
		}
		

		public int ReadInt() {
			return ReadPrimitive(BitConverter.ToInt32, sizeof(int));
		}
		
		
		public short ReadShort() {
			return ReadPrimitive(BitConverter.ToInt16, sizeof(short));
		}
		

		public float ReadFloat() {
			return ReadPrimitive(BitConverter.ToSingle, sizeof(float));
		}


		public Vector3 ReadVector3() {
			return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
		}

		
		// for all 'IfExists' methods - read one bit, if it's set, read the desired field


		public bool? ReadBoolIfExists() { // wtf is this shit
			return ReadBool() ? ReadBool() : (bool?) null;
		}


		public byte? ReadByteIfExists() {
			return ReadBool() ? ReadByte() : (byte?) null;
		}
		
		
		private T? ReadPrimitiveIfExists<T>(Func<byte[], int, T> bitConverterFunc, int sizeOfType) where T : struct {
			return ReadBool() ? (T?)ReadPrimitive(bitConverterFunc, sizeOfType) : null;
		}
		
		
		public int? ReadIntIfExists() {
			return ReadPrimitiveIfExists(BitConverter.ToInt32, sizeof(int));
		}
		
		
		public short? ReadShortIfExists() {
			return ReadPrimitiveIfExists(BitConverter.ToInt16, sizeof(short));
		}
		

		public float? ReadFloatIfExists() {
			return ReadPrimitiveIfExists(BitConverter.ToSingle, sizeof(float));
		}


		public byte[] ReadBytesIfExists(int byteCount) {
			return ReadBool() ? ReadBytes(byteCount) : null;
		}


		public byte[] ReadBitsIfExists(int bitCount) {
			return ReadBool() ? ReadBits(bitCount) : null;
		}


		public int? ReadBitsAsIntIfExists(int bitCount) {
			return ReadBool() ? ReadBitsAsInt(bitCount) : (int?)null;
		}


		public string ReadStringIfExists() {
			return ReadBool() ? ReadNullTerminatedString() : null;
		}
	}
}