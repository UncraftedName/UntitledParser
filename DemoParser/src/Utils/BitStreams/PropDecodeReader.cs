using System;
using System.Collections.Generic;
using System.Numerics;
using DemoParser.Parser.HelperClasses.EntityStuff;
using static DemoParser.Parser.HelperClasses.EntityStuff.SendPropEnums;
using static DemoParser.Utils.BitStreams.PropDecodeConsts;

// I want to access NumBits from props without my IDE screaming at me
// ReSharper disable PossibleInvalidOperationException

namespace DemoParser.Utils.BitStreams {
	
	// mostly just an extra class to keep this nasty unusual stuff separate
	public partial class BitStreamReader  {

		public void DecodeVector3(SendTableProp propInfo, out Vector3 vec3) { // src_main\engine\dt_encode.cpp line 158
			vec3.X = DecodeFloat(propInfo);
			vec3.Y = DecodeFloat(propInfo);
			if (propInfo.FloatParseType == FloatParseType.Normal) {
				// Don't read in the third component for normals
				bool sign = ReadBool();
				float distSqr = vec3.X * vec3.X + vec3.Y * vec3.Y;
				if (distSqr < 1)
					vec3.Z = (float)Math.Sqrt(1 - distSqr);
				else
					vec3.Z = 0;
				if (sign)
					vec3.Z = -vec3.Z;
			} else { 
				vec3.Z = DecodeFloat(propInfo);
			}
		}


		public void DecodeVector2(SendTableProp propInfo, out Vector2 vec2) { // nice and easy
			vec2.X = DecodeFloat(propInfo);
			vec2.Y = DecodeFloat(propInfo);
		}


		private float ReadBitCellCoord(SendTableProp propInfo) {
			uint intval = ReadBitsAsUInt(propInfo.NumBits!.Value);
			if (propInfo.FloatParseType == FloatParseType.BitCellChordInt) {
				return intval;
			} else {
				bool lp = propInfo.FloatParseType == FloatParseType.BitCellChordLp;
				uint fractVal = ReadBitsAsUInt(lp ? CoordFracBitsMpLp : CoordFracBits);
				return intval + fractVal * (lp ? CoordResLp : CoordRes);
			}
		}


		public float ReadBitNormal() { // src_main\tier1\newbitbuf.cpp line 662
			bool signBit = ReadBool();
			float val = ReadBitsAsUInt(NormFracBits) * NormRes;
			if (signBit)
				val = -val;
			return val;
		}


		public float DecodeFloat(SendTableProp propInfo) {
			if (propInfo.FloatParseType == null)
				SetFloatParseType(propInfo);

			return propInfo.FloatParseType switch {
				FloatParseType.Standard        => ReadStandardFloat(propInfo),
				FloatParseType.Coord           => ReadBitCoord(),
				FloatParseType.BitCoordMp      => ReadBitCoordMp(propInfo),
				FloatParseType.BitCoordMpLp    => ReadBitCoordMp(propInfo),
				FloatParseType.BitCoordMpInt   => ReadBitCoordMp(propInfo),
				FloatParseType.NoScale         => ReadFloat(),
				FloatParseType.Normal          => ReadBitNormal(),
				FloatParseType.BitCellChord    => ReadBitCellCoord(propInfo),
				FloatParseType.BitCellChordLp  => ReadBitCellCoord(propInfo),
				FloatParseType.BitCellChordInt => ReadBitCellCoord(propInfo),
				_ => throw new ArgumentOutOfRangeException(nameof(propInfo.FloatParseType))
			};
		}


		private float ReadStandardFloat(SendTableProp propInfo) {
			int bits = (int)propInfo.NumBits!.Value;
			uint dwInterp = ReadBitsAsUInt(bits);
			float val = (float)dwInterp / ((1 << bits) - 1);
			return propInfo.LowValue!.Value + (propInfo.HighValue!.Value - propInfo.LowValue.Value) * val;
		}


