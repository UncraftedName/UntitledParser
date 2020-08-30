#nullable enable
using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.HelperClasses.EntityStuff.SendPropEnums;

namespace DemoParser.Parser.HelperClasses.EntityStuff {

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


		public SendTableProp(SourceDemo demoRef, BitStreamReader reader, SendTable tableRef) : base(demoRef, reader) {
			TableRef = tableRef;
		}


		internal override void ParseStream(BitStreamReader bsr) {
			SendPropType = DemoSettings.SendPropTypes[(int)bsr.ReadBitsAsUInt(5)];
			Name = bsr.ReadNullTerminatedString();
			Flags = (int)bsr.ReadBitsAsUInt(DemoSettings.SendPropFlagBits);
			if (DemoSettings.NewDemoProtocol)
				Priority = bsr.ReadByte();
			if (SendPropType == SendPropType.DataTable || DemoSettings.PropFlagChecker.HasFlag(Flags, PropFlag.Exclude)) {
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
						NumBits = bsr.ReadBitsAsUInt(DemoSettings.SendPropNumBitsToGetNumBits);
						break;
					case SendPropType.Array:
						NumElements = bsr.ReadBitsAsUInt(10);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(SendPropType),
							$"Invalid prop type: {SendPropType}");
				}
			}
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"{SendPropType.ToString().ToLower(),-10}");
			AppendToWriterWithoutType(iw);
		}


		private void AppendToWriterWithoutType(IndentedWriter iw) {
			if (ExcludeDtName != null) {
				iw.Append($"{Name}{(ExcludeDtName == null ? "" : $" : {ExcludeDtName}")}".PadRight(50));
			} else {
				iw.Append($"{Name}".PadRight(60, '.'));
				switch (SendPropType) {
					case SendPropType.String:
					case SendPropType.Int:
					case SendPropType.Float:
					case SendPropType.Vector3:
					case SendPropType.Vector2:
						iw.Append($"low: {LowValue,-12} high: {HighValue,-12} {NumBits,3} bit{(NumBits == 1 ? "" : "s")}");
						break;
					case SendPropType.Array:
						iw.Append($"elements: {NumElements}");
						break;
					default:
						iw.Append($"unknown prop type: {SendPropType}");
						break;
				}
				iw.PadLastLine(120, ' ');
			}
			iw.Append($" flags: {DemoSettings.PropFlagChecker.ToFlagString(Flags)}");
			iw.FutureIndent++;
			if (DemoSettings.NewDemoProtocol) {
				iw.PadLastLine(155, ' ');
				iw.Append($" priority: {Priority}");
			}
			iw.FutureIndent--;
		}


		public string ToStringNoType() { // just for pretty toString() from the console app
			var iw = new IndentedWriter();
			AppendToWriterWithoutType(iw);
			return iw.ToString();
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