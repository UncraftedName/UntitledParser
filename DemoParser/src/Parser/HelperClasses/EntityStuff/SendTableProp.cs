#nullable enable
using System;
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
		public SendPropFlags Flags;
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
			Flags = (SendPropFlags)bsr.ReadBitsAsUInt(
				DemoRef.Header.DemoProtocol switch {
				2 => 11, // todo put in constants
				3 => 16,
				4 => 19
			});
			if (DemoSettings.NewDemoProtocol)
				Priority = bsr.ReadByte();
			if (SendPropType == SendPropType.DataTable || (Flags & SendPropFlags.Exclude) != 0) {
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
				iw.PadLastLine(130, ' ');
			}
			iw.Append($"flags: {Flags}");
			iw.FutureIndent++;
			if (DemoSettings.NewDemoProtocol) {
				iw.PadLastLine(170, ' ');
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


	[Flags]
	public enum SendPropFlags : uint { // https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/DT/SendTableProperty.cs
		/*None                = 0,
		Unsigned            = 1,
		Coord               = 1 << 1,
		NoScale             = 1 << 2,
		RoundDown           = 1 << 3,
		RoundUp             = 1 << 4,
		Normal              = 1 << 5,
		Exclude             = 1 << 6,
		XYZE                = 1 << 7,
		InsideArray         = 1 << 8,
		ProxyAlwaysYes      = 1 << 9,
		ChangesOften        = 1 << 10,
		IsAVectorElement    = 1 << 11,
		Collapsible         = 1 << 12,
		CoordMp             = 1 << 13,
		CoordMpLowPrecision = 1 << 14,
		CoordMpIntegral     = 1 << 15*/
		
		None                  = 0x0,
		Unsigned              = 0x1,
		Coord                 = 0x2,
		NoScale               = 0x4,
		RoundDown             = 0x8,
		RoundUp               = 0x10,
		Normal                = 0x20,
		Exclude               = 0x40,
		XYZE                  = 0x80,
		InsideArray           = 0x100,
		ProxyAlwaysYes        = 0x200,
		
		IsVectorElem          = 0x400,
		Collapsible           = 0x800,
		
		CoordMp               = 0x1000,
		CoordMpLowPrecision   = 0x2000,
		CoordMpIntegral       = 0x4000,
		CellCoord             = 0x8000, // todo ReadBitCellCoord
		CellCoordLowPrecision = 0x10000,
		CellCoordIntegral     = 0x20000,
		ChangesOften          = 0x40000
		
		
		/*ChangesOften        = 0x400,
		IsAVectorElement    = 0x800,
		Collapsible         = 0x1000,
		CoordMp             = 0x2000,
		CoordMpLowPrecision = 0x4000,
		CoordMpIntegral     = 0x8000*/
	}
}