		private static void SetFloatParseType(SendTableProp propInfo) {
			int flags = propInfo.Flags;
			AbstractFlagChecker<PropFlag> checker = propInfo.DemoRef.DemoSettings.PropFlagChecker;
			ref FloatParseType? pType = ref propInfo.FloatParseType; 
			if (checker.HasFlag(flags, PropFlag.Coord)) {
				pType = FloatParseType.Coord;
			} else if (checker.HasFlag(flags, PropFlag.CoordMp)) {
				pType = FloatParseType.BitCoordMp;
			} else if (checker.HasFlag(flags, PropFlag.CoordMpLp)) {
				pType = FloatParseType.BitCoordMpLp;
			} else if (checker.HasFlag(flags, PropFlag.CoordMpInt)) {
				pType = FloatParseType.BitCoordMpInt;
			} else if (checker.HasFlag(flags, PropFlag.NoScale)) {
				pType = FloatParseType.NoScale;
			} else if (checker.HasFlag(flags, PropFlag.Normal)) {
				pType = FloatParseType.Normal;
			} else if (propInfo.DemoRef.DemoSettings.NewDemoProtocol) {
				if (checker.HasFlag(flags, PropFlag.CellCoord)) {
					pType = FloatParseType.BitCellChord;
				} else if (checker.HasFlag(flags, PropFlag.CellCoordLp)) {
					pType = FloatParseType.BitCellChordLp;
				} else if (checker.HasFlag(flags, PropFlag.CellCoordInt)) {
					pType = FloatParseType.BitCellChordInt;
				}
			}
			pType ??= FloatParseType.Standard;
#if DEBUG
			// I assume in the vec3 parsing that if the float is a normal that these values before it are not set.
			// (if normal -> no other float flags before the normal flag are set)
			if (pType == FloatParseType.Coord ||
				pType == FloatParseType.BitCoordMp ||
				pType == FloatParseType.BitCoordMpLp ||
				pType == FloatParseType.BitCoordMpInt ||
				pType == FloatParseType.NoScale)
			{
				Debug.Assert(!checker.HasFlag(flags, PropFlag.Normal));
			}
#endif
		}


		public float ReadBitCoord() {
			float val = 0;
			bool hasInt = ReadBool();
			bool hasFrac = ReadBool();
			if (hasInt || hasFrac) {
				bool sign = ReadBool();
				if (hasInt)
					val += ReadBitsAsUInt(CoordIntBits) + 1;
				if (hasFrac)
					val += ReadBitsAsUInt(CoordFracBits) * CoordRes;
				if (sign)
					val = -val;
			}
			return val;
		}


		public float ReadBitCoordMp(SendTableProp propInfo) { // src_main\tier1\newbitbuf.cpp line 578
			bool sign = false;
			float val = 0;
			bool bInBounds = ReadBool();
			if (propInfo.FloatParseType == FloatParseType.BitCoordMpInt) {
				if (ReadBool()) {
					sign = ReadBool();
					if (bInBounds)
						val = ReadBitsAsUInt(CoordIntBitsMp) + 1;
					else
						val = ReadBitsAsUInt(CoordIntBits) + 1;
				}
			} else {
				uint intval = (uint)(ReadBool() ? 1 : 0);
				sign = ReadBool();
				if (intval != 0) {
					if (bInBounds)
						intval = ReadBitsAsUInt(CoordIntBitsMp) + 1;
					else
						intval = ReadBitsAsUInt(CoordIntBits) + 1;
				}
				bool lp = propInfo.FloatParseType == FloatParseType.BitCoordMpLp;
				uint fractval = ReadBitsAsUInt(lp ? CoordFracBitsMpLp : CoordFracBits);
				val = intval + fractval * (lp ? CoordResLp : CoordRes);
			}
			if (sign)
				val = -val;
			return val;
		}


		public int DecodeInt(SendTableProp propInfo) {
			return propInfo.DemoRef.DemoSettings.PropFlagChecker.HasFlag(propInfo.Flags, PropFlag.Unsigned)
				? (int)ReadBitsAsUInt(propInfo.NumBits!.Value)
				: ReadBitsAsSInt(propInfo.NumBits!.Value);
		}


		public string DecodeString() {
			return ReadStringOfLength(ReadBitsAsUInt(DtMaxStringBits));
		}


