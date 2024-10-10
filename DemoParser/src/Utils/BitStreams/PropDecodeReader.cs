using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using DemoParser.Parser;
using DemoParser.Parser.EntityStuff;
using DemoParser.Parser.GameState;
using static DemoParser.Parser.EntityStuff.SendPropEnums;
using static DemoParser.Utils.BitStreams.PropDecodeConsts;

// I want to access NumBits from props without my IDE screaming at me
// ReSharper disable PossibleInvalidOperationException

namespace DemoParser.Utils.BitStreams {

	// mostly just an extra class to keep this nasty unusual stuff separate
	public partial struct BitStreamReader {

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
			uint intVal = (uint)ReadULong((int)propInfo.NumBits!.Value);
			if (propInfo.FloatParseType == FloatParseType.BitCellChordInt) {
				return intVal;
			} else {
				bool lp = propInfo.FloatParseType == FloatParseType.BitCellChordLp;
				uint fractVal = ReadUInt(lp ? CoordFracBitsMpLp : CoordFracBits);
				return intVal + fractVal * (lp ? CoordResLp : CoordRes);
			}
		}


		public float ReadBitNormal() { // src_main\tier1\newbitbuf.cpp line 662
			bool signBit = ReadBool();
			float val = ReadUInt(NormFracBits) * NormRes;
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
			uint dwInterp = ReadUInt(bits);
			float val = (float)dwInterp / ((1 << bits) - 1);
			return propInfo.LowValue!.Value + (propInfo.HighValue!.Value - propInfo.LowValue.Value) * val;
		}


