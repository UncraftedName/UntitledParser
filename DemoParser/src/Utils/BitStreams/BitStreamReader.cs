using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;

namespace DemoParser.Utils.BitStreams {

	public partial struct BitStreamReader {

		public const int MaxDataSize = int.MaxValue / 8;
		public readonly byte[] Data;
		public readonly int BitLength;
		public readonly int AbsoluteStart; // index of first readable bit
		private ulong _prefetch; // we fetch 8 bytes at a time and mostly fiddle with that
		public int AbsoluteBitIndex {get;private set;}
		public int CurrentBitIndex {
			readonly get => AbsoluteBitIndex - AbsoluteStart;
			set {
				AbsoluteBitIndex = AbsoluteStart + value;
				Fetch();
			}
		}
		public readonly int BitsRemaining => BitLength - CurrentBitIndex;


		public BitStreamReader(byte[] data) : this(data, data.Length * 8, 0) {}


		public BitStreamReader(byte[] data, int bitLength, int start) {
			if (data.Length > MaxDataSize)
				throw new ArgumentException("input array is too long", nameof(data));
			Data = data;
			BitLength = bitLength;
			AbsoluteBitIndex = AbsoluteStart = start;
			_prefetch = 0;
			if (Data.Length > 0)
				Fetch();
		}


		// splits this stream, and returns a stream which starts at the (same) next readable bit
		public readonly BitStreamReader Split() => Split(CurrentBitIndex, BitsRemaining);


		public readonly BitStreamReader Split(uint bitLength) => Split((int)bitLength);


		public readonly BitStreamReader Split(int bitLength) => Split(CurrentBitIndex, bitLength);


		// uses relative bit index (not absolute)
		public readonly BitStreamReader Split(int fromBitIndex, int bitCount) {
			if (bitCount < 0)
				throw new ArgumentOutOfRangeException(nameof(bitCount), $"{nameof(bitCount)} cannot be less than 0");
			if (fromBitIndex + bitCount > CurrentBitIndex + BitsRemaining)
				throw new ArgumentOutOfRangeException(nameof(bitCount),
					$"{BitsRemaining} bits remaining, attempted to create a substream with {fromBitIndex + bitCount - BitsRemaining} too many bits");
			return new BitStreamReader(Data, bitCount, AbsoluteStart + fromBitIndex);
		}


		public readonly BitStreamReader FromBeginning() {
			return new BitStreamReader(Data, BitLength, AbsoluteStart);
		}


		public BitStreamReader SplitAndSkip(uint bits) => SplitAndSkip((int)bits);


		public BitStreamReader SplitAndSkip(int bits) {
			BitStreamReader ret = Split(bits);
			SkipBits(bits);
			return ret;
		}


		// use if we're at an arbitrary location (e.g. using skip)
		private unsafe void Fetch() {
			fixed (byte* pData = Data)
				_prefetch = *(ulong*)(pData + ((AbsoluteBitIndex / 8) & ~0b111));
			_prefetch >>= AbsoluteBitIndex % 64;
		}


		private unsafe void FetchIfAligned() {
			if (AbsoluteBitIndex % 64 == 0)
				fixed (byte* pData = Data)
					_prefetch = *(ulong*)(pData + AbsoluteBitIndex / 8);
		}


		// to suppress debug warnings that I didn't finish reading a component
		internal void SkipToEnd() => SkipBits(BitsRemaining);


		public void SkipBytes(int byteCount) => SkipBits(byteCount * 8);

		public void SkipBits(int bitCount) {
			EnsureCapacity(bitCount);
			AbsoluteBitIndex += bitCount;
			Fetch();
		}


		private readonly void EnsureCapacity(int bitCount) {
			if (bitCount > BitsRemaining)
				throw new ArgumentOutOfRangeException(nameof(bitCount),
					$"{nameof(EnsureCapacity)} failed - {bitCount} bits were needed but only {BitsRemaining} were left");
		}


		public bool ReadBool() {
			EnsureCapacity(1);
			bool ret = (_prefetch & 1) != 0;
			_prefetch >>= 1;
			AbsoluteBitIndex++;
			FetchIfAligned();
			return ret;
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
			if (AbsoluteBitIndex % 8 == 0) {
				// we're byte-aligned
				Data.AsSpan(AbsoluteBitIndex / 8, byteSpan.Length).CopyTo(byteSpan);
				SkipBytes(byteSpan.Length);
			} else {
				for (int i = 0; i < byteSpan.Length; i++)
					byteSpan[i] = ReadByte();
			}
		}


		public byte[] ReadBits(int bitCount) {
			byte[] result = new byte[bitCount / 8 + (bitCount % 8 == 0 ? 0 : 1)];
			ReadBitsToSpan(result.AsSpan(), bitCount);
			return result;
		}


		private void ReadBitsToSpan(Span<byte> byteSpan, int bitCount) {
			ReadBytesToSpan(byteSpan[..(bitCount / 8)]);
			if (bitCount % 8 != 0)
				byteSpan[^1] = (byte)ReadBitsAsUInt(bitCount % 8);
		}


