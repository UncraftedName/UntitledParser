#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Parser.Components.Messages;
using UntitledParser.Parser.HelperClasses;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Packets {
	
	public class DataTables : DemoPacket {

		public List<SendTable> Tables;
		public List<ServerClass>? Classes;
		
		
		public DataTables(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader, tick) {}


		internal override void ParseStream(BitStreamReader bsr) {
			int byteSize = (int)bsr.ReadUInt();
			int indexBeforeData = bsr.CurrentBitIndex;
			try {
				Tables = new List<SendTable>();
				while (bsr.ReadBool()) {
					Tables.Add(new SendTable(DemoRef, bsr));
					Tables[^1].ParseStream(bsr);
				}

				ushort classCount = bsr.ReadUShort();
				Classes = new List<ServerClass>(classCount);
				for (int i = 0; i < classCount; i++) {
					Classes.Add(new ServerClass(DemoRef, Reader));
					Classes[^1].ParseStream(bsr);
				}
				
				DemoRef.DataTableParser = new DataTableParser(DemoRef, this);
				DemoRef.DataTableParser.FlattenClasses();
			}
			catch (Exception e) {
				DemoRef.AddError($"exception while parsing datatables\n\texception: {e.Message}");
				Debug.WriteLine(e);
			}

			bsr.CurrentBitIndex = indexBeforeData + (byteSize << 3);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			Debug.Assert(Tables.Count > 0, "there's no tables hmmmmmmmmmmmm");
			iw.Append($"{Tables.Count} send table{(Tables.Count > 1 ? "s" : "")}:");
			iw.AddIndent();
			foreach (SendTable sendTable in Tables) {
				iw.AppendLine();
				sendTable.AppendToWriter(iw);
			}
			iw.SubIndent();
			iw.AppendLine();
			if ((Classes?.Count ?? 0) > 0) {
				iw.Append($"{Classes.Count} class{(Classes.Count > 1 ? "es" : "")}:");
				iw.AddIndent();
				foreach (ServerClass classInfo in Classes) {
					iw.AppendLine();
					classInfo.AppendToWriter(iw);
				}
				iw.SubIndent();
			} else {
				iw.Append("no classes");
			}
		}
	}
	
	
	public class SendTable : DemoComponent {

		public bool NeedsDecoder;
		public string Name;
		public List<SendTableProperty> Properties;
		
		
		public SendTable(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			NeedsDecoder = bsr.ReadBool();
			Name = bsr.ReadNullTerminatedString();
			uint propCount = bsr.ReadBitsAsUInt(10);
			Properties = new List<SendTableProperty>((int)propCount);
			for (int i = 0; i < propCount; i++) {
				Properties.Add(new SendTableProperty(DemoRef, bsr, this));
				Properties[^1].ParseStream(bsr);
			}
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"{Name}{(NeedsDecoder ? "*" : "")} (");
			if (Properties.Count > 0) {
				iw.Append($"{Properties.Count} prop{(Properties.Count > 1 ? "s" : "")}):");
				iw.AddIndent();
				foreach (SendTableProperty sendProp in Properties) {
					iw.AppendLine();
					sendProp.AppendToWriter(iw);
				}
				iw.SubIndent();
			} else {
				iw.Append("no props)");
			}
		}
	}
	
	
	public class SendTableProperty : DemoComponent {
		
		internal readonly SendTable TableRef;
		public SendPropertyType SendPropertyType;
		public string Name;
		public SendPropertyFlags Flags;
		public int? Priority;
		public string? ExcludeDtName;
		public float? LowValue;
		public float? HighValue;
		public uint? NumBits;
		public uint? Elements;
		

		public SendTableProperty(SourceDemo demoRef, BitStreamReader reader, SendTable tableRef) : base(demoRef, reader) {
			TableRef = tableRef;
		}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			SendPropertyType = UIntToSendPropertyType(DemoRef, bsr.ReadBitsAsUInt(5));
			Name = bsr.ReadNullTerminatedString();
			Flags = (SendPropertyFlags)bsr.ReadBitsAsUInt(DemoRef.Header.DemoProtocol == 2 ? 11 : 16);
			if (DemoRef.DemoSettings.NewEngine)
				Priority = bsr.ReadBitsAsSInt(11);
			if (SendPropertyType == SendPropertyType.DataTable || (Flags & SendPropertyFlags.Exclude) != 0) {
				ExcludeDtName = bsr.ReadNullTerminatedString();
			} else {
				switch (SendPropertyType) {
					case SendPropertyType.String:
					case SendPropertyType.Int:
					case SendPropertyType.Float:
					case SendPropertyType.Vector:
					case SendPropertyType.VectorXY:
						LowValue = bsr.ReadFloat();
						HighValue = bsr.ReadFloat();
						NumBits = bsr.ReadBitsAsUInt(
							DemoRef.Header.NetworkProtocol == 14 || 
							DemoRef.DemoSettings.Game == SourceDemoSettings.SourceGame.L4D2_2000 
								? 6 : 7);
						break;
					case SendPropertyType.Array:
						Elements = bsr.ReadBitsAsUInt(10);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(SendPropertyType),
							$"Invalid prop type: {SendPropertyType}");
				}
			}
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"{SendPropertyType.ToString().ToLower(), -10}");
			if (ExcludeDtName != null) {
				iw.Append($"{Name}{(ExcludeDtName == null ? "" : $" : {ExcludeDtName}")}".PadRight(50));
			} else {
				iw.Append($"{Name}".PadRight(60, '.'));
				switch (SendPropertyType) {
					case SendPropertyType.String:
					case SendPropertyType.Int:
					case SendPropertyType.Float:
					case SendPropertyType.Vector:
					case SendPropertyType.VectorXY:
						iw.Append($"low: {LowValue, -12} high: {HighValue, -12} {NumBits, 3} bit{(NumBits == 1 ? "" : "s")}");
						break;
					case SendPropertyType.Array:
						iw.Append($"elements: {Elements}");
						break;
					default:
						iw.Append($"unknown prop type: {SendPropertyType}");
						break;
				}
				iw.PadLastLine(130, ' ');
			}
			iw.Append($"flags: {Flags}");
			iw.AddIndent();
			if (DemoRef.DemoSettings.NewEngine) {
				iw.PadLastLine(170, ' ');
				iw.Append($" priority: {Priority}");
			}

			iw.SubIndent();
		}


		public static SendPropertyType UIntToSendPropertyType(SourceDemo demoRef, uint i) {
			if (demoRef.Header.NetworkProtocol == 14) {
				if (i >= 3) // vectorXY doesn't exist in leak/3420 build of portal
					i++;
			}
			return (SendPropertyType)i;
		}
	}


	public enum SendPropertyType : uint {
		Int,
		Float,
		Vector,
		VectorXY,
		String,
		Array,
		DataTable
	}


	[Flags]
	public enum SendPropertyFlags : uint { // https://github.com/StatsHelix/demoinfo/blob/ac3e820d68a5a76b1c4c86bf3951e9799f669a56/DemoInfo/DT/SendTableProperty.cs
		NoFlags						= 0,
		Unsigned 					= 1,
		Coord 						= 1 << 1,
		NoScale 					= 1 << 2,
		RoundDown					= 1 << 3,
		RoundUp						= 1 << 4,
		Normal						= 1 << 5,
		Exclude						= 1 << 6,
		XYZE						= 1 << 7,
		InsideArray					= 1 << 8,
		ProxyAlwaysYes				= 1 << 9,
		ChangesOften				= 1 << 10,
		IsAVectorElement			= 1 << 11,
		Collapsible					= 1 << 12,
		CoordMp						= 1 << 13,
		CoordMpLowPrecision			= 1 << 14,
		CoordMpLowIntegral			= 1 << 15
		// CellCoordMp				= 1 << 16,
		// CellCoordMpLowPrecision	= 1 << 17,
		// CellCoordMpLowIntegral	= 1 << 18,
		// ChangesOften				= 1 << 18,
		// VarInt					= 1 << 19
	}
}