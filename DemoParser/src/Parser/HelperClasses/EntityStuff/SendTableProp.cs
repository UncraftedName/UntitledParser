#nullable enable
using System;
using System.Linq;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.HelperClasses.EntityStuff {
	
	public class SendTableProp : DemoComponent {
		
		public readonly SendTable TableRef;
		// these fields should only be set once in ParseStream()
		public SendPropType SendPropType;
		public string Name;
		public uint Flags;
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
			SendPropType = UIntToSendPropertyType(DemoRef, bsr.ReadBitsAsUInt(5));
			Name = bsr.ReadNullTerminatedString();
			Flags = bsr.ReadBitsAsUInt(DemoSettings.SendPropFlagBits);
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
						NumBits = bsr.ReadBitsAsUInt(
							DemoRef.Header.NetworkProtocol == 14 || 
							DemoSettings.Game == SourceGame.L4D2_2000 ||
							DemoSettings.Game == SourceGame.L4D2_2042
								? 6 : 7);
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
			iw.Append($"{SendPropType.ToString().ToLower(), -10}");
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
						iw.Append($"low: {LowValue, -12} high: {HighValue, -12} {NumBits, 3} bit{(NumBits == 1 ? "" : "s")}");
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


		public static SendPropType UIntToSendPropertyType(SourceDemo demoRef, uint i) {
			if (demoRef.Header.NetworkProtocol <= 14) {
				if (i >= 3) // vectorXY/vec2 doesn't exist in leak/3420 build of portal
					i++;
			}
			return (SendPropType)i;
		}
	}


	public enum SendPropType : uint {
		Int,
		Float,
		Vector3,
		Vector2, // called VectorXY internally
		String,
		Array,
		DataTable
	}

	/*
	 * The flags are actually different depending on the protocol version, which is a big heck. Since remapping flags
	 * to a different set of flags would be not too hot and revolve around iterating over every bit and the like, what
	 * I've done here is create a non-flag enum that you can pass to a separate object along with your int which will
	 * actually determine if your desired flag is set. That object will be stored in DemoSettings.cs.
	 * Flags for demo protocol 3 can be found in src_main/public/dt_common.h
	 * csgo flags can be found here: https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/DT/SendTableProperty.cs
	 */
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


	public abstract class PropFlagChecker {

		private static readonly PropFlag[] PropFlags = (PropFlag[])Enum.GetValues(typeof(PropFlag));
		private static readonly string[] PropNames = Enum.GetNames(typeof(PropFlag));
		private static readonly int MaxStrLen = PropNames.Select(s => s.Length + 2).Sum() - 2;
		

		public static PropFlagChecker CreateFromDemoHeader(DemoHeader h) {
			return h.DemoProtocol switch {
				3 => new DemoProtocol3FlagChecker(),
				4 => new DemoProtocol4FlagChecker(),
				_ => throw new ArgumentException($"There is no prop flag list for demo protocol {h.DemoProtocol}")
			};
		}
		
		
		public abstract bool HasFlag(uint val, PropFlag flag);
		

		public bool HasFlags(uint val, PropFlag f1, PropFlag f2) {
			return HasFlag(val, f1) || HasFlag(val, f2);
		}


		public string ToFlagString(uint val) {
			Span<char> str = stackalloc char[MaxStrLen];
			int len = 0;
			int flagCount = 0;
			for (int i = 0; i < PropFlags.Length; i++) {
				if (HasFlag(val, PropFlags[i])) {
					if (flagCount++ > 0) {
						", ".AsSpan().CopyTo(str.Slice(len));
						len += 2;
					}
					PropNames[i].AsSpan().CopyTo(str.Slice(len));
					len += PropNames[i].Length;
				}
			}
			return len == 0 ? "None" : str.Slice(0, len).ToString();
		}


		private class DemoProtocol3FlagChecker : PropFlagChecker {
			
			public override bool HasFlag(uint val, PropFlag flag) {
				return flag switch {
					PropFlag.Unsigned       => (val & 1) != 00,
					PropFlag.Coord          => (val & (1 << 01)) != 0,
					PropFlag.NoScale        => (val & (1 << 02)) != 0,
					PropFlag.RoundDown      => (val & (1 << 03)) != 0,
					PropFlag.RoundUp        => (val & (1 << 04)) != 0,
					PropFlag.Normal         => (val & (1 << 05)) != 0,
					PropFlag.Exclude        => (val & (1 << 06)) != 0,
					PropFlag.Xyze           => (val & (1 << 07)) != 0,
					PropFlag.InsideArray    => (val & (1 << 08)) != 0,
					PropFlag.ProxyAlwaysYes => (val & (1 << 09)) != 0,
					PropFlag.ChangesOften   => (val & (1 << 10)) != 0,
					PropFlag.IsVectorElem   => (val & (1 << 11)) != 0,
					PropFlag.Collapsible    => (val & (1 << 12)) != 0,
					PropFlag.CoordMp        => (val & (1 << 13)) != 0,
					PropFlag.CoordMpLp      => (val & (1 << 14)) != 0,
					PropFlag.CoordMpInt     => (val & (1 << 15)) != 0,
					_ => false
				};
			}
		}
		
		
		private class DemoProtocol4FlagChecker : PropFlagChecker {
			
			public override bool HasFlag(uint val, PropFlag flag) {
				return flag switch {
					PropFlag.Unsigned       => (val & 1) != 00,
					PropFlag.Coord          => (val & (1 << 01)) != 0,
					PropFlag.NoScale        => (val & (1 << 02)) != 0,
					PropFlag.RoundDown      => (val & (1 << 03)) != 0,
					PropFlag.RoundUp        => (val & (1 << 04)) != 0,
					PropFlag.Normal         => (val & (1 << 05)) != 0,
					PropFlag.Exclude        => (val & (1 << 06)) != 0,
					PropFlag.Xyze           => (val & (1 << 07)) != 0,
					PropFlag.InsideArray    => (val & (1 << 08)) != 0,
					PropFlag.ProxyAlwaysYes => (val & (1 << 09)) != 0,
					PropFlag.IsVectorElem   => (val & (1 << 10)) != 0,
					PropFlag.Collapsible    => (val & (1 << 11)) != 0,
					PropFlag.CoordMp        => (val & (1 << 12)) != 0,
					PropFlag.CoordMpLp      => (val & (1 << 13)) != 0,
					PropFlag.CoordMpInt     => (val & (1 << 14)) != 0,
					PropFlag.CellCoord      => (val & (1 << 15)) != 0,
					PropFlag.CellCoordLp    => (val & (1 << 16)) != 0,
					PropFlag.CellCoordInt   => (val & (1 << 17)) != 0,
					PropFlag.ChangesOften   => (val & (1 << 18)) != 0,
					_ => false
				};
			}
		}
	}
}