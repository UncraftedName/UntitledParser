using System;

namespace DemoParser.Utils.BitStreams {
    
    public partial class BitStreamWriter {
        
        // doesn't take into account starting inside the list and finishing outside
        public void EditBitsAtIndex(byte[] bytes, int bitIndex, int bitCount) {
            int byteIndex = bitIndex >> 3;
            byte remainingBitMaskFirstByte = (byte)(0xff >> (8 - (bitIndex & 0x07)));
            int bytesModified = 1;
            // zero out bits that are going to be overriden
            if (8 - (bitIndex & 0x07) >= bitCount) { // all bits can be written to in a single byte, bitCount <= 8
                remainingBitMaskFirstByte |= (byte)(0xff << bitCount << (bitIndex & 0x07));
                _data[byteIndex] &= remainingBitMaskFirstByte;
            } else {
                _data[byteIndex++] &= remainingBitMaskFirstByte;
                int bitsRemaining = bitCount - (8 - (bitIndex & 0x07));
                while (bitsRemaining >= 8) {
                    _data[byteIndex++] = 0;
                    bitsRemaining -= 8;
                    bytesModified++;
                }
                if (bitsRemaining > 0) {
                    _data[byteIndex] &= (byte)(0xff << bitsRemaining);
                    bytesModified++;
                }
            }
            BitStreamWriter writer = new BitStreamWriter((bitCount << 3) + 1);
            writer.WriteBitsFromInt(0, bitIndex & 0x07);
            writer.WriteBits(bytes, bitCount);
            byte[] arr = writer.AsArray;
            for (int i = 0; i < bytesModified; i++) 
                _data[(bitIndex >> 3) + i] |= arr[i];
        }


        public void EditBitsAtIndex(byte[] bytes, int bitIndex) => EditBitsAtIndex(bytes, bitIndex, bytes.Length << 3);


        public void EditIntAtIndex(int i, int bitIndex, int bitCount) {
            byte[] b = BitConverter.GetBytes(i);
            if (IsLittleEndian ^ BitConverter.IsLittleEndian)
                Array.Reverse(b);
            EditBitsAtIndex(b, bitIndex, bitCount);
        }
        


        public void RemoveBitsAtIndex(int bitIndex, int bitCount) {
            if (bitIndex + bitCount > BitLength)
                throw new ArgumentOutOfRangeException(nameof(bitIndex), "the removed bits extend outside of the existing array");
            // it's way easier to just write the data to a new list, should work just fine for now
            BitStreamReader bsr = new BitStreamReader(_data);
            BitStreamWriter newWriter = new BitStreamWriter(_data.Capacity);
            newWriter.WriteBits(bsr.ReadBits(bitIndex), bitIndex);
            bsr.SkipBits(bitCount);
            newWriter.WriteBits(bsr.ReadRemainingBits());
            _data = newWriter._data;
            BitLength = newWriter.BitLength;
        }
    }
}