namespace DemoParser.Utils.BitStreams {
    
    public partial class BitStreamWriter {
        
        // doesn't take into account starting inside the list and finishing outside
        public void EditBitsAtIndex(int bitIndex, byte[] bytes, int bitCount) {
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


        public void EditBitsAtIndex(int bitIndex, byte[] bytes) => EditBitsAtIndex(bitIndex, bytes, bytes.Length << 3);
    }
}