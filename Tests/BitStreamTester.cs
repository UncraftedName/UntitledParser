using System;
using System.Collections.Generic;
using DemoParser.Utils.BitStreams;
using NUnit.Framework;

namespace Tests {
	
	public class BitStreamTester {

		private const int Iterations = 100000;
		private Random _random;
		

		[SetUp]
		public void Setup() {
			_random = new Random(0);
		}
		

		[Test]
		public void WriteBools() {
			BitStreamWriter bsw = new BitStreamWriter();
			List<bool> bools = new List<bool>();
			for (int _ = 0; _ < Iterations; _++)  {
				bool r = _random.NextDouble() >= .5;
				bools.Add(r);
				bsw.WriteBool(r);
			}
			BitStreamReader bsr = new BitStreamReader(bsw.AsArray);
			for (int i = 0; i < Iterations; i++) 
				Assert.AreEqual(bools[i], bsr.ReadBool(), "index: " + i);
		}
		

		[Test]
		public void WriteUBits() {
			BitStreamWriter bsw = new BitStreamWriter();
			List<(uint val, int size)> bits = new List<(uint val, int size)>();
			
			for (int _ = 0; _ < Iterations; _++)  {
				(uint val, int size) r = ((uint)_random.Next(), _random.Next(1, 33));
				r.val &= uint.MaxValue >> (32 - r.size);
				bits.Add(r);
				bsw.WriteBitsFromInt(r.val, r.size);
			}
			BitStreamReader bsr = new BitStreamReader(bsw.AsArray);
			for (int i = 0; i < Iterations; i++) 
				Assert.AreEqual(bits[i].val, bsr.ReadBitsAsUInt(bits[i].size), "index: " + i);
		}
		
		
		[Test]
		public void WriteSInts() {
			BitStreamWriter bsw = new BitStreamWriter();
			List<(int val, int size)> bits = new List<(int val, int size)>();
			
			for (int _ = 0; _ < Iterations; _++)  {
				(int val, int size) r = (_random.Next(), _random.Next(1, 33));
				r.val &= int.MaxValue >> (32 - r.size);
				if ((r.val & (1 << (r.size - 1))) != 0) // sign extend, not necessary for primitive types
					r.val |= int.MaxValue << r.size;    // can use int here since the leftmost 0 will get shifted away anyway
				bits.Add(r);
				bsw.WriteBitsFromInt(r.val, r.size);
			}
			BitStreamReader bsr = new BitStreamReader(bsw.AsArray);
			for (int i = 0; i < Iterations; i++) 
				Assert.AreEqual(bits[i].val, bsr.ReadBitsAsSInt(bits[i].size), "index: " + i);
		}


		[Test]
		public void WriteUIntsIfExists() {
			BitStreamWriter bsw = new BitStreamWriter();
			List<(uint? val, int size)> bits = new List<(uint? val, int size)>();
			
			for (int _ = 0; _ < Iterations; _++)  {
				bool rBool = _random.NextDouble() >= 0.5;
				(uint? val, int size) r = (rBool ? (uint?)null : (uint)_random.Next(), _random.Next(1, 33));
				r.val &= uint.MaxValue >> (32 - r.size);
				bits.Add(r);
				bsw.WriteBitsFromUIntIfExists(r.val, r.size);
			}
			BitStreamReader bsr = new BitStreamReader(bsw.AsArray);
			for (int i = 0; i < Iterations; i++) 
				Assert.AreEqual(bits[i].val, bsr.ReadBitsAsUIntIfExists(bits[i].size), "index: " + i);
		}


