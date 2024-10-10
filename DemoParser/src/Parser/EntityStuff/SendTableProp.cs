#nullable enable
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.EntityStuff.SendPropEnums;

namespace DemoParser.Parser.EntityStuff {

	public class SendTableProp : DemoComponent {

		public readonly SendTable TableRef;
		// these fields should only be set once in ParseStream()
		public SendPropType SendPropType;
		public string Name;
		public int Flags;
		public int? Priority;
		public string? ExcludeDtName;
		public float? LowValue;
		public float? HighValue;
		public uint? NumBits;
		public uint? NumElements;
		// cache for faster parsing
		internal FloatParseType? FloatParseType;


		public SendTableProp(SourceDemo? demoRef, SendTable tableRef) : base(demoRef) {
			TableRef = tableRef;
		}


		protected override void Parse(ref BitStreamReader bsr) {
			int type = (int)bsr.ReadUInt(5);
			if (type >= DemoInfo.SendPropTypes.Count) {
				bsr.SetOverflow();
				return;
			}
			SendPropType = DemoInfo.SendPropTypes[type];
			Name = bsr.ReadNullTerminatedString();
			Flags = (int)bsr.ReadUInt(DemoInfo.SendPropFlagBits);
			if (DemoInfo.NewDemoProtocol && !DemoInfo.Game.IsLeft4Dead1())
				Priority = bsr.ReadByte();
			if (SendPropType == SendPropType.DataTable || DemoInfo.PropFlagChecker.HasFlag(Flags, PropFlag.Exclude)) {
				ExcludeDtName = bsr.ReadNullTerminatedString();
			} else {
				switch (SendPropType) {
					case SendPropType.String:
					case SendPropType.Int:
					case SendPropType.Float:
					case SendPropType.Vector3:
					case SendPropType.Vector2:
						LowValue = bsr.ReadFloat();
						HighValue = bsr.ReadFloat();
						NumBits = bsr.ReadUInt(DemoInfo.SendPropNumBitsToGetNumBits);
						break;
					case SendPropType.Array:
						NumElements = bsr.ReadUInt(10);
						break;
					default:
						bsr.SetOverflow();
						return;
				}
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"{SendPropType.ToString().ToLower(),-10}");
			PrettyWriteWithoutType(pw);
		}


		private void PrettyWriteWithoutType(IPrettyWriter pw) {
			if (ExcludeDtName != null) {
				pw.Append($"{Name}{(ExcludeDtName == null ? "" : $" : {ExcludeDtName}")}".PadRight(50));
			} else {
				pw.Append($"{Name}".PadRight(60, '.'));
				switch (SendPropType) {
					case SendPropType.String:
					case SendPropType.Int:
					case SendPropType.Float:
					case SendPropType.Vector3:
					case SendPropType.Vector2:
						pw.Append($"low: {LowValue,-12} high: {HighValue,-12} {NumBits,3} bit{(NumBits == 1 ? "" : "s")}");
						break;
					case SendPropType.Array:
						pw.Append($"elements: {NumElements}");
						break;
					default:
						pw.Append($"unknown prop type: {SendPropType}");
						break;
				}
				pw.PadLastLine(120, ' ');
			}
			pw.Append($" flags: {DemoInfo.PropFlagChecker.ToFlagString(Flags)}");
			pw.FutureIndent++;
			if (DemoInfo.NewDemoProtocol) {
				pw.PadLastLine(155, ' ');
				pw.Append($" priority: {Priority}");
			}
			pw.FutureIndent--;
		}


		public string ToStringNoType() { // just for pretty toString() from the console app
			var pw = new PrettyToStringWriter();
			PrettyWriteWithoutType(pw);
			return pw.Contents;
		}
	}


	public static class SendPropEnums {

		public enum SendPropType {
			Int,
			Float,
			Vector3,
			Vector2, // called VectorXY internally
			String,
			Array,
			DataTable
		}


