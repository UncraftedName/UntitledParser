using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;

namespace DemoParser.Utils {
	
	public class BitStreamReader : ICloneable {

		public readonly byte[] Data;
		public readonly int BitLength;
		private readonly int _start; // index of first readable bit
		public int AbsoluteBitIndex {get;private set;}
		public int CurrentBitIndex {
			get => AbsoluteBitIndex - _start;
			set => AbsoluteBitIndex = _start + value;
		}
		public int BitsRemaining => _start + BitLength - AbsoluteBitIndex;
		internal bool IsLittleEndian; // this doesn't work w/ big endian atm, probably won't try to fix it since it's not necessary
		private byte CurrentByte => Data[AbsoluteBitIndex >> 3];  // same as Pointer / 8
		private byte IndexInByte => (byte)(AbsoluteBitIndex & 0x07); // same as Pointer % 8
		private int RemainingBitMask => 0xff << IndexInByte;         // mask to get remaining bits in this byte
		private bool IsByteAligned => IndexInByte == 0;
		

		public BitStreamReader(byte[] data, bool isLittleEndian = true) : this(data, data.Length << 3, 0, isLittleEndian) {}


		private BitStreamReader(byte[] data, int bitLength, int start, bool isLittleEndian) {
			Data = data;
			BitLength = bitLength;
			AbsoluteBitIndex = _start = start;
			IsLittleEndian = isLittleEndian;
		}


		public object Clone() {
			return MemberwiseClone();
		}


		// splits this stream, and returns a stream which starts at the next readable bit
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
			return new BitStreamReader(Data, bitCount, _start + fromBitIndex, IsLittleEndian);
		}
		

		public BitStreamReader FromBeginning() {
			return new BitStreamReader(Data, BitLength, _start, IsLittleEndian);
		}

		
		public BitStreamReader WithEndAt(BitStreamReader reader) {
			return new BitStreamReader(Data, reader.AbsoluteBitIndex - _start, _start, IsLittleEndian);
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


		// these find functions should only be used for a one time search, they should never be in a release version of the parser
		// be sure to create a substream before calling this if you want to keep using the same offset later
		
		public int FindUInt(uint val) => FindUInt(val, BitUtils.HighestBitIndex(val) + 1);
		
		
		public int FindUInt(uint val, int bitCount) {
			var tmp = new BitStreamWriter(4, IsLittleEndian);
			tmp.WriteUInt(val);
			return FindBytes(tmp.AsArray, bitCount);
		}


		public List<int> FindUIntAllInstances(uint val, int bitCount) {
			var tmp = new BitStreamWriter(4, IsLittleEndian);
			tmp.WriteUInt(val);
			return FindBytesAllInstances(tmp.AsArray, bitCount);
		}


		public int FindFloat(float val) {
			var tmp = new BitStreamWriter(4, IsLittleEndian);
			tmp.WriteFloat(val);
			return FindBytes(tmp.AsArray, 32);
		}


		public int FindBytes(byte[] arr, int bitCount) {
			byte[] resized = new byte[((bitCount - 1) >> 3) + 1]; // ensure that resized is the minimum length
			Array.Copy(arr, resized, resized.Length);
			while (BitsRemaining >= bitCount) {
				if (SubStream().ReadBits(bitCount).SequenceEqual(resized))
					return CurrentBitIndex;
				AbsoluteBitIndex++;
			}
			return -1;
		}


		public int FindString(string s) {
			byte[] bytes = ParserTextUtils.StringAsByteArray(s, s.Length);
			return FindBytes(bytes, bytes.Length << 3);
		}


		public List<int> FindBytesAllInstances(byte[] arr, int bitCount) {
			byte[] resized = new byte[((bitCount - 1) >> 3) + 1]; // ensure that resized is the minimum length
			Array.Copy(arr, resized, resized.Length);
			List<int> results = new List<int>();
			while (BitsRemaining >= bitCount) {
				if (SubStream().ReadBits(bitCount).SequenceEqual(resized))
					results.Add(CurrentBitIndex);
				AbsoluteBitIndex++;
			}
			return results;
		}


		public (int offset, T component) FindComponentWithProperty<T>(Func<T, bool> property, SourceDemo demoRef) where T : DemoComponent {
			while (BitsRemaining > 0) {
				T inst = (T)Activator.CreateInstance(typeof(T), demoRef, SubStream());
				try {
					inst.ParseStream(SubStream());
				}
				catch (Exception) {} // the whole point is to push past exceptions for a brute force search
				if (property.Invoke(inst))
					return (CurrentBitIndex, inst);
				AbsoluteBitIndex++;
			}
			return (-1, null);
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


		[Conditional("DEBUG")]
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
			if (IsByteAligned) {
				EnsureCapacity(byteCount << 3);
				byte[] result = Data[(AbsoluteBitIndex >> 3)..((AbsoluteBitIndex >> 3) + byteCount)];
				AbsoluteBitIndex += byteCount << 3;
				return result;
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


		public Tuple<byte[], int> ReadRemainingBits() {
			int bitsRemaining = BitsRemaining;
			return Tuple.Create(ReadBits(bitsRemaining), bitsRemaining);
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


		public int ReadBitsAsSInt(int bitCount) {
			int res = (int)ReadBitsAsUInt(bitCount);
			if ((res & (1 << (bitCount - 1))) != 0) // sign extend, not necessary for primitive types
				res |= int.MaxValue << bitCount; // can use int here since the leftmost 0 will get shifted away anyway
			return res;
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
		}


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
		
		
		public uint ReadUBitInt() {
			uint ret = ReadBitsAsUInt(6);
			ret = (ret & (16 | 32)) switch {
				16 => ((ret & 15) | (ReadBitsAsUInt(4) << 4)),
				32 => ((ret & 15) | (ReadBitsAsUInt(8) << 4)),
				48 => ((ret & 15) | (ReadBitsAsUInt(28) << 4)),
				_ => ret
			};
			return ret;
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


		public float ReadBitAngle(int bitCount) {
			float shift = 1 << bitCount;
			uint i = ReadBitsAsUInt(bitCount);
			return i * (360f / shift);
		}


		public float ReadCoord() {
			float val = 0;
			int intVal = ReadBool() ? 1 : 0;
			int fracVal = ReadBool() ? 1 : 0;
			if (intVal == 1 || fracVal == 1) {
				bool sign = ReadBool();
				if (intVal == 1)
					intVal = (int)ReadBitsAsUInt(14) + 1;
				if (fracVal == 1)
					fracVal = (int)ReadBitsAsUInt(5);
				val = intVal + fracVal * (1f / (1 << 5));
				val *= sign ? -1 : 1;
			}
			return val;
		}


		public Vector3 ReadVectorCoord() {
			var exists = new {x = ReadBool(), y = ReadBool(), z = ReadBool()};
			return new Vector3 {X = exists.x ? ReadCoord() : 0, Y = exists.y ? ReadCoord() : 0, Z = exists.z ? ReadCoord() : 0};
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