		[Test]
		public void WriteBitCoord() {
			for (int i = 0; i < Iterations; i++) {
				int skip = i % 16;
				BitStreamWriter bsw = new BitStreamWriter();
				bsw.WriteBitsFromInt(_random.Next(), skip);
				bsw.WriteBitCoord((float)((_random.NextDouble() - 0.5) * 100000));
				BitStreamReader bsr = new BitStreamReader(bsw.AsArray);
				bsr.SkipBits(skip);
				// Write the float back and then read it again, this will constrain it to the precision of a 
				// bit coord so we can do a direct equality check.
				float f = bsr.ReadBitCoord();
				// Now do all the same stuff we just did but with the constrained float.
				bsw = new BitStreamWriter();
				bsw.WriteBitsFromInt(_random.Next(), skip);
				bsw.WriteBitCoord(f);
				bsr = new BitStreamReader(bsw.AsArray);
				bsr.SkipBits(skip);
				Assert.AreEqual(bsr.ReadBitCoord(), f, "index: " + i);
			}
		}


		[Test]
		public void WriteBits() {
			for (int i = 0; i < Iterations; i++) {
				int skip = i % 16;
				BitStreamWriter bsw = new BitStreamWriter();
				bsw.WriteBitsFromInt(_random.Next(), skip);
				int bitLen = _random.Next(1, 100);
				byte[] rand = new byte[(bitLen >> 3) + ((bitLen & 0x07) == 0 ? 0 : 1)];
				_random.NextBytes(rand);
				bsw.WriteBits(rand, bitLen);
				if ((bitLen & 0x07) != 0)
					rand[^1] &= (byte)(0xff >> (8 - (bitLen & 0x07))); // get rid of unused bits
				BitStreamReader bsr = new BitStreamReader(bsw.AsArray);
				bsr.SkipBits(skip);
				CollectionAssert.AreEqual(rand, bsr.ReadBits(bitLen), $"index: {i}, bitlen: {bitLen}");
			}
		}


		[Test]
		public void EditBits() {
			for (int i = 0; i < Iterations; i++) {
				int skipBefore = i % 16;
				int skipAfter = i % 17 % 16;
				BitStreamWriter bsw = new BitStreamWriter();
				bsw.WriteBitsFromInt(_random.Next(), skipBefore);
				int bitLen = _random.Next(1, 100);
				byte[] rand = new byte[(bitLen >> 3) + ((bitLen & 0x07) == 0 ? 0 : 1)];
				_random.NextBytes(rand);
				bsw.WriteBits(rand, bitLen);
				bsw.WriteBitsFromInt(_random.Next(), skipAfter);
				// now we have written some random data to a fixed location, let's edit it
				_random.NextBytes(rand);
				bsw.EditBitsAtIndex(rand, skipBefore, bitLen);
				if ((bitLen & 0x07) != 0)
					rand[^1] &= (byte)(0xff >> (8 - (bitLen & 0x07))); // get rid of unused bits
				// okay, now lets read and compare to the rand array
				BitStreamReader bsr = new BitStreamReader(bsw.AsArray);
				bsr.SkipBits(skipBefore);
				CollectionAssert.AreEqual(rand, bsr.ReadBits(bitLen), $"index: {i}, bitlen: {bitLen}");
			}
		}


		[Test]
		public void RemoveBits() {
			for (int i = 0; i < Iterations; i++) {
				int skip = i % 16;
				int testIntBits = _random.Next(1, 32);
				int testInt = _random.Next(0, 1 << (testIntBits - 1));
				BitStreamWriter bsw = new BitStreamWriter();
				bsw.WriteBitsFromInt(_random.Next(), skip);
				byte[] rand = new byte[10];
				_random.NextBytes(rand);
				int randCount = _random.Next(1, 80);
				bsw.WriteBits(rand, randCount);
				bsw.WriteBitsFromInt(testInt, testIntBits);
				bsw.RemoveBitsAtIndex(skip, randCount);
				BitStreamReader bsr = new BitStreamReader(bsw.AsArray);
				bsr.SkipBits(skip);
				Assert.AreEqual((int)bsr.ReadBitsAsUInt(testIntBits), testInt);
			}
		}
	}
}