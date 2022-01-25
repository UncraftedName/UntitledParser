using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DemoParser.Utils.BitStreams {

	public partial struct BitStreamReader {

		public const int MaxDataSize = int.MaxValue / 8;
		public readonly byte[] Data;
		public int BitLength;
		public readonly int AbsoluteStart; // index of first readable bit
		internal bool IsLittleEndian; // this doesn't work w/ big endian atm, probably won't try to fix it since it's not necessary

		public int AbsoluteBitIndex {get;private set;}
		public readonly int ByteLength => BitLength >> 3;
		public readonly int AbsoluteByteIndex => AbsoluteBitIndex >> 3;
		public int CurrentBitIndex {
			readonly get => AbsoluteBitIndex - AbsoluteStart;
			set => AbsoluteBitIndex = AbsoluteStart + value;
		}
		public readonly int BitsRemaining => AbsoluteStart + BitLength - AbsoluteBitIndex;
		private readonly byte CurrentByte => Data[AbsoluteByteIndex];  // same as Pointer / 8
		private readonly byte IndexInByte => (byte)(AbsoluteBitIndex & 0x07); // same as Pointer % 8
		private readonly byte RemainingBitMask => (byte)(0xff << IndexInByte); // mask to get remaining bits in this byte
		private readonly bool IsByteAligned => IndexInByte == 0;
		private static readonly byte[,] BitMaskLookup;
		private readonly byte BitMask(int bitCount) => BitMaskLookup[AbsoluteBitIndex & 0x07, bitCount];


		static BitStreamReader() {
			BitMaskLookup = new byte[8,8];
			for (int inByte = 0; inByte < 8; inByte++)
			for (int count = 0; count < 8; count++)
				BitMaskLookup[inByte, count] = (byte)((0xff << inByte) & ~(0xff << (count + inByte)));
		}


		public BitStreamReader(byte[] data, bool isLittleEndian = true) : this(data, data.Length << 3, 0, isLittleEndian) {}


		public BitStreamReader(byte[] data, int bitLength, int start, bool isLittleEndian = true) {
			if (data.Length > MaxDataSize)
				throw new ArgumentException("input array is too long", nameof(data));
			Data = data;
			BitLength = bitLength;
			AbsoluteBitIndex = AbsoluteStart = start;
			IsLittleEndian = isLittleEndian;
		}


		// splits this stream, and returns a stream which starts at the (same) next readable bit
		public readonly BitStreamReader Split() => Split(CurrentBitIndex, BitsRemaining);


		public readonly BitStreamReader Split(uint bitLength) => Split((int)bitLength);


		public readonly BitStreamReader Split(int bitLength) => Split(CurrentBitIndex, bitLength);


		public readonly BitStreamReader Split(int fromBitIndex, int bitCount) {
			if (bitCount < 0)
				throw new ArgumentOutOfRangeException(nameof(bitCount), $"{nameof(bitCount)} cannot be less than 0");
			if (fromBitIndex + bitCount > CurrentBitIndex + BitsRemaining)
				throw new ArgumentOutOfRangeException(nameof(bitCount),
					$"{BitsRemaining} bits remaining, attempted to create a substream with {fromBitIndex + bitCount - BitsRemaining} too many bits");
			return new BitStreamReader(Data, bitCount, AbsoluteStart + fromBitIndex, IsLittleEndian);
		}


		public BitStreamReader SplitAndSkip(uint bits) => SplitAndSkip((int)bits);


		public BitStreamReader SplitAndSkip(int bits) {
			BitStreamReader ret = Split(bits);
			AbsoluteBitIndex += bits;
			return ret;
		}


		public readonly BitStreamReader FromBeginning() {
			return new BitStreamReader(Data, BitLength, AbsoluteStart, IsLittleEndian);
		}


		public readonly string ToBinaryString() {
			(byte[] bytes, int bitCount) = Split().ReadRemainingBits();
			if ((bitCount & 0x07) == 0)
				return ParserTextUtils.BytesToBinaryString(bytes);
			else
				return $"{ParserTextUtils.BytesToBinaryString(bytes.Take(bytes.Length - 1))} {ParserTextUtils.ByteToBinaryString(bytes[^1], bitCount & 0x07)}".Trim();
		}


		public readonly string ToHexString(string separator = " ") {
			(byte[] bytes, int _) = Split().ReadRemainingBits();
			return ParserTextUtils.BytesToHexString(bytes, separator);
		}


		public override string ToString() {
			return $"index: {CurrentBitIndex}, remaining: {BitsRemaining}";
		}


		public void SkipBytes(uint byteCount) => SkipBits(byteCount << 3);
		public void SkipBytes(int byteCount) => SkipBits(byteCount << 3);
		public void SkipBits(uint bitCount) => SkipBits((int)bitCount);


		// to suppress debug warnings that I didn't finish reading a component
		internal void SkipToEnd() => SkipBits(BitsRemaining);


		public void SkipBits(int bitCount) {
			EnsureCapacity(bitCount);
			AbsoluteBitIndex += bitCount;
		}


		public void EnsureByteAlignment() {
			if (!IsByteAligned)
				AbsoluteBitIndex += 8 - IndexInByte;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private readonly void EnsureCapacity(long bitCount) {
			if (bitCount > BitsRemaining)
				throw new ArgumentOutOfRangeException(nameof(bitCount),
					$"{nameof(EnsureCapacity)} failed - {bitCount} bits were needed but only {BitsRemaining} were left");
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
			return (byte)output;
		}


		public sbyte ReadSByte() {
			return (sbyte)ReadByte();
		}


		public byte[] ReadBytes(int byteCount) {
			if (byteCount < 0)
				throw new IndexOutOfRangeException($"{nameof(byteCount)} should not be negative");
			byte[] result = new byte[byteCount];
			ReadBytesToSpan(result.AsSpan());
			return result;
		}


		public void ReadBytesToSpan(Span<byte> byteSpan) {
			if (byteSpan.Length == 0)
				return;
			EnsureCapacity(byteSpan.Length << 3);
			if (IsByteAligned) {
				Data.AsSpan(AbsoluteByteIndex, byteSpan.Length).CopyTo(byteSpan);
				AbsoluteBitIndex += byteSpan.Length << 3;
			} else {
				for (int i = 0; i < byteSpan.Length; i++)
					byteSpan[i] = ReadByte();
			}
		}


		public byte[] ReadBits(int bitCount) {
			byte[] result = new byte[(bitCount >> 3) + ((bitCount & 0x07) > 0 ? 1 : 0)];
			ReadBitsToSpan(result.AsSpan(), bitCount);
			return result;
		}


		// hey idiot! bytes are read least significant bit first!
		private void ReadBitsToSpan(Span<byte> byteSpan, int bitCount) {
			Span<byte> fullBytes = byteSpan.Slice(0, bitCount >> 3);
			ReadBytesToSpan(fullBytes);
			bitCount &= 0x07;
			EnsureCapacity(bitCount);
			if (bitCount > 0) {
				int lastByte;
				int bitsLeftInByte = 8 - IndexInByte;
				if (bitCount > bitsLeftInByte) {               // if remaining bits stretch over 2 bytes
					lastByte = CurrentByte & RemainingBitMask; // get bits from current byte
					lastByte >>= IndexInByte;
					AbsoluteBitIndex += bitsLeftInByte;
					bitCount -= bitsLeftInByte;
					lastByte |= (CurrentByte & BitMask(bitCount)) << bitsLeftInByte; // get bits from remaining byte
				} else {
					lastByte = (CurrentByte & BitMask(bitCount)) >> IndexInByte;
				}
				AbsoluteBitIndex += bitCount;
				byteSpan[fullBytes.Length] = (byte)lastByte;
			}
		}


		public (byte[] bytes, int bitCount) ReadRemainingBits() {
			int rem = BitsRemaining;
			return (ReadBits(rem), rem);
		}


		public uint ReadBitsAsUInt(int bitCount) {
			if (bitCount > 32 || bitCount < 0)
				throw new ArgumentException("the number of bits requested must fit in an int", nameof(bitCount));
			Span<byte> bytes = stackalloc byte[4];
			bytes.Clear();
			ReadBitsToSpan(bytes, bitCount);
			return BitUtils.ToUInt32(bytes, IsLittleEndian);
		}


		public uint ReadBitsAsUInt(uint bitCount) => ReadBitsAsUInt((int)bitCount);


		public int ReadBitsAsSInt(int bitCount) {
			int res = (int)ReadBitsAsUInt(bitCount);
			if ((res & (1 << (bitCount - 1))) != 0) // sign extend, not necessary for primitive types
				res |= int.MaxValue << bitCount; // can use int here since the leftmost 0 will get shifted away anyway
			return res;
		}


		public int ReadBitsAsSInt(uint bitCount) => ReadBitsAsSInt((int)bitCount);


		public string ReadNullTerminatedString() {
			unsafe {
				sbyte* strPtr = stackalloc sbyte[1024];
				int i = 0;
				do
					strPtr[i] = ReadSByte();
				while (strPtr[i++] != 0);
				return new string(strPtr);
			}
		}


		// always reads strLength bytes, but returns a string that is "null-terminated"
		public string ReadStringOfLength(int strLength) {
			if (strLength < 0)
				throw new ArgumentException("bro that's not supposed to be negative", nameof(strLength));

			Span<byte> bytes = strLength < 1000
				? stackalloc byte[strLength + 1]
				: new byte[strLength + 1];

			ReadBytesToSpan(bytes.Slice(0, strLength));
			bytes[strLength] = 0; // I would assume that in this case the string might not be null-terminated.
			unsafe {
				fixed(byte* strPtr = bytes)
					return new string((sbyte*)strPtr);
			}
		}


		public string ReadStringOfLength(uint strLength) => ReadStringOfLength((int)strLength);

		// I need to write these manually because I can't do Func<Span<byte>, T> cuz Span is a ref struct.


		public ulong ReadULong() {
			Span<byte> span = stackalloc byte[sizeof(ulong)];
			ReadBytesToSpan(span);
			return BitUtils.ToUInt64(span, IsLittleEndian);
		}

		public uint ReadUInt() {
			Span<byte> span = stackalloc byte[sizeof(uint)];
			ReadBytesToSpan(span);
			return BitUtils.ToUInt32(span, IsLittleEndian);
		}


		public int ReadSInt() => (int)ReadUInt();


		public ushort ReadUShort() {
			Span<byte> span = stackalloc byte[sizeof(ushort)];
			ReadBytesToSpan(span);
			return BitUtils.ToUInt16(span, IsLittleEndian);
		}


		public short ReadSShort() {
			Span<byte> span = stackalloc byte[sizeof(short)];
			ReadBytesToSpan(span);
			return (short)BitUtils.ToUInt16(span, IsLittleEndian);
		}


		public float ReadFloat() {
			Span<byte> span = stackalloc byte[sizeof(float)];
			ReadBytesToSpan(span);
			// float bytes shouldn't get swapped, so treat as same as system endian
			return BitUtils.Int32BitsToSingle((int)BitUtils.ToUInt32(
					span, BitConverter.IsLittleEndian));
		}


		public EHandle ReadEHandle() => (EHandle)ReadUInt();


		public T ReadStruct<T>(int size) where T : struct {
			Span<byte> span = stackalloc byte[size];
			ReadBytesToSpan(span);
			return ParserUtils.ByteSpanToStruct<T>(span);
		}


		public void ReadVector3(out Vector3 vec3) {
			vec3.X = ReadFloat();
			vec3.Y = ReadFloat();
			vec3.Z = ReadFloat();
		}


		// for all 'IfExists' methods - read one bit, if it's set, read the desired field


		public byte? ReadByteIfExists() {
			return ReadBool() ? ReadByte() : (byte?) null;
		}


		public uint? ReadUIntIfExists() {
			return ReadBool() ? ReadUInt() : (uint?)null;
		}


		public ushort? ReadUShortIfExists() {
			return ReadBool() ? ReadUShort() : (ushort?)null;
		}


		public float? ReadFloatIfExists() {
			return ReadBool() ? ReadFloat() : (float?)null;
		}


		public uint? ReadBitsAsUIntIfExists(int bitCount) {
			return ReadBool() ? ReadBitsAsUInt(bitCount) : (uint?)null;
		}


		public int? ReadBitsAsSIntIfExists(int bitCount) {
			return ReadBool() ? ReadBitsAsSInt(bitCount) : (int?)null;
		}
	}
}
