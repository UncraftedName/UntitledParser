using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser;
using DemoParser.Parser.Components.Abstract;

namespace DemoParser.Utils.BitStreams {
    
    // This file will have all of the brute force searching utilities,
    // these should never be used when parsing the demo, only for finding new patters and offsets.
    // Be sure to create a substream if you don't want the offset of the reader to be affected.
    public partial class BitStreamReader {
        
		
		public int FindUInt(uint val) => FindUInt(val, BitUtils.HighestBitIndex(val) + 1);
		
		
		public int FindUInt(uint val, int bitCount) {
			var tmp = new BitStreamWriter(4, IsLittleEndian);
			tmp.WriteUInt(val);
			return FindBytes(tmp.AsArray, bitCount);
		}


		public IEnumerable<int> FindUIntAllInstances(uint val, int bitCount) {
			var tmp = new BitStreamWriter(4, IsLittleEndian);
			tmp.WriteUInt(val);
			return FindBytesAllInstances(tmp.AsArray, bitCount);
		}


		public IEnumerable<int> FindFloatAllInstances(float f) {
			var tmp = new BitStreamWriter(4, IsLittleEndian);
			tmp.WriteFloat(f);
			return FindBytesAllInstances(tmp.AsArray, 32);
		}


		public int FindFloat(float val) {
			var tmp = new BitStreamWriter(4, IsLittleEndian);
			tmp.WriteFloat(val);
			return FindBytes(tmp.AsArray, 32);
		}


		public int FindBytes(byte[] arr, int bitCount) {
			byte[] resized = new byte[((bitCount - 1) >> 3) + 1]; // ensure that resized is the minimum length
			Array.Copy(arr, resized, resized.Length);
			while (BitsRemaining >= bitCount) {
				if (SubStream().ReadBits(bitCount).SequenceEqual(resized))
					return CurrentBitIndex;
				AbsoluteBitIndex++;
			}
			return -1;
		}


		public int FindString(string s) {
			byte[] bytes = ParserTextUtils.StringAsByteArray(s, s.Length);
			return FindBytes(bytes, bytes.Length << 3);
		}


		public IEnumerable<int> FindBytesAllInstances(byte[] arr, int bitCount) {
			byte[] resized = new byte[((bitCount - 1) >> 3) + 1]; // ensure that resized is the minimum length
			Array.Copy(arr, resized, resized.Length);
			while (BitsRemaining >= bitCount) {
				if (SubStream().ReadBits(bitCount).SequenceEqual(resized))
					yield return CurrentBitIndex;
				AbsoluteBitIndex++;
			}
		}


		public (int offset, T? component) FindComponentWithProperty<T>(Func<T, bool> property, SourceDemo demoRef) where T : DemoComponent {
			while (BitsRemaining > 0) {
				T inst = (T)Activator.CreateInstance(typeof(T), demoRef, SubStream())!;
				try {
					inst.ParseStream(SubStream());
				}
				catch (Exception) {} // the whole point is to push past exceptions for a brute force search
				if (property.Invoke(inst))
					return (CurrentBitIndex, inst);
				AbsoluteBitIndex++;
			}
			return (-1, null);
		}
    }
}