using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace DemoParser.Utils.BitStreams {
	
	// because I don't use this nearly as much as the reader atm, some of the methods may be poorly or incorrectly implemented
	public partial class BitStreamWriter {
		
		public int BitLength {get; private set;}
		private List<byte> _data;
		public byte[] AsArray => _data.ToArray();
		private int IndexInByte => BitLength & 0x07;
		private bool IsByteAligned => IndexInByte == 0;
		public int ByteLength => (BitLength >> 3) + (IsByteAligned ? 0 : 1);
		internal bool IsLittleEndian; // this doesn't work w/ big endian atm, probably won't try to fix it since it's not necessary
		
		
		public BitStreamWriter(int initialByteCapacity, bool isLittleEndian = true) {
			_data = new List<byte>(initialByteCapacity);
			IsLittleEndian = isLittleEndian;
			BitLength = 0;
		}
		

		public BitStreamWriter(bool isLittleEndian = true) : this(10, isLittleEndian) {}


		public BitStreamWriter(byte[] data, bool isLittleEndian = true) : this(data.Length, isLittleEndian) {
			WriteBytes(data);
		}
		
		

		public string ToBinaryString() {
			if (IsByteAligned)
				return ParserTextUtils.BytesToBinaryString(_data);
			else
				return ParserTextUtils.BytesToBinaryString(_data.Take(_data.Count - 1)) + ParserTextUtils.ByteToBinaryString(_data[^1], IndexInByte);
		}


		public void WriteBitStreamReader(BitStreamReader reader, bool writeUntilByteBoundary) {
			(byte[] bytes, int bitCount) = reader.ReadRemainingBits();
			WriteBits(bytes, bitCount);
			if (writeUntilByteBoundary)
				WriteUntilByteBoundary();
		}


		public void WriteUntilByteBoundary() {
			if (!IsByteAligned)
				BitLength += 8 - IndexInByte; // maybe wrong idk
		}


		public void WriteBits(byte[] bytes, int bitCount) { // size of bytes will be reduced to bitCount / 8 if bitCount % 8 == 0
			if (bitCount < 0)
				throw new ArgumentOutOfRangeException(nameof(bitCount), $"{nameof(bitCount)} cannot be less than 0");
			if (Math.Ceiling(bitCount / 8.0f) > bytes.Length)
				throw new ArgumentOutOfRangeException(nameof(bitCount), "the index is out of range of the provided array");
			if ((bitCount & 0x07) == 0) { // multiple of bytes
				Array.Resize(ref bytes, bitCount >> 3);
				WriteBytes(bytes);
			} else {
				for (int i = 0; i < bitCount >> 3; i++)
					WriteByte(bytes[i]);
				if (IsByteAligned)
					_data.Add(0);
				byte lastByteToWrite = bytes[bitCount >> 3];
				if ((bitCount & 0x07) <= 8 - IndexInByte) { // if remaining bits don't cross a byte boundary
					// whatever this garbage is it could be improved
					_data[^1] |= (byte)((lastByteToWrite << IndexInByte) & (0xff << IndexInByte) & (0xff >> (8 - (IndexInByte + (bitCount & 0x07)))));
				} else {
					int shiftingOffset = IndexInByte;
					_data[^1] |= (byte)(((0xff >> IndexInByte) & lastByteToWrite) << IndexInByte);
					_data.Add((byte)((lastByteToWrite >> (8 - shiftingOffset)) & (0xff >> (16 - shiftingOffset - (bitCount & 0x07)))));
				}
				BitLength += bitCount & 0x07;
			}
		}


		public void WriteBits((byte[] bytes, int bitCount) t) => WriteBits(t.bytes, t.bitCount);


		public void WriteBitsFromSInt(int i, int bitCount) {
			i |= (i & (1 << 31)) >> (32 - bitCount);
			WriteBitsFromUInt((uint)i, bitCount);
		}


		public void WriteBitsFromUInt(uint i, int bitCount) {
			byte[] bytes = BitConverter.GetBytes(i);
			if (BitConverter.IsLittleEndian ^ IsLittleEndian) // this might be the opposite
				Array.Reverse(bytes);
			WriteBits(bytes, bitCount);
		}


		public void WriteBytes(byte[] bytes) {
			if (IsByteAligned) {
				_data.AddRange(bytes);
				BitLength += bytes.Length << 3;
			} else {
				foreach (byte b in bytes)
					WriteByte(b);
			}
		}
		

		public void WriteByte(byte b) {
			if (IsByteAligned) {
				_data.Add(b);
			} else {
				int bitsInNextByte = IndexInByte;
				int bitsInCurrentByte = 8 - bitsInNextByte;
				_data[^1] |= (byte)(((0xff >> bitsInNextByte) & b) << bitsInNextByte);
				_data.Add((byte)(((0xff << bitsInCurrentByte) & b) >> bitsInCurrentByte));
			}
			BitLength += 8;
		}
		

		public void WriteBool(bool b) {
			if (IsByteAligned)
				_data.Add((byte)(b ? 1 : 0));
			else
				_data[^1] |= (byte)((1 << IndexInByte) & (b ? 0xff : 0));
			BitLength++;
		}
		

		public void WriteString(string str, bool nullTerminate = true) {
			WriteBytes(Encoding.ASCII.GetBytes(str));
			if (nullTerminate)
				WriteByte(0);
		}
		

		private void WritePrimitive<T>(Func<T, byte[]> bitConverterFunc, T primitive) {
			byte[] bytes = bitConverterFunc(primitive);
			if (BitConverter.IsLittleEndian ^ IsLittleEndian)
				Array.Reverse(bytes);
			WriteBytes(bytes);
		}
		

		public void WriteUInt(uint i) {
			WritePrimitive(BitConverter.GetBytes, i);
		}
		
		
		public void WriteSInt(int i) {
			WritePrimitive(BitConverter.GetBytes, i);
		}
		

		public void WriteShort(short s) {
			WritePrimitive(BitConverter.GetBytes, s);
		}
		

		public void WriteFloat(float f) {
			WritePrimitive(BitConverter.GetBytes, f);
		}


		public void WriteVector3(Vector3 vector3) {
			WriteFloat(vector3.X);
			WriteFloat(vector3.Y);
			WriteFloat(vector3.Z);
		}

		
		// for all 'IfExists' methods, writes a bool if the field exists before the field itself
		
		
		private void WritePrimitiveIfExists<T>(Action<T> writeFunc, T? primitive) where T : struct {
			if (primitive.HasValue) {
				WriteBool(true);
				writeFunc(primitive.Value);
			} else {
				WriteBool(false);
			}
		}

		
		public void WriteUIntIfExists(uint? i) {
			WritePrimitiveIfExists(WriteUInt, i);
		}


		public void WriteShortIfExists(short? s) {
			WritePrimitiveIfExists(WriteShort, s);
		}


		public void WriteFloatIfExists(float? f) {
			WritePrimitiveIfExists(WriteFloat, f);
		}


		// this does not write the 'exists' bit since the only cases where this is an optional field (in string table)
		// do not use the bit, and instead have the bit set only for the size of the byte array before the array itself if it exists
		public void WriteByteArrIfExists(byte[] b) {
			if (b != null) 
				WriteBytes(b);
		}

		
		public void WriteBitsIfExists(byte[] b, int bitCount) {
			if (b != null) {
				WriteBool(true);
				WriteBits(b, bitCount);
			} else {
				WriteBool(false);
			}
		}


		public void WriteStringIfExists(string str) {
			if (str != null) {
				WriteBool(true);
				WriteString(str);
			} else {
				WriteBool(false);
			}
		}


		public void WriteBitsFromUIntIfExists(uint? i, int bitCount) {
			if (i != null) {
				WriteBool(true);
				WriteBitsFromUInt(i.Value, bitCount);
			} else {
				WriteBool(false);
			}
		}


		public override string ToString() {
			//return ParserTextUtils.BytesToBinaryString(_data);
			return "bit length: " + BitLength;
			/*if (IsByteAligned) {
				return ParserTextUtils.BytesToBinaryString(_data);
			} else {
				return ParserTextUtils.BytesToBinaryString(_data.Take(_data.Count - 1)) 
					   + " " + ParserTextUtils.ByteToBinaryString(_data.Last(), IndexInByte);
			}*/
		}
	}
}