		#region send prop type enums for different versions

		public static readonly SendPropType[] OldNetPropTypes = {
			SendPropType.Int,
			SendPropType.Float,
			SendPropType.Vector3,
			SendPropType.String,
			SendPropType.Array,
			SendPropType.DataTable
		};

		public static readonly SendPropType[] NewNetPropTypes = {
			SendPropType.Int,
			SendPropType.Float,
			SendPropType.Vector3,
			SendPropType.Vector2,
			SendPropType.String,
			SendPropType.Array,
			SendPropType.DataTable
		};

		#endregion


		// csgo: https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/DT/SendTableProperty.cs
		public enum PropFlag {
			Unsigned,
			Coord,
			NoScale,
			RoundDown,
			RoundUp,
			Normal,
			Exclude,
			Xyze,
			InsideArray,
			ProxyAlwaysYes,
			IsVectorElem,
			Collapsible,
			CoordMp,
			CoordMpLp, // low precision
			CoordMpInt,
			CellCoord,
			CellCoordLp,
			CellCoordInt,
			ChangesOften
		}


		#region prop flags for different versions

		public class DemoProtocol3FlagChecker : AbstractFlagChecker<PropFlag> {

			public override bool HasFlag(long val, PropFlag flag) {
				return flag switch {
					PropFlag.Unsigned       => CheckBit(val, 00),
					PropFlag.Coord          => CheckBit(val, 01),
					PropFlag.NoScale        => CheckBit(val, 02),
					PropFlag.RoundDown      => CheckBit(val, 03),
					PropFlag.RoundUp        => CheckBit(val, 04),
					PropFlag.Normal         => CheckBit(val, 05),
					PropFlag.Exclude        => CheckBit(val, 06),
					PropFlag.Xyze           => CheckBit(val, 07),
					PropFlag.InsideArray    => CheckBit(val, 08),
					PropFlag.ProxyAlwaysYes => CheckBit(val, 09),
					PropFlag.ChangesOften   => CheckBit(val, 10),
					PropFlag.IsVectorElem   => CheckBit(val, 11),
					PropFlag.Collapsible    => CheckBit(val, 12),
					PropFlag.CoordMp        => CheckBit(val, 13),
					PropFlag.CoordMpLp      => CheckBit(val, 14),
					PropFlag.CoordMpInt     => CheckBit(val, 15),
					_ => false
				};
			}
		}

		public class DemoProtocol4FlagChecker : AbstractFlagChecker<PropFlag> {

			public override bool HasFlag(long val, PropFlag flag) {
				return flag switch {
					PropFlag.Unsigned       => CheckBit(val, 00),
					PropFlag.Coord          => CheckBit(val, 01),
					PropFlag.NoScale        => CheckBit(val, 02),
					PropFlag.RoundDown      => CheckBit(val, 03),
					PropFlag.RoundUp        => CheckBit(val, 04),
					PropFlag.Normal         => CheckBit(val, 05),
					PropFlag.Exclude        => CheckBit(val, 06),
					PropFlag.Xyze           => CheckBit(val, 07),
					PropFlag.InsideArray    => CheckBit(val, 08),
					PropFlag.ProxyAlwaysYes => CheckBit(val, 09),
					PropFlag.IsVectorElem   => CheckBit(val, 10),
					PropFlag.Collapsible    => CheckBit(val, 11),
					PropFlag.CoordMp        => CheckBit(val, 12),
					PropFlag.CoordMpLp      => CheckBit(val, 13),
					PropFlag.CoordMpInt     => CheckBit(val, 14),
					PropFlag.CellCoord      => CheckBit(val, 15),
					PropFlag.CellCoordLp    => CheckBit(val, 16),
					PropFlag.CellCoordInt   => CheckBit(val, 17),
					PropFlag.ChangesOften   => CheckBit(val, 18),
					_ => false
				};
			}
		}

		#endregion
	}
}
