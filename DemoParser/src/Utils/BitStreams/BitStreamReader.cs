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
		public readonly int BitsRemaining => HasOverflowed ? 0 : BitLength - CurrentBitIndex;

		// Used for informative messages, better (and faster) than exceptions. If we're overflowed we just return
		// default values. It's the responsibility of callers to check the overflow flag.
		public bool HasOverflowed {get;private set;}


		public BitStreamReader(byte[] data) : this(data, data.Length * 8, 0) {}


		public BitStreamReader(byte[] data, int bitLength, int start) : this(data, bitLength, start, false) {}


		private BitStreamReader(byte[] data, int bitLength, int start, bool hasOverflowed) {
			if (data.Length > MaxDataSize)
				throw new IndexOutOfRangeException($"input array is longer than {MaxDataSize} bytes");
			Data = data;
			BitLength = bitLength;
			AbsoluteBitIndex = AbsoluteStart = start;
			_prefetch = 0;
			HasOverflowed = hasOverflowed;
			if (!HasOverflowed) {
				if (start < 0 || bitLength < 0 || (start + bitLength) / 8 > Data.Length)
					HasOverflowed = true;
				else if (Data.Length > 0)
					Fetch();
			}
		}


		public void SetOverflow() => HasOverflowed = true;


		public readonly BitStreamReader Fork() => Fork(BitsRemaining);

		public readonly BitStreamReader Fork(int bitLength) => Fork(CurrentBitIndex, bitLength);


		// Splits this stream, and returns a stream which starts at the (same) next readable bit.
		// Uses the current (relative) bit index, not the absolute one.
		public readonly BitStreamReader Fork(int fromBitIndex, int numBits)
			=> new BitStreamReader(Data, numBits, AbsoluteStart + fromBitIndex, HasOverflowed);


		public readonly BitStreamReader FromBeginning()
			=> new BitStreamReader(Data, BitLength, AbsoluteStart);


		public BitStreamReader ForkAndSkip(int bits) {
			BitStreamReader ret = Fork(bits);
			SkipBits(bits);
			return ret;
		}


		// use if we're at an arbitrary location (e.g. using skip)
		private unsafe void Fetch() {
			fixed (byte* pData = Data)
				_prefetch = *(ulong*)(pData + ((AbsoluteBitIndex / 8) & ~0b111));
			_prefetch >>= AbsoluteBitIndex % 64;
		}


		public void SkipBytes(int byteCount) => SkipBits(byteCount * 8);

		public void SkipBits(int numBits) {
			if (numBits > BitsRemaining || numBits < 0) // never 'skip' backwards
				HasOverflowed = true;
			if (HasOverflowed)
				return;
			AbsoluteBitIndex += numBits;
			Fetch();
		}


		public unsafe bool ReadBool() {
			if (BitsRemaining < 1)
				HasOverflowed = true;
			if (HasOverflowed)
				return false;

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
			if (byteCount < 0 || byteCount * 8 > BitsRemaining)
				HasOverflowed = true;
			if (HasOverflowed)
				return Array.Empty<byte>();
			byte[] result = new byte[byteCount];
			ReadToSpan(result);
			return result;
		}


		public void ReadToSpan(Span<byte> span) {
			if (span.Length == 0 || HasOverflowed)
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
			if (numBits > BitsRemaining)
				HasOverflowed = true;
			if (HasOverflowed)
				return 0;

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
			if (HasOverflowed)
				return string.Empty;

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
			for (int i = 0; i < maxSize / 8 && BitsRemaining > 0; i++) {
				int readByteCount = Math.Min(BitsRemaining / 8, 8);
				ulong cur = pStr[i] = ReadULong(readByteCount * 8);
				ulong mask = 0xFF;
				for (int j = 0; j < readByteCount; j++) {
					if ((cur & mask) == 0) {
						if (j < readByteCount - 1) {
							// we've read too many bytes, backtrack
							AbsoluteBitIndex -= 8 * (readByteCount - j - 1);
							Fetch();
						}
						return new string((sbyte*)pStr);
					}
					mask <<= 8;
				}
			}
			HasOverflowed = true;
			return string.Empty;
		}


		public unsafe string ReadStringOfLength(int strLen) {
			if (strLen < 0)
				HasOverflowed = true;
			if (strLen == 0 || HasOverflowed)
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
			(byte[] bytes, int numBits) = Fork().ReadRemainingBits();
			if ((numBits & 0x07) == 0)
				return ParserTextUtils.BytesToBinaryString(bytes);
			else
				return $"{ParserTextUtils.BytesToBinaryString(bytes.Take(bytes.Length - 1))} {ParserTextUtils.ByteToBinaryString(bytes[^1], numBits % 8)}".Trim();
		}


		public readonly string ToHexString(string separator = " ") {
			(byte[] bytes, int _) = Fork().ReadRemainingBits();
			return ParserTextUtils.BytesToHexString(bytes, separator);
		}


		public override string ToString() {
			if (HasOverflowed)
				return "overflowed";
			return $"index: {CurrentBitIndex}, remaining: {BitsRemaining}";
		}
	}
}
