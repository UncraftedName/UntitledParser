using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace UncraftedDemoParser.Utils {
	
	public class BitFieldWriter {

		public int Pointer {get; private set;}
		private readonly List<byte> _data;
		public byte[] Data => _data.ToArray();
		private int BitIndex => Pointer & 0x07;
		private bool IsByteAligned => BitIndex == 0;
		public int SizeInBytes => Pointer >> 3;
		
		
		public BitFieldWriter(int initialCapacity) {
			_data = new List<byte>(initialCapacity);
			Pointer = 0;
		}
		

		public BitFieldWriter() : this(10) {}
		
		
		public void WriteBits(byte[] bytes, int bitCount) { // size of bytes will be reduced to bitCount / 8 if bitCount % 8 == 0
			if (Math.Ceiling(bitCount / 8.0f) > bytes.Length || bitCount < 0)
				throw new IndexOutOfRangeException();
			if ((bitCount & 0x07) == 0) { // multiple of bytes
				Array.Resize(ref bytes, bitCount >> 3);
				WriteBytes(bytes);
			} else {
				for (int i = 0; i < bitCount >> 3; i++)
					WriteByte(bytes[i]);
				byte lastByte = bytes[bytes.Length - 1];
				if ((bitCount & 0x07) < 8 - BitIndex) { // if remaining bits don't cross a byte boundary
					int bitsInCurrentByte = Math.Min(8 - BitIndex, bitCount & 0x07);
					int shiftingOffset = 8 - bitsInCurrentByte;
					_data[_data.Count - 1] |= (byte)(((0xff >> shiftingOffset) & lastByte) << shiftingOffset);
				} else {
					int shiftingOffset = BitIndex;
					_data[_data.Count - 1] |= (byte)(((0xff >> shiftingOffset) & lastByte) << shiftingOffset);
					_data.Add((byte)(lastByte & (0xff >> (8 - (bitCount & 0x07))) & (0xff << (8 - shiftingOffset))));
				}
				Pointer += bitCount & 0x07;
			}
		}


		public void WriteBits(Tuple<byte[], int> bytes) {
			WriteBits(bytes.Item1, bytes.Item2);
		}


		public void WriteBitsFromInt(int i, int bitCount) {
			byte[] bytes = BitConverter.GetBytes(i);
			if (BitConverter.IsLittleEndian) // this might be the opposite
				Array.Reverse(bytes);
			WriteBits(bytes, bitCount);
		}
		

		public void WriteBytes(byte[] bytes) {
			if (IsByteAligned) {
				_data.AddRange(bytes);
				Pointer += bytes.Length << 3;
			} else {
				foreach (byte b in bytes)
					WriteByte(b);
			}
		}
		

		public void WriteByte(byte b) {
			if (IsByteAligned) {
				_data.Add(b);
			} else {
				int bitsInNextByte = BitIndex;
				int bitsInCurrentByte = 8 - bitsInNextByte;
				_data[_data.Count - 1] |= (byte)(((0xff >> bitsInNextByte) & b) << bitsInNextByte);
				_data.Add((byte)(((0xff << bitsInCurrentByte) & b) >> bitsInCurrentByte));
			}
			Pointer += 8;
		}
		

		public void WriteBool(bool b) {
			if (IsByteAligned)
				_data.Add((byte)(b ? 1 : 0));
			else
				_data[_data.Count - 1] |= (byte)((1 << BitIndex) & (b ? 0xff : 0));
			Pointer++;
		}
		

		public void WriteString(string str, bool nullTerminate = true) {
			WriteBytes(Encoding.ASCII.GetBytes(str));
			if (nullTerminate)
				WriteByte(0);
		}
		

		private void WritePrimitive<T>(Func<T, byte[]> bitConverterFunc, T primitive) {
			byte[] bytes = bitConverterFunc(primitive);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(bytes);
			WriteBytes(bytes);
		}
		

		public void WriteInt(int i) {
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

		
		public void WriteIntIfExists(int? i) {
			WritePrimitiveIfExists(WriteInt, i);
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


		public void WriteBitsFromIntIfExists(int? i, int bitCount) {
			if (i != null) {
				WriteBool(true);
				WriteBitsFromInt(i.Value, bitCount);
			} else {
				WriteBool(false);
			}
		}
	}
}