		public (byte[] bytes, int bitCount) ReadRemainingBits() {
			int rem = BitsRemaining;
			return (ReadBits(rem), rem);
		}


		public uint ReadBitsAsUInt(uint bitCount) => ReadBitsAsUInt((int)bitCount);

		public uint ReadBitsAsUInt(int bitCount) {
			Debug.Assert(bitCount >= 0 && bitCount <= 32);
			EnsureCapacity(bitCount);
			int firstShiftCount = bitCount;
			bitCount -= 64 - AbsoluteBitIndex % 64;
			uint ret = (uint)(_prefetch & ~(0xffffffffUL << firstShiftCount));
			firstShiftCount -= Math.Max(bitCount, 0); // go at most up to the next ulong boundary
			_prefetch >>= firstShiftCount;
			AbsoluteBitIndex += firstShiftCount;
			FetchIfAligned();
			if (bitCount > 0) {
				ret |= ((uint)_prefetch & ~(0xffffffff << bitCount)) << firstShiftCount;
				_prefetch >>= bitCount;
				AbsoluteBitIndex += bitCount;
			}
			return ret;
		}


		public int ReadBitsAsSInt(uint bitCount) => ReadBitsAsSInt((int)bitCount);

		public int ReadBitsAsSInt(int bitCount) {
			int res = (int)ReadBitsAsUInt(bitCount);
			if ((res & (1 << (bitCount - 1))) != 0) // sign extend, not necessary for primitive types
				res |= int.MaxValue << bitCount; // can use int here since the leftmost 0 will get shifted away anyway
			return res;
		}


		public unsafe string ReadNullTerminatedString() {
			if (AbsoluteBitIndex % 8 == 0) {
				// we're aligned
				string str;
				fixed (byte* pBuf = Data)
					str = new string((sbyte*)pBuf + AbsoluteBitIndex / 8);
				SkipBytes(str.Length + 1);
				return str;
			}
			sbyte* pStr = stackalloc sbyte[1024];
			int i = 0;
			do
				pStr[i] = ReadSByte();
			while (pStr[i++] != 0);
			return new string(pStr);
		}


		public string ReadStringOfLength(uint strLength) => ReadStringOfLength((int)strLength);

		public unsafe string ReadStringOfLength(int strLength) {
			if (strLength < 0)
				throw new ArgumentException("bro that's not supposed to be negative", nameof(strLength));

			Span<byte> bytes = strLength < 10000
				? stackalloc byte[strLength + 1]
				: new byte[strLength + 1];

			ReadBytesToSpan(bytes[..strLength]);
			bytes[strLength] = 0; // I would assume that in this case the string might not be null-terminated.
			fixed(byte* strPtr = bytes)
				return new string((sbyte*)strPtr);
		}


		public ulong ReadULong() => ((ulong)ReadUInt() << 32) | ReadUInt();
		public uint ReadUInt() => ReadBitsAsUInt(32);
		public int ReadSInt() => (int)ReadUInt();
		public ushort ReadUShort() => (ushort)ReadBitsAsUInt(16);
		public short ReadSShort() => (short)ReadUShort();
		public EHandle ReadEHandle() => (EHandle)ReadUInt();
		public byte ReadByte() => (byte)ReadBitsAsUInt(8);
		public sbyte ReadSByte() => (sbyte)ReadByte();


		public unsafe float ReadFloat() {
			uint tmp = ReadUInt();
			return *(float*)&tmp;
		}


		public void ReadVector3(out Vector3 vec3) {
			vec3.X = ReadFloat();
			vec3.Y = ReadFloat();
			vec3.Z = ReadFloat();
		}


		// for all 'IfExists' methods - read one bit, if it's set, read the desired field

		public byte? ReadByteIfExists() => ReadBool() ? ReadByte() : (byte?) null;
		public uint? ReadUIntIfExists() => ReadBool() ? ReadUInt() : (uint?)null;
		public ushort? ReadUShortIfExists() => ReadBool() ? ReadUShort() : (ushort?)null;
		public float? ReadFloatIfExists() => ReadBool() ? ReadFloat() : (float?)null;
		public uint? ReadBitsAsUIntIfExists(int bitCount) => ReadBool() ? ReadBitsAsUInt(bitCount) : (uint?)null;
		public int? ReadBitsAsSIntIfExists(int bitCount) => ReadBool() ? ReadBitsAsSInt(bitCount) : (int?)null;


		public readonly string ToBinaryString() {
			(byte[] bytes, int bitCount) = Split().ReadRemainingBits();
			if ((bitCount & 0x07) == 0)
				return ParserTextUtils.BytesToBinaryString(bytes);
			else
				return $"{ParserTextUtils.BytesToBinaryString(bytes.Take(bytes.Length - 1))} {ParserTextUtils.ByteToBinaryString(bytes[^1], bitCount % 8)}".Trim();
		}


		public readonly string ToHexString(string separator = " ") {
			(byte[] bytes, int _) = Split().ReadRemainingBits();
			return ParserTextUtils.BytesToHexString(bytes, separator);
		}


		public override string ToString() {
			return $"index: {CurrentBitIndex}, remaining: {BitsRemaining}";
		}
	}
}
