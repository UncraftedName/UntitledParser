using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace DemoParser.Utils.BitStreams {
	
	public partial class BitStreamReader {
		
		public readonly IReadOnlyList<byte> Data;
		public readonly int BitLength;
		public int ByteLength => BitLength >> 3;
		public readonly int Start; // index of first readable bit
		public int AbsoluteBitIndex {get;private set;}
		public int CurrentBitIndex {
			get => AbsoluteBitIndex - Start;
			set => AbsoluteBitIndex = Start + value;
		}
		public int BitsRemaining => Start + BitLength - AbsoluteBitIndex;
		internal bool IsLittleEndian; // this doesn't work w/ big endian atm, probably won't try to fix it since it's not necessary
		private byte CurrentByte => Data[AbsoluteBitIndex >> 3];  // same as Pointer / 8
		private byte IndexInByte => (byte)(AbsoluteBitIndex & 0x07); // same as Pointer % 8
		private int RemainingBitMask => 0xff << IndexInByte;         // mask to get remaining bits in this byte
		private bool IsByteAligned => IndexInByte == 0;
		

		public BitStreamReader(IReadOnlyList<byte> data, bool isLittleEndian = true) : this(data, data.Count << 3, 0, isLittleEndian) {}


		private BitStreamReader(IReadOnlyList<byte> data, int bitLength, int start, bool isLittleEndian) {
			Data = data;
			BitLength = bitLength;
			AbsoluteBitIndex = Start = start;
			IsLittleEndian = isLittleEndian;
		}


		// splits this stream, and returns a stream which starts at the (same) next readable bit
		public BitStreamReader SubStream() => SubStream(BitsRemaining);


		public BitStreamReader SubStream(uint bitLength) => SubStream((int)bitLength);
		
		
		public BitStreamReader SubStream(int bitLength) => SubStream(CurrentBitIndex, bitLength);
		
		
		public BitStreamReader SubStream(int fromBitIndex, int bitCount) {
			if (bitCount < 0)
				throw new ArgumentOutOfRangeException(nameof(CurrentBitIndex), $"{nameof(CurrentBitIndex)} cannot be less than 0");
			if (bitCount < 0)
				throw new ArgumentOutOfRangeException(nameof(bitCount), $"{nameof(bitCount)} cannot be less than 0");
			if (fromBitIndex + bitCount > CurrentBitIndex + BitsRemaining)
				throw new ArgumentOutOfRangeException(nameof(bitCount),
					$"{BitsRemaining} bits remaining, attempted to create a substream with {fromBitIndex + bitCount - BitsRemaining} too many bits");
			return new BitStreamReader(Data, bitCount, Start + fromBitIndex, IsLittleEndian);
		}
		

		public BitStreamReader FromBeginning() {
			return new BitStreamReader(Data, BitLength, Start, IsLittleEndian);
		}

		
		public BitStreamReader WithEndAt(BitStreamReader reader) {
			return new BitStreamReader(Data, reader.AbsoluteBitIndex - Start, Start, IsLittleEndian);
		}


		public string ToBinaryString() {
			int tmp = AbsoluteBitIndex;
			(byte[] bytes, int bitCount) = ReadRemainingBits();
			AbsoluteBitIndex = tmp;
			if ((bitCount & 0x07) == 0)
				return ParserTextUtils.BytesToBinaryString(bytes);
			else
				return $"{ParserTextUtils.BytesToBinaryString(bytes[..^1])} {ParserTextUtils.ByteToBinaryString(bytes[^1], bitCount & 0x07)}".Trim();
		}


		public string ToHexString(string separator = " ") {
			int tmp = AbsoluteBitIndex;
			(byte[] bytes, int _) = ReadRemainingBits();
			AbsoluteBitIndex = tmp;
			return ParserTextUtils.BytesToHexString(bytes, separator);
		}


		public void SkipBytes(uint byteCount) => SkipBytes((int)byteCount);


		public void SkipBytes(int byteCount) {
			EnsureCapacity(byteCount << 3);
			AbsoluteBitIndex += byteCount << 3;
		}


		public void SkipBits(uint bitCount) => SkipBits((int)bitCount);


		public void SkipBits(int bitCount) {
			EnsureCapacity(bitCount);
			AbsoluteBitIndex += bitCount;
		}


		public void EnsureByteAlignment() {
			if (!IsByteAligned)
				AbsoluteBitIndex += 8 - IndexInByte;
		}


		private void EnsureCapacity(long bitCount) {
			if (bitCount > BitsRemaining)
				throw new ArgumentOutOfRangeException(nameof(bitCount),
					$"{nameof(EnsureCapacity)} failed - {bitCount} bits were needed but only {BitsRemaining} were left");
		}
		
		
		private int BitMask(int bitCount) {
			return RemainingBitMask & (0xff >> (8 - bitCount - IndexInByte));
		}


		public bool ReadBool() {
			EnsureCapacity(1);
			bool result = (CurrentByte & BitMask(1)) != 0;
			AbsoluteBitIndex++;
			return result;
		}
		

		public byte ReadByte() {
			EnsureCapacity(8);
			if (IsByteAligned) {
				byte result = CurrentByte;
				AbsoluteBitIndex += 8;
				return result;
			}

			int bitsToGetInNextByte = IndexInByte;
			int bitsToGetInCurrentByte = 8 - bitsToGetInNextByte;
			int output = (CurrentByte & RemainingBitMask) >> bitsToGetInNextByte;
			AbsoluteBitIndex += bitsToGetInCurrentByte;
			output |= (CurrentByte & BitMask(bitsToGetInNextByte)) << bitsToGetInCurrentByte;
			AbsoluteBitIndex += bitsToGetInNextByte;
			return (byte) output;
		}


		public sbyte ReadSByte() {
			return (sbyte)ReadByte();
		}
		

		public byte[] ReadBytes(int byteCount) {
			EnsureCapacity(byteCount << 3);
			if (IsByteAligned) {
				var result = Data.Skip(AbsoluteBitIndex >> 3).Take(byteCount);
				AbsoluteBitIndex += byteCount << 3;
				return result.ToArray();
			}

			byte[] output = new byte[byteCount];
			for (int i = 0; i < byteCount; i++)
				output[i] = ReadByte();
			return output;
		}
		
		
		// hey idiot! bytes are read least significant bit first!
		public byte[] ReadBits(int bitCount) {
			int byteCount = bitCount >> 3;
			byte[] bytes = ReadBytes(byteCount);
			bitCount &= 0x07; // remaining bits
			if (bitCount > 0) {
				Array.Resize(ref bytes, bytes.Length + 1);
				int lastByte;
				int bitsLeft = 8 - IndexInByte;
				if (bitCount > bitsLeft) {                     // if remaining bits stretch over 2 bytes
					lastByte = CurrentByte & RemainingBitMask; // get bits from current byte
					lastByte >>= IndexInByte;
					AbsoluteBitIndex += bitsLeft;
					bitCount -= bitsLeft;
					lastByte |= (CurrentByte & BitMask(bitCount)) << bitsLeft; // get bits from remaining byte
				} else {
					lastByte = (CurrentByte & BitMask(bitCount)) >> IndexInByte;
				}
				AbsoluteBitIndex += bitCount;
				bytes[^1] = (byte)lastByte;
			}
			return bytes;
		}


		public (byte[] bytes, int bitCount) ReadRemainingBits() {
			int bitsRemaining = BitsRemaining;
			return (ReadBits(bitsRemaining), bitsRemaining);
		}


		public uint ReadBitsAsUInt(int bitCount) {
			EnsureCapacity(bitCount);
			if (bitCount > 32 || bitCount < 0)
				throw new ArgumentException("the number of bits requested must fit in an int", nameof(bitCount));
			byte[] bytes = new byte[4];
			Array.Copy(ReadBits(bitCount), 0, bytes, 0, (bitCount - 1) / 8 + 1);
			if (BitConverter.IsLittleEndian ^ IsLittleEndian)
				Array.Reverse(bytes);
			return BitConverter.ToUInt32(bytes, 0);
		}


		public uint ReadBitsAsUInt(uint bitCount) => ReadBitsAsUInt((int)bitCount);


		public int ReadBitsAsSInt(int bitCount) {
			int res = (int)ReadBitsAsUInt(bitCount);
			if ((res & (1 << (bitCount - 1))) != 0) // sign extend, not necessary for primitive types
				res |= int.MaxValue << bitCount; // can use int here since the leftmost 0 will get shifted away anyway
			return res;
		}


		public int ReadBitsAsSInt(uint bitCount) => ReadBitsAsSInt((int)bitCount);


		public int ReadSingleBitAsSInt() => ReadBool() ? 1 : 0;
		
		
		public uint ReadOneBit() => (uint)(ReadBool() ? 1 : 0);


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
		}


		public string ReadStringOfLength(uint strLength) => ReadStringOfLength((int)strLength);


		public CharArray ReadCharArray(int byteCount) {
			return new CharArray(ReadBytes(byteCount));
		}
		

		private T ReadPrimitive<T>(Func<byte[], int, T> bitConverterFunc, int sizeOfType) {
			byte[] bytes = ReadBytes(sizeOfType);
			if (BitConverter.IsLittleEndian ^ IsLittleEndian)
				Array.Reverse(bytes);
			return bitConverterFunc(bytes, 0);
		}
		

		public uint ReadUInt() {
			return ReadPrimitive(BitConverter.ToUInt32, sizeof(uint));
		}


		public int ReadSInt() {
			return ReadPrimitive(BitConverter.ToInt32, sizeof(int));
		}
		
		
		public ushort ReadUShort() {
			return ReadPrimitive(BitConverter.ToUInt16, sizeof(ushort));
		}


		public short ReadSShort() {
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
		
		
		public uint? ReadUIntIfExists() {
			return ReadPrimitiveIfExists(BitConverter.ToUInt32, sizeof(uint));
		}
		
		
		public ushort? ReadUShortIfExists() {
			return ReadPrimitiveIfExists(BitConverter.ToUInt16, sizeof(ushort));
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


		public uint? ReadBitsAsUIntIfExists(int bitCount) {
			return ReadBool() ? ReadBitsAsUInt(bitCount) : (uint?)null;
		}


		public int? ReadBitsAsSIntIfExists(int bitCount) {
			return ReadBool() ? ReadBitsAsSInt(bitCount) : (int?)null;
		}


		public string ReadStringIfExists() {
			return ReadBool() ? ReadNullTerminatedString() : null;
		}
	}
}