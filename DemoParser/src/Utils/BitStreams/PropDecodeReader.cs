using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DemoParser.Parser.Components.Packets;
using static DemoParser.Utils.BitStreams.CoordSize;

namespace DemoParser.Utils.BitStreams {
    
    // mostly just an extra class to keep this nasty unusual stuff separate
    public partial class BitStreamReader  {

        public void DecodeVector(SendTableProperty prop, ref Vector3 vec3) { // src_main\engine\dt_encode.cpp line 158
            vec3.X = DecodeFloat(prop);
            vec3.Y = DecodeFloat(prop);
            if ((prop.Flags & SendPropertyFlags.Normal) == 0) { 
                vec3.Z = DecodeFloat(prop);
            } else { // Don't read in the third component for normals
                bool sign = ReadBool();
                float distSqr = vec3.X * vec3.X + vec3.Y * vec3.Y;
                if (distSqr < 1)
                    vec3.Z = (float)Math.Sqrt(1 - distSqr);
                else
                    vec3.Z = 0;
                if (sign)
                    vec3.Z = -vec3.Z;
            }
        }
        

        public bool DecodeSpecialFloat(SendTableProperty prop, ref float val) {
            SendPropertyFlags flags = prop.Flags;
            if ((flags & SendPropertyFlags.Coord) != 0) {
                val = ReadBitCoord();
                return true;
            } else if ((flags & SendPropertyFlags.CoordMp) != 0) {
                val = ReadBitCoordMp(false, false);
                return true;
            } else if ((flags & SendPropertyFlags.CoordMpLowPrecision) != 0) {
                val = ReadBitCoordMp(false, true);
                return true;
            } else if ((flags & SendPropertyFlags.CoordMpIntegral) != 0) {
                val = ReadBitCoordMp(true, false);
                return true;
            } else if ((flags & SendPropertyFlags.NoScale) != 0) {
                val = ReadBitFloat();
                return true;
            } else if ((flags & SendPropertyFlags.Normal) != 0) {
                val = ReadBitNormal();
                return true;
            }
            return false;
        }


        public float ReadBitNormal() { // src_main\tier1\newbitbuf.cpp line 662
            bool signBit = ReadBool();
            uint fracVal = ReadBitsAsUInt(NormFracBits);
            float val = fracVal * NormRes;
            if (signBit)
                val = -val;
            return val;
        }


        public float ReadBitFloat() => BitConverter.Int32BitsToSingle((int)ReadUInt());


        [SuppressMessage("ReSharper", "PossibleInvalidOperationException")]
        public float DecodeFloat(SendTableProperty prop) {
            float val = default;
            if (DecodeSpecialFloat(prop, ref val)) // check for special flags
                return val;
            int bits = (int)prop.NumBits.Value;
            uint dwInterp = ReadBitsAsUInt(bits);
            val = (float)dwInterp / ((1 << bits) - 1);
            return prop.LowValue.Value + (prop.HighValue.Value - prop.LowValue.Value) * val;
        }


        public float ReadBitCoord() {
            float val = 0;
            uint intVal = ReadSingleBitAsUInt();
            uint fracVal = ReadSingleBitAsUInt();
            if (intVal == 1 || fracVal == 1) {
                bool sign = ReadBool();
                if (intVal == 1)
                    intVal = ReadBitsAsUInt(CoordIntBits) + 1;
                if (fracVal == 1)
                    fracVal = ReadBitsAsUInt(CoordFracBits);
                val = intVal + fracVal * CoordRes;
                if (sign)
                    val = -val;
            }
            return val;
        }


        public float ReadBitCoordMp(bool bIntegral, bool bLowPrecision) { // src_main\tier1\newbitbuf.cpp line 578
            uint intval, fractval;
            bool sign = false;
            float val = 0;
            bool bInBounds = ReadBool();
            if (bIntegral) {
                intval = ReadSingleBitAsUInt();
                if (intval != 0) {
                    sign = ReadBool();
                    if (bInBounds)
                        val = ReadBitsAsUInt(CoordIntBitsMp) + 1;
                    else
                        val = ReadBitsAsUInt(CoordIntBits) + 1;
                }
            } else {
                intval = ReadSingleBitAsUInt();
                sign = ReadBool();
                if (intval != 0) {
                    if (bInBounds)
                        intval = ReadBitsAsUInt(CoordIntBitsMp) + 1;
                    else
                        intval = ReadBitsAsUInt(CoordIntBits) + 1;
                }

                fractval = ReadBitsAsUInt(bLowPrecision ? CoordFracBitsMpLp : CoordFracBits);
                val = intval + fractval * (bLowPrecision ? CoordResLp : CoordRes);
            }
            if (sign)
                val = -val;
            return val;
        }
        
        
        public uint ReadUBitInt() {
            uint ret = ReadBitsAsUInt(6);
            ret = (ret & (16 | 32)) switch {
                16 => ((ret & 15) | (ReadBitsAsUInt(4) << 4)),
                32 => ((ret & 15) | (ReadBitsAsUInt(8) << 4)),
                48 => ((ret & 15) | (ReadBitsAsUInt(28) << 4)),
                _ => ret
            };
            return ret;
        }
		
		
        // switch is exhaustive
#pragma warning disable 8509 
        public uint ReadUBitVar() {
            return ReadBitsAsUInt(2) switch {
                0 => ReadBitsAsUInt(4),
                1 => ReadBitsAsUInt(8),
                2 => ReadBitsAsUInt(12),
                3 => ReadBitsAsUInt(32)
            };
        }
#pragma warning restore 8509


        public float ReadBitAngle(int bitCount) {
            float shift = 1 << bitCount;
            uint i = ReadBitsAsUInt(bitCount);
            return i * (360f / shift);
        }


        public Vector3 ReadVectorCoord() {
            var exists = new {x = ReadBool(), y = ReadBool(), z = ReadBool()};
            return new Vector3 {X = exists.x ? ReadBitCoord() : 0, Y = exists.y ? ReadBitCoord() : 0, Z = exists.z ? ReadBitCoord() : 0};
        }
    }


    // these might be different depending on the game, i sure hope fucking not
    public static class CoordSize { 
        // src_main\public\coordsize.h
        public const int   CoordIntBits      = 14;
        public const int   CoordFracBits     = 5;
        public const int   CoordDenom        = 1 << CoordFracBits;
        public const float CoordRes          = 1.0f / CoordDenom;
        public const int   CoordIntBitsMp    = 11;
        public const int   CoordFracBitsMpLp = 3;
        public const int   CoordDenomLp      = 1 << CoordFracBitsMpLp;
        public const float CoordResLp        = 1.0f / CoordDenomLp;

        public const int   NormFracBits      = 11;
        public const int   NormDenom         = (1 << NormFracBits) - 1;
        public const float NormRes           = 1.0f / NormDenom;
        
        // src_main\public\dt_common.h
        /*public const int CoordMp = 1 << 13;
        public const int CoordMpLp = 1 << 14;
        public const int CoordMpIntegral = 1 << 15;
        public const int NumFlagBitsNetworked = 16;*/
    }
}