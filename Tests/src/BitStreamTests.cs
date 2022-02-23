using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Utils.BitStreams;
using NUnit.Framework;

namespace Tests {

	public class BitStreamTests {

		// It's really hard to come up with all possible alignments and sizes for which a particular read/write may
		// happen. The easiest solution is to just do thousands of reads/writes on random data and hope that catches all
		// edge cases.
		private const int Iterations = 100000;


		private static string RandomString(Random r, int length) {
			return new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", length).Select(s => s[r.Next(s.Length)]).ToArray());
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void WriteBools() {
			var random = new Random(0);
			BitStreamWriter bsw = new BitStreamWriter();
			List<bool> bools = new List<bool>();
			for (int _ = 0; _ < Iterations; _++)  {
				bool r = random.NextDouble() >= .5;
				bools.Add(r);
				bsw.WriteBool(r);
			}
			BitStreamReader bsr = new BitStreamReader(bsw);
			for (int i = 0; i < Iterations; i++)
				Assert.AreEqual(bools[i], bsr.ReadBool(), $"index: {i}");
			Assert.IsFalse(bsr.HasOverflowed);
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void WriteUInts() {
			var random = new Random(0);
			BitStreamWriter bsw = new BitStreamWriter();
			List<(uint val, int size)> bits = new List<(uint val, int size)>();

			for (int _ = 0; _ < Iterations; _++)  {
				(uint val, int size) r = ((uint)random.Next(), random.Next(1, 33));
				r.val &= uint.MaxValue >> (32 - r.size);
				bits.Add(r);
				bsw.WriteBitsFromUInt(r.val, r.size);
			}
			BitStreamReader bsr = new BitStreamReader(bsw);
			for (int i = 0; i < Iterations; i++)
				Assert.AreEqual(bits[i].val, bsr.ReadUInt(bits[i].size), $"index: {i}");
			Assert.IsFalse(bsr.HasOverflowed);
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void WriteSInts() {
			var random = new Random(0);
			BitStreamWriter bsw = new BitStreamWriter();
			List<(int val, int size)> bits = new List<(int val, int size)>();

			for (int _ = 0; _ < Iterations; _++)  {
				(int val, int size) r = (random.Next(), random.Next(1, 33));
				r.val &= int.MaxValue >> (32 - r.size);
				if ((r.val & (1 << (r.size - 1))) != 0) // sign extend, not necessary for primitive types
					r.val |= int.MaxValue << r.size;    // can use int here since the leftmost 0 will get shifted away anyway
				bits.Add(r);
				bsw.WriteBitsFromSInt(r.val, r.size);
			}
			BitStreamReader bsr = new BitStreamReader(bsw);
			for (int i = 0; i < Iterations; i++)
				Assert.AreEqual(bits[i].val, bsr.ReadSInt(bits[i].size), $"index: {i}");
			Assert.IsFalse(bsr.HasOverflowed);
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void WriteUIntsIfExists() {
			var random = new Random(0);
			BitStreamWriter bsw = new BitStreamWriter();
			List<(uint? val, int size)> bits = new List<(uint? val, int size)>();

			for (int _ = 0; _ < Iterations; _++)  {
				bool rBool = random.NextDouble() >= 0.5;
				(uint? val, int size) r = (rBool ? (uint?)null : (uint)random.Next(), random.Next(1, 33));
				r.val &= uint.MaxValue >> (32 - r.size);
				bits.Add(r);
				bsw.WriteBitsFromUIntIfExists(r.val, r.size);
			}
			BitStreamReader bsr = new BitStreamReader(bsw);
			for (int i = 0; i < Iterations; i++)
				Assert.AreEqual(bits[i].val, bsr.ReadUIntIfExists(bits[i].size), $"index: {i}");
			Assert.IsFalse(bsr.HasOverflowed);
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void SkipBits() {
			var random = new Random(0);
			BitStreamWriter bsw = new BitStreamWriter();
			List<(int skipCount, uint val)> data = new List<(int skipCount, uint val)>();
			byte[] randArr = new byte[100 / 8 + 1];
			random.NextBytes(randArr);
			for (int _ = 0; _ < Iterations; _++) {
				(int skip, uint val) r = (random.Next(0, 100), (uint)random.Next());
				data.Add(r);
				bsw.WriteBits(randArr, r.skip);
				bsw.WriteUInt(r.val);
			}
			BitStreamReader bsr = new BitStreamReader(bsw);
			for (int i = 0; i < Iterations; i++) {
				bsr.SkipBits(data[i].skipCount);
				Assert.AreEqual(data[i].val, bsr.ReadUInt(), $"index: {i}");
			}
			Assert.IsFalse(bsr.HasOverflowed);
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void WriteBitCoord() {
			var random = new Random(0);
			for (int i = 0; i < Iterations; i++) {
				int skip = i % 16;
				BitStreamWriter bsw = new BitStreamWriter();
				bsw.WriteBitsFromSInt(random.Next(), skip);
				bsw.WriteBitCoord((float)((random.NextDouble() - 0.5) * 100000));
				BitStreamReader bsr = new BitStreamReader(bsw);
				bsr.SkipBits(skip);
				// Write the float back and then read it again, this will constrain it to the precision of a
				// bit coord so we can do a direct equality check.
				float f = bsr.ReadBitCoord();
				// Now do all the same stuff we just did but with the constrained float.
				bsw = new BitStreamWriter();
				bsw.WriteBitsFromSInt(random.Next(), skip);
				bsw.WriteBitCoord(f);
				bsr = new BitStreamReader(bsw);
				bsr.SkipBits(skip);
				Assert.AreEqual(f, bsr.ReadBitCoord(), $"index: {i}");
				Assert.IsFalse(bsr.HasOverflowed);
			}
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void WriteBits() {
			var random = new Random(0);
			for (int i = 0; i < Iterations; i++) {
				int skip = i % 16;
				BitStreamWriter bsw = new BitStreamWriter();
				bsw.WriteBitsFromSInt(random.Next(), skip);
				int bitLen = random.Next(1, 100);
				byte[] rand = new byte[(bitLen >> 3) + ((bitLen & 0x07) == 0 ? 0 : 1)];
				random.NextBytes(rand);
				bsw.WriteBits(rand, bitLen);
				if ((bitLen & 0x07) != 0)
					rand[^1] &= (byte)(0xff >> (8 - (bitLen & 0x07))); // get rid of unused bits
				BitStreamReader bsr = new BitStreamReader(bsw);
				bsr.SkipBits(skip);
				CollectionAssert.AreEqual(rand, bsr.ReadBits(bitLen), $"index: {i}, bitlen: {bitLen}");
				Assert.IsFalse(bsr.HasOverflowed);
			}
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void EditBits() {
			var random = new Random(0);
			for (int i = 0; i < Iterations; i++) {
				int skipBefore = i % 16;
				int skipAfter = i % 17 % 16;
				BitStreamWriter bsw = new BitStreamWriter();
				bsw.WriteBitsFromSInt(random.Next(), skipBefore);
				int bitLen = random.Next(1, 100);
				byte[] rand = new byte[(bitLen >> 3) + ((bitLen & 0x07) == 0 ? 0 : 1)];
				random.NextBytes(rand);
				bsw.WriteBits(rand, bitLen);
				bsw.WriteBitsFromSInt(random.Next(), skipAfter);
				// now we have written some random data to a fixed location, let's edit it
				random.NextBytes(rand);
				bsw.EditBitsAtIndex(rand, skipBefore, bitLen);
				if ((bitLen & 0x07) != 0)
					rand[^1] &= (byte)(0xff >> (8 - (bitLen & 0x07))); // get rid of unused bits
				// okay, now lets read and compare to the rand array
				BitStreamReader bsr = new BitStreamReader(bsw);
				bsr.SkipBits(skipBefore);
				CollectionAssert.AreEqual(rand, bsr.ReadBits(bitLen), $"index: {i}, bitlen: {bitLen}");
				Assert.IsFalse(bsr.HasOverflowed);
			}
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void RemoveBits() {
			var random = new Random(0);
			for (int i = 0; i < Iterations; i++) {
				int skip = i % 16;
				int testIntBits = random.Next(1, 32);
				int testInt = random.Next(0, 1 << (testIntBits - 1));
				BitStreamWriter bsw = new BitStreamWriter();
				bsw.WriteBitsFromSInt(random.Next(), skip);
				byte[] rand = new byte[10];
				random.NextBytes(rand);
				int randCount = random.Next(1, 80);
				bsw.WriteBits(rand, randCount);
				bsw.WriteBitsFromSInt(testInt, testIntBits);
				bsw.RemoveBitsAtIndex(skip, randCount);
				BitStreamReader bsr = new BitStreamReader(bsw);
				bsr.SkipBits(skip);
				Assert.AreEqual(testInt, bsr.ReadSInt(testIntBits), $"index: {i}");
				Assert.IsFalse(bsr.HasOverflowed);
			}
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void WriteNullTerminatedStrings() {
			var random = new Random(0);
			List<(int skip, string str)> data = new List<(int skip, string str)>();
			BitStreamWriter bsw = new BitStreamWriter();
			byte[] randArr = new byte[100 / 8 + 1];
			random.NextBytes(randArr);
			for (int _ = 0; _ < Iterations; _++) {
				(int skip, string str) r = (random.Next(0, 100), RandomString(random, random.Next(0, 15)));
				data.Add(r);
				bsw.WriteBits(randArr, r.skip);
				bsw.WriteString(r.str);
			}
			BitStreamReader bsr = new BitStreamReader(bsw);
			for (int i = 0; i < Iterations; i++) {
				bsr.SkipBits(data[i].skip);
				Assert.AreEqual(data[i].str, bsr.ReadNullTerminatedString(), $"index: {i}");
			}
			Assert.IsFalse(bsr.HasOverflowed);

			// ReadNullTerminatedString fetches 8 bytes at a time when it's not aligned, this is a special check to make
			// sure that it keeps track of the index correctly (which the above code may have missed).

			const bool test1 = true;
			const string test2 = "The FitnessGram Pacer Test is a multistage aerobic capacity test that progressively gets more difficult as it continues.";
			const byte test3 = 69;
			bsw = new BitStreamWriter();
			bsw.WriteBool(test1);
			bsw.WriteString(test2);
			bsw.WriteByte(test3);
			bsr = new BitStreamReader(bsw);
			Assert.AreEqual(test1, bsr.ReadBool());
			Assert.AreEqual(test2, bsr.ReadNullTerminatedString());
			Assert.AreEqual(test3, bsr.ReadByte());
			Assert.IsFalse(bsr.HasOverflowed);
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void WriteStringsOfLength() {
			var random = new Random(0);
			List<(int skip, string str)> data = new List<(int skip, string str)>();
			BitStreamWriter bsw = new BitStreamWriter();
			byte[] randArr = new byte[100 / 8 + 1];
			random.NextBytes(randArr);
			for (int _ = 0; _ < Iterations; _++) {
				(int skip, string str) = (random.Next(0, 100), RandomString(random, random.Next(0, 15)));
				// give some of these strings a null terminator in the middle (which we don't expect to see)
				if (random.NextDouble() > 0.5)
					str += "\0tactical spoon";
				bsw.WriteBits(randArr, skip);
				bsw.WriteString(str, false);
				data.Add((skip, str));
			}
			BitStreamReader bsr = new BitStreamReader(bsw);
			for (int i = 0; i < Iterations; i++) {
				bsr.SkipBits(data[i].skip);
				int terminator = data[i].str.IndexOf('\0');
				string actual = terminator == -1 ? data[i].str : data[i].str[..terminator];
				Assert.AreEqual(actual, bsr.ReadStringOfLength(data[i].str.Length), $"index: {i}");
			}
			Assert.IsFalse(bsr.HasOverflowed);
		}


		[Test, Parallelizable(ParallelScope.Self)]
		public void CheckReaderOverflow() {
			BitStreamReader bsr = new BitStreamReader(new byte[] {2});
			for (int i = 0; i < 2; i++) {
				bsr.ReadULong(20);
				// best thing I can think of is that none of these should throw exceptions
				bsr.ReadNullTerminatedString();
				bsr.ReadStringOfLength(6);
				bsr.ReadULong();
				bsr.ReadBits(1);
				bsr.ReadBool();
				bsr.ReadFloat();
				bsr.ReadBitAngle(20);
				bsr.ReadToSpan(new byte[5]);
				bsr.ReadVarUInt32();
				bsr.ReadBitCoord();
				bsr.ReadBytes(5);
				bsr.ReadBytes(-1);
			}
			Assert.That(bsr.HasOverflowed);
		}
	}
}
