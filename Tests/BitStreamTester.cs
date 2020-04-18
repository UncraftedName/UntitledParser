using System;
using System.Collections.Generic;
using DemoParser.Utils.BitStreams;
using NUnit.Framework;

namespace Tests {
	
	public class BitStreamTester {

		private const int Count = 10000;
		private BitStreamReader _reader;
		private BitStreamWriter _writer;
		private Random _random;
		

		[SetUp]
		public void Setup() {
			_writer = new BitStreamWriter();
			_random = new Random(4);
		}
		

		[Test]
		public void WriteBools() {
			List<bool> bools = new List<bool>();
			for (int _ = 0; _ < Count; _++)  {
				bool r = _random.NextDouble() >= .5;
				bools.Add(r);
				_writer.WriteBool(r);
			}
			_reader = new BitStreamReader(_writer.AsArray);
			for (int i = 0; i < Count; i++) 
				Assert.AreEqual(bools[i], _reader.ReadBool());
			Assert.Pass();
		}
		

		[Test]
		public void WriteUBits() {
			List<(uint val, int size)> bits = new List<(uint val, int size)>();
			
			for (int _ = 0; _ < Count; _++)  {
				(uint val, int size) r = ((uint)_random.Next(), _random.Next(1, 33));
				r.val &= uint.MaxValue >> (32 - r.size);
				bits.Add(r);
				_writer.WriteBitsFromInt(r.val, r.size);
			}
			_reader = new BitStreamReader(_writer.AsArray);
			for (int i = 0; i < Count; i++) 
				Assert.AreEqual(bits[i].val, _reader.ReadBitsAsUInt(bits[i].size));
			Assert.Pass();
		}
		
		
		[Test]
		public void WriteSInts() {
			List<(int val, int size)> bits = new List<(int val, int size)>();
			
			for (int _ = 0; _ < Count; _++)  {
				(int val, int size) r = (_random.Next(), _random.Next(1, 33));
				r.val &= int.MaxValue >> (32 - r.size);
				if ((r.val & (1 << (r.size - 1))) != 0) // sign extend, not necessary for primitive types
					r.val |= int.MaxValue << r.size;    // can use int here since the leftmost 0 will get shifted away anyway
				bits.Add(r);
				_writer.WriteBitsFromInt(r.val, r.size);
			}
			_reader = new BitStreamReader(_writer.AsArray);
			for (int i = 0; i < Count; i++) 
				Assert.AreEqual(bits[i].val, _reader.ReadBitsAsSInt(bits[i].size));
			Assert.Pass();
		}


		[Test]
		public void WriteUIntsIfExists() {
			List<(uint? val, int size)> bits = new List<(uint? val, int size)>();
			
			for (int _ = 0; _ < Count; _++)  {
				bool rBool = _random.NextDouble() >= 0.5;
				(uint? val, int size) r = (rBool ? (uint?)null : (uint)_random.Next(), _random.Next(1, 33));
				r.val &= uint.MaxValue >> (32 - r.size);
				bits.Add(r);
				_writer.WriteBitsFromUIntIfExists(r.val, r.size);
			}
			_reader = new BitStreamReader(_writer.AsArray);
			for (int i = 0; i < Count; i++) 
				Assert.AreEqual(bits[i].val, _reader.ReadBitsAsUIntIfExists(bits[i].size));
			Assert.Pass();
		}


		[Test]
		public void WriteBitCoord() {
			for (int i = 0; i < Count; i++) {
				int skip = i % 8;
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
				Assert.AreEqual(bsr.ReadBitCoord(), f);
			}
		}
	}
}