using System;
using System.Collections.Generic;
using NUnit.Framework;
using DemoParser.Utils.BitStreams;

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
			for (int i = 0; i < Count; i++)  {
				bool r = _random.NextDouble() >= .5;
				bools.Add(r);
				_writer.WriteBool(r);
			}
			_reader = new BitStreamReader(_writer.AsArray);
			for (int i = 0; i < Count; i++) {
				Assert.AreEqual(bools[i], _reader.ReadBool());
			}
			Assert.Pass();
		}
		

		[Test]
		public void WriteUBits() {
			List<(uint val, int size)> bits = new List<(uint val, int size)>();
			
			for (int i = 0; i < Count; i++)  {
				var r = ((uint)_random.Next(), _random.Next(1, 33));
				r.Item1 &= uint.MaxValue >> (32 - r.Item2);
				bits.Add(r);
				_writer.WriteBitsFromInt(r.Item1, r.Item2);
			}
			_reader = new BitStreamReader(_writer.AsArray);
			for (int i = 0; i < Count; i++) {
				Assert.AreEqual(bits[i].val, _reader.ReadBitsAsUInt(bits[i].size));
			}
			Assert.Pass();
		}
		
		
		[Test]
		public void WriteSInts() {
			List<(int val, int size)> bits = new List<(int val, int size)>();
			
			for (int i = 0; i < Count; i++)  {
				var r = (_random.Next(), _random.Next(1, 33));
				r.Item1 &= int.MaxValue >> (32 - r.Item2);
				if ((r.Item1 & (1 << (r.Item2 - 1))) != 0) // sign extend, not necessary for primitive types
					r.Item1 |= int.MaxValue << r.Item2;    // can use int here since the leftmost 0 will get shifted away anyway
				bits.Add(r);
				_writer.WriteBitsFromInt(r.Item1, r.Item2);
			}
			_reader = new BitStreamReader(_writer.AsArray);
			for (int i = 0; i < Count; i++) {
				Assert.AreEqual(bits[i].val, _reader.ReadBitsAsSInt(bits[i].size));
			}
			Assert.Pass();
		}


		[Test]
		public void WriteUIntsIfExists() {
			List<(uint? val, int size)> bits = new List<(uint? val, int size)>();
			
			for (int i = 0; i < Count; i++)  {
				bool rBool = _random.NextDouble() >= 0.5;
				var r = (rBool ? (uint?)null : (uint)_random.Next(), _random.Next(1, 33));
				r.Item1 &= uint.MaxValue >> (32 - r.Item2);
				bits.Add(r);
				_writer.WriteBitsFromUIntIfExists(r.Item1, r.Item2);
			}
			_reader = new BitStreamReader(_writer.AsArray);
			for (int i = 0; i < Count; i++) {
				Assert.AreEqual(bits[i].val, _reader.ReadBitsAsUIntIfExists(bits[i].size));
			}
			Assert.Pass();
		}
	}
}