		private static void SetFloatParseType(SendTableProp propInfo) {
			int flags = propInfo.Flags;
			AbstractFlagChecker<PropFlag> checker = propInfo.DemoRef.DemoInfo.PropFlagChecker;
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
			} else if (propInfo.DemoRef.DemoInfo.NewDemoProtocol) {
				// these never go through for DemoProtocol3FlagChecker
				// maybe this check should be about flag versions and not demo protocol version
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
					val += ReadUInt(CoordIntBits) + 1;
				if (hasFrac)
					val += ReadUInt(CoordFracBits) * CoordRes;
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
						val = ReadUInt(CoordIntBitsMp) + 1;
					else
						val = ReadUInt(CoordIntBits) + 1;
				}
			} else {
				uint intVal = (uint)(ReadBool() ? 1 : 0);
				sign = ReadBool();
				if (intVal != 0) {
					if (bInBounds)
						intVal = ReadUInt(CoordIntBitsMp) + 1;
					else
						intVal = ReadUInt(CoordIntBits) + 1;
				}
				bool lp = propInfo.FloatParseType == FloatParseType.BitCoordMpLp;
				uint fractVal = ReadUInt(lp ? CoordFracBitsMpLp : CoordFracBits);
				val = intVal + fractVal * (lp ? CoordResLp : CoordRes);
			}
			if (sign)
				val = -val;
			return val;
		}


		public int DecodeInt(SendTableProp propInfo) {
			return propInfo.DemoRef.DemoInfo.PropFlagChecker.HasFlag(propInfo.Flags, PropFlag.Unsigned)
				? (int)(uint)ReadULong((int)propInfo.NumBits!.Value)
				: ReadSInt((int)propInfo.NumBits!.Value);
		}


		public string DecodeString() {
			return ReadStringOfLength((int)ReadULong(DtMaxStringBits));
		}


		public List<int> DecodeIntArr(FlattenedProp propInfo) {
			int count = (int)ReadUInt(ParserUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			if (HasOverflowed)
				count = 0;
			List<int> result = new List<int>(count);
			for (int i = 0; i < count; i++)
				result.Add(DecodeInt(propInfo.ArrayElementPropInfo!));
			return result;
		}


		public List<float> DecodeFloatArr(FlattenedProp propInfo) {
			int count = (int)ReadUInt(ParserUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			if (HasOverflowed)
				count = 0;
			List<float> result = new List<float>(count);
			for (int i = 0; i < count; i++)
				result.Add(DecodeFloat(propInfo.ArrayElementPropInfo!));
			return result;
		}


		public List<string> DecodeStringArr(FlattenedProp propInfo) {
			int count = (int)ReadUInt(ParserUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			if (HasOverflowed)
				count = 0;
			List<string> result = new List<string>(count);
			for (int i = 0; i < count; i++)
				result.Add(DecodeString());
			return result;
		}


		public List<Vector3> DecodeVector3Arr(FlattenedProp propInfo) {
			int count = (int)ReadUInt(ParserUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			if (HasOverflowed)
				count = 0;
			List<Vector3> result = new List<Vector3>(count);
			for (int i = 0; i < count; i++) {
				DecodeVector3(propInfo.ArrayElementPropInfo!, out Vector3 v3);
				result.Add(v3);
			}
			return result;
		}


		public List<Vector2> DecodeVector2Arr(FlattenedProp propInfo) {
			int count = (int)ReadUInt(ParserUtils.HighestBitIndex(propInfo.PropInfo.NumElements!.Value) + 1);
			if (HasOverflowed)
				count = 0;
			List<Vector2> result = new List<Vector2>(count);
			for (int i = 0; i < count; i++) {
				DecodeVector2(propInfo.ArrayElementPropInfo!, out Vector2 v2);
				result.Add(v2);
			}
			return result;
		}


		public uint ReadVarUInt32() {
			uint result = 0;
			for (int i = 0; i < MaxVarInt32Bytes; i++) {
				uint b = ReadByte();
				result |= (b & 0x7F) << (7 * i);
				if ((b & 0x80) == 0)
					break;
			}
			return result;
		}


#pragma warning disable 8509

		public uint ReadUBitVar() {
			return ReadUInt(2) switch {
				0 => ReadUInt(4),
				1 => ReadUInt(8),
				2 => ReadUInt(12),
				3 => ReadUInt(32)
			};
		}


		public uint ReadUBitInt() {
			uint ret = ReadUInt(4);
			return ReadUInt(2) switch {
				0 => ret,
				1 => ret | (ReadUInt(4) << 4),
				2 => ret | (ReadUInt(8) << 4),
				3 => ret | (ReadUInt(28) << 4)
			};
		}


		public int ReadFieldIndex(int lastIndex, bool bNewWay) {
			if (bNewWay && ReadBool()) // short circuit
				return lastIndex + 1;
			uint ret;
			if (bNewWay && ReadBool()) {
				ret = ReadUInt(3);
			} else {
				ret = ReadUInt(5);
				ret = ReadUInt(2) switch {
					0 => ret,
					1 => ret | (ReadUInt(2) << 5),
					2 => ret | (ReadUInt(4) << 5),
					3 => ret | (ReadUInt(7) << 5)
				};
			}
			if (ret == 0xFFF) // end marker
				return -1;
			return lastIndex + 1 + (int)ret;
		}

#pragma warning restore 8509


		public float ReadBitAngle(int bitCount) {
			return ReadUInt(bitCount) * (360f / (1 << bitCount));
		}


		public void ReadVectorCoord(out Vector3 vec3) {
			var exists = new {x = ReadBool(), y = ReadBool(), z = ReadBool()};
			vec3.X = exists.x ? ReadBitCoord() : 0;
			vec3.Y = exists.y ? ReadBitCoord() : 0;
			vec3.Z = exists.z ? ReadBitCoord() : 0;
		}


		// these two functions below have the real irresistible juice


		public List<(int propIndex, EntityProperty prop)> ReadEntProps(
			IReadOnlyList<FlattenedProp> fProps,
			SourceDemo? demoRef)
		{
			var props = new List<(int propIndex, EntityProperty prop)>();

			int i = -1;
			if (demoRef.DemoInfo.NewDemoProtocol) {
				bool newWay = !demoRef.DemoInfo.Game.IsLeft4Dead1() && ReadBool();
				while ((i = ReadFieldIndex(i, newWay)) != -1 && !HasOverflowed) {
					if (i < 0 || i >= fProps.Count) {
						HasOverflowed = true;
						return props;
					}
					props.Add((i, CreateAndReadProp(fProps[i])));
				}
			} else {
				while (ReadBool() && !HasOverflowed) {
					i += (int)ReadUBitVar() + 1;
					if (i < 0 || i >= fProps.Count) {
						HasOverflowed = true;
						return props;
					}
					props.Add((i, CreateAndReadProp(fProps[i])));
				}
			}
			return props;
		}


		// all of this fun jazz can be found in src_main/engine/dt_encode.cpp, a summary with comments is at the very end
		private EntityProperty CreateAndReadProp(FlattenedProp fProp) {
			const string exceptionMsg = "an impossible entity type has appeared while creating/reading props ";
			int offset = AbsoluteBitIndex;
			switch (fProp.PropInfo.SendPropType) {
				case SendPropType.Int:
					int i = DecodeInt(fProp.PropInfo);
					return new SingleEntProp<int>(fProp, i, offset, AbsoluteBitIndex - offset);
				case SendPropType.Float:
					float f = DecodeFloat(fProp.PropInfo);
					return new SingleEntProp<float>(fProp, f, offset, AbsoluteBitIndex - offset);
				case SendPropType.Vector3:
					DecodeVector3(fProp.PropInfo, out Vector3 v3);
					return new SingleEntProp<Vector3>(fProp, v3, offset, AbsoluteBitIndex - offset);
				case SendPropType.Vector2:
					DecodeVector2(fProp.PropInfo, out Vector2 v2);
					return new SingleEntProp<Vector2>(fProp, v2, offset, AbsoluteBitIndex - offset);
				case SendPropType.String:
					string s = DecodeString();
					return new SingleEntProp<string>(fProp, s, offset, AbsoluteBitIndex - offset);
				case SendPropType.Array:
					return fProp.ArrayElementPropInfo!.SendPropType switch {
						SendPropType.Int => new ArrEntProp<int>(fProp, DecodeIntArr(fProp), offset, AbsoluteBitIndex - offset),
						SendPropType.Float => new ArrEntProp<float>(fProp, DecodeFloatArr(fProp), offset, AbsoluteBitIndex - offset),
						SendPropType.Vector3 => new ArrEntProp<Vector3>(fProp, DecodeVector3Arr(fProp), offset, AbsoluteBitIndex - offset),
						SendPropType.Vector2 => new ArrEntProp<Vector2>(fProp, DecodeVector2Arr(fProp), offset, AbsoluteBitIndex - offset),
						SendPropType.String => new ArrEntProp<string>(fProp, DecodeStringArr(fProp), offset, AbsoluteBitIndex - offset),
						_ => throw new ArgumentException(exceptionMsg, nameof(fProp.PropInfo.SendPropType))
					};
				}
			throw new ArgumentException(exceptionMsg, nameof(fProp.PropInfo.SendPropType));
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
		public const int   MaxVarInt32Bytes  = 5;
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
