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
		public readonly BitStreamReader Split(int fromBitIndex, int numBits) {
			if (numBits < 0)
				throw new ArgumentOutOfRangeException(nameof(numBits), $"{nameof(numBits)} cannot be less than 0");
			if (fromBitIndex + numBits > CurrentBitIndex + BitsRemaining)
				throw new ArgumentOutOfRangeException(nameof(numBits),
					$"{BitsRemaining} bits remaining, attempted to create a substream with {fromBitIndex + numBits - BitsRemaining} too many bits");
			return new BitStreamReader(Data, numBits, AbsoluteStart + fromBitIndex);
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


		// to suppress debug warnings that I didn't finish reading a component
		internal void SkipToEnd() => SkipBits(BitsRemaining);


		public void SkipBytes(int byteCount) => SkipBits(byteCount * 8);

		public void SkipBits(int numBits) {
			EnsureCapacity(numBits);
			AbsoluteBitIndex += numBits;
			Fetch();
		}


		private readonly void EnsureCapacity(int numBits) {
			if (numBits > BitsRemaining)
				throw new ArgumentOutOfRangeException(nameof(numBits),
					$"{nameof(EnsureCapacity)} failed - {numBits} bits were needed but only {BitsRemaining} were left");
		}


		public unsafe bool ReadBool() {
			EnsureCapacity(1);
			bool ret = (_prefetch & 1) != 0;
			if (++AbsoluteBitIndex % 64 == 0) { // fetch if we're on an 8-byte boundary
				fixed (byte* pData = Data)
					_prefetch = *(ulong*)(pData + AbsoluteBitIndex / 8);
			} else {
				_prefetch >>= 1;
			}
			return ret;
		}


		public byte[] ReadBytes(int byteCount) {
			Debug.Assert(byteCount >= 0);
			byte[] result = new byte[byteCount];
			ReadToSpan(result);
			return result;
		}


		public void ReadToSpan(Span<byte> span) {
			if (span.Length == 0)
				return;
			if (AbsoluteBitIndex % 8 == 0) {
				// we're byte-aligned
				Data.AsSpan(AbsoluteBitIndex / 8, span.Length).CopyTo(span);
				SkipBytes(span.Length);
			} else {
				for (int i = 0; i < span.Length; i++)
					span[i] = ReadByte();
			}
		}


		public byte[] ReadBits(int numBits) {
			byte[] result = new byte[numBits / 8 + (numBits % 8 == 0 ? 0 : 1)];
			ReadToSpan(result.AsSpan(0, numBits / 8));
			if (numBits % 8 != 0)
				result[^1] = (byte)ReadULong(numBits % 8);
			return result;
		}


		public (byte[] bytes, int numBits) ReadRemainingBits() {
			int rem = BitsRemaining;
			return (ReadBits(rem), rem);
		}


		// sign extend, not necessary for primitive types
		public int ReadSInt(int numBits) {
			int res = (int)ReadULong(numBits);
			if ((res & (1 << (numBits - 1))) != 0)
				res |= int.MaxValue << numBits; // can use int here since the leftmost 0 will get shifted away anyway
			return res;
		}


		public unsafe ulong ReadULong(int numBits = 64) {
			Debug.Assert(numBits >= 0 && numBits <= 64);
			EnsureCapacity(numBits);
			int firstNumBits = numBits;
			ulong ret = firstNumBits == 64 ? _prefetch : _prefetch & ~(ulong.MaxValue << firstNumBits);
			numBits -= 64 - AbsoluteBitIndex % 64;
			firstNumBits -= Math.Max(numBits, 0);
			AbsoluteBitIndex += firstNumBits;
			if (AbsoluteBitIndex % 64 == 0) {
				fixed (byte* pData = Data)
					_prefetch = *(ulong*)(pData + AbsoluteBitIndex / 8);
				if (numBits > 0) {
					ret |= (_prefetch & ~(ulong.MaxValue << numBits)) << firstNumBits;
					_prefetch >>= numBits;
					AbsoluteBitIndex += numBits;
				}
			} else {
				_prefetch >>= firstNumBits;
			}
			return ret;
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
			// Demos have on the order of tens or hundreds of thousands of strings, this is a personal exercise to
			// optimize reading them. Read a full ulong at a time and check each byte of it.
			const int maxSize = 1024;
			ulong* pStr = stackalloc ulong[maxSize / 8];
			for (int i = 0; i < maxSize / 8; i++) {
				int actualReadCount = Math.Min(BitsRemaining, 64);
				ulong cur = pStr[i] = ReadULong(actualReadCount);
				ulong mask = 0xFF;
				for (int j = 1; j < 8; j++) {
					if ((cur & mask) == 0) {
						// we've read too many bytes, backtrack
						AbsoluteBitIndex -= actualReadCount - 8 * j;
						Fetch();
						return new string((sbyte*)pStr);
					}
					mask <<= 8;
				}
				if ((cur & mask) == 0) // the last byte in cur is a '\0', no backtracking necessary
					return new string((sbyte*)pStr);
			}
			throw new IndexOutOfRangeException($"{nameof(BitStreamReader)}.{nameof(ReadNullTerminatedString)} " +
											   $"tried to read a string that's more than {maxSize} bytes long");
		}

		public unsafe string ReadStringOfLength(int strLen) {
			Debug.Assert(strLen >= 0);
			if (strLen == 0)
				return string.Empty;

			Span<byte> bytes = AbsoluteBitIndex % 8 == 0
				? Data.AsSpan(AbsoluteBitIndex / 8, strLen)
				: stackalloc byte[strLen]; // this stackalloc only works inside this ternary operator for some reason

			if (AbsoluteBitIndex % 8 == 0)
				SkipBytes(strLen);
			else
				ReadToSpan(bytes);

			int terminator = bytes.IndexOf((byte)0);
			fixed (byte* strPtr = bytes)
				return new string((sbyte*)strPtr, 0, terminator == -1 ? strLen : terminator);
		}


		public uint ReadUInt(int numBits) => (uint)ReadULong(numBits);
		public uint ReadUInt() => (uint)ReadULong(32);
		public int ReadSInt() => (int)ReadULong(32);
		public ushort ReadUShort() => (ushort)ReadULong(16);
		public short ReadSShort() => (short)ReadULong(16);
		public EHandle ReadEHandle() => (EHandle)ReadUInt();
		public byte ReadByte() => (byte)ReadULong(8);
		public sbyte ReadSByte() => (sbyte)ReadULong(8);


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
		public uint? ReadUIntIfExists(int numBits) => ReadBool() ? ReadUInt(numBits) : (uint?)null;
		public int? ReadSIntIfExists(int numBits) => ReadBool() ? ReadSInt(numBits) : (int?)null;


		public readonly string ToBinaryString() {
			(byte[] bytes, int numBits) = Split().ReadRemainingBits();
			if ((numBits & 0x07) == 0)
				return ParserTextUtils.BytesToBinaryString(bytes);
			else
				return $"{ParserTextUtils.BytesToBinaryString(bytes.Take(bytes.Length - 1))} {ParserTextUtils.ByteToBinaryString(bytes[^1], numBits % 8)}".Trim();
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