		public List<int> DecodeIntArr(FlattenedProp propInfo) {
			int count = (int)ReadBitsAsUInt(BitUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			List<int> result = new List<int>(count);
			for (int i = 0; i < count; i++) 
				result.Add(DecodeInt(propInfo.ArrayElementPropInfo!));
			return result;
		}


		public List<float> DecodeFloatArr(FlattenedProp propInfo) {
			int count = (int)ReadBitsAsUInt(BitUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			List<float> result = new List<float>(count);
			for (int i = 0; i < count; i++) 
				result.Add(DecodeFloat(propInfo.ArrayElementPropInfo!));
			return result;
		}


		public List<string> DecodeStringArr(FlattenedProp propInfo) {
			int count = (int)ReadBitsAsUInt(BitUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			List<string> result = new List<string>(count);
			for (int i = 0; i < count; i++) 
				result.Add(DecodeString());
			return result;
		}


		public List<Vector3> DecodeVector3Arr(FlattenedProp propInfo) {
			int count = (int)ReadBitsAsUInt(BitUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			List<Vector3> result = new List<Vector3>(count);
			for (int i = 0; i < count; i++) {
				DecodeVector3(propInfo.ArrayElementPropInfo!, out Vector3 v3);
				result.Add(v3);
			}
			return result;
		}
		
		
		public List<Vector2> DecodeVector2Arr(FlattenedProp propInfo) {
			int count = (int)ReadBitsAsUInt(BitUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			List<Vector2> result = new List<Vector2>(count);
			for (int i = 0; i < count; i++) {
				DecodeVector2(propInfo.ArrayElementPropInfo!, out Vector2 v2);
				result.Add(v2);
			}
			return result;
		}
		

#pragma warning disable 8509
		
		public uint ReadUBitVar() {
			return ReadBitsAsUInt(2) switch {
				0 => ReadBitsAsUInt(4),
				1 => ReadBitsAsUInt(8),
				2 => ReadBitsAsUInt(12),
				3 => ReadBitsAsUInt(32)
			};
		}


		public uint ReadUBitInt() {
			uint ret = ReadBitsAsUInt(4);
			return ReadBitsAsUInt(2) switch {
				0 => ret,
				1 => ret | (ReadBitsAsUInt(4) << 4),
				2 => ret | (ReadBitsAsUInt(8) << 4),
				3 => ret | (ReadBitsAsUInt(28) << 4)
			};
		}


		public int ReadFieldIndex(int lastIndex, bool bNewWay) {
			if (bNewWay && ReadBool()) // short circuit
				return lastIndex + 1;
			uint ret;
			if (bNewWay && ReadBool()) {
				ret = ReadBitsAsUInt(3);
			} else {
				ret = ReadBitsAsUInt(5);
				ret = ReadBitsAsUInt(2) switch {
					0 => ret,
					1 => ret | (ReadBitsAsUInt(2) << 5),
					2 => ret | (ReadBitsAsUInt(4) << 5),
					3 => ret | (ReadBitsAsUInt(7) << 5)
				};
			}
			if (ret == 0xFFF) // end marker
				return -1;
			return lastIndex + 1 + (int)ret;
		}
		
#pragma warning restore 8509


		public float ReadBitAngle(int bitCount) {
			return ReadBitsAsUInt(bitCount) * (360f / (1 << bitCount));
		}


		public void ReadVectorCoord(out Vector3 vec3) {
			var exists = new {x = ReadBool(), y = ReadBool(), z = ReadBool()};
			vec3.X = exists.x ? ReadBitCoord() : 0;
			vec3.Y = exists.y ? ReadBitCoord() : 0;
			vec3.Z = exists.z ? ReadBitCoord() : 0;
		}
	}


	// these might be different depending on the game, i sure hope fucking not
	// found in src_main\public\coordsize.h
	internal static class PropDecodeConsts { 
		public const int   CoordFracBits     = 5;
		public const int   CoordDenom        = 1 << CoordFracBits;
		public const float CoordRes          = 1.0f / CoordDenom;
		public const int   CoordIntBitsMp    = 11;
		public const int   CoordIntBits      = 14;
		public const int   CoordFracBitsMpLp = 3;
		public const int   CoordDenomLp      = 1 << CoordFracBitsMpLp;
		public const float CoordResLp        = 1.0f / CoordDenomLp;
		public const int   NormFracBits      = 11;
		public const int   NormDenom         = (1 << NormFracBits) - 1;
		public const float NormRes           = 1.0f / NormDenom;
		public const int   DtMaxStringBits   = 9; // read this many bits to get the length of a string prop
	}


	internal enum FloatParseType {
		Standard,
		Coord,
		BitCoordMp,
		BitCoordMpLp,
		BitCoordMpInt,
		NoScale,
		Normal,
		BitCellChord,
		BitCellChordLp,
		BitCellChordInt,
	}
}