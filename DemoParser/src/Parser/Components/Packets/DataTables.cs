#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {
	/*
	 * So fuck me this is pretty complicated, but this packet consists of a bunch of tables, each of which contain a
	 * list of properties and/or references to other tables. See the data tables manager to see how those properties
	 * are unpacked and flattened. Once they are, they serve as a massive lookup table to decode entity properties.
	 * For instance, when reading the entity info, it will say something along the lines of 'update the 3rd property
	 * of the 107th class'. Then you would use the lookup tables to determine that that corresponds to the m_vecOrigin
	 * property of the 'CProp_Portal' class, which has a corresponding data table called 'DT_Prop_Portal'.
	 * That property is also a vector 3, and must be read accordingly. Needless to say, it gets a little wack.
	 */
	
	/// <summary>
	/// Contains all the information needed to decode entity properties.
	/// </summary>
	public class DataTables : DemoPacket {

		public List<SendTable> Tables;
		public List<ServerClass>? ServerClasses;
		
		
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
				ServerClasses = new List<ServerClass>(classCount);
				for (int i = 0; i < classCount; i++) {
					ServerClasses.Add(new ServerClass(DemoRef, Reader));
					ServerClasses[^1].ParseStream(bsr);
					// this is an assumption I make in the structure of all the entity stuff, very critical
					if (i != ServerClasses[i].DataTableId)
						throw new ConstraintException("server class ID does not match it's index in the list");
				}
				
				// re-init the baselines if the count doesn't match (maybe I should just init them from here?)
				if (DemoRef.CBaseLines.ClassBaselines.Length != classCount)
					DemoRef.CBaseLines.ClearBaseLineState(classCount);
				
				// create the prop list for each class
				DemoRef.DataTableParser = new DataTableParser(DemoRef, this);
				DemoRef.DataTableParser.FlattenClasses();
			} catch (Exception e) {
				DemoRef.AddError($"exception while parsing datatables\n\texception: {e.Message}");
				Debug.WriteLine(e);
			}

			bsr.CurrentBitIndex = indexBeforeData + (byteSize << 3);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			Debug.Assert(Tables.Count > 0, "there's no tables hmmmmmmmmmmmm");
			iw.Append($"{Tables.Count} send table{(Tables.Count > 1 ? "s" : "")}:");
			iw.FutureIndent++;
			foreach (SendTable sendTable in Tables) {
				iw.AppendLine();
				sendTable.AppendToWriter(iw);
			}
			iw.FutureIndent--;
			iw.AppendLine();
			if ((ServerClasses?.Count ?? 0) > 0) {
				iw.Append($"{ServerClasses.Count} class{(ServerClasses.Count > 1 ? "es" : "")}:");
				iw.FutureIndent++;
				foreach (ServerClass classInfo in ServerClasses) {
					iw.AppendLine();
					classInfo.AppendToWriter(iw);
				}
				iw.FutureIndent--;
			} else {
				iw.Append("no classes");
			}
		}
	}
	
	
	public class SendTable : DemoComponent {

		public bool NeedsDecoder;
		public string Name;
		public List<SendTableProp> Properties;
		
		
		public SendTable(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			NeedsDecoder = bsr.ReadBool();
			Name = bsr.ReadNullTerminatedString();
			uint propCount = bsr.ReadBitsAsUInt(10);
			Properties = new List<SendTableProp>((int)propCount);
			for (int i = 0; i < propCount; i++) {
				Properties.Add(new SendTableProp(DemoRef, bsr, this));
				Properties[^1].ParseStream(bsr);
			}
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"{Name}{(NeedsDecoder ? "*" : "")} (");
			if (Properties.Count > 0) {
				iw.Append($"{Properties.Count} prop{(Properties.Count > 1 ? "s" : "")}):");
				iw.FutureIndent++;
				foreach (SendTableProp sendProp in Properties) {
					iw.AppendLine();
					sendProp.AppendToWriter(iw);
				}
				iw.FutureIndent--;
			} else {
				iw.Append("no props)");
			}
		}
	}
	
	
	public class SendTableProp : DemoComponent, IEquatable<SendTableProp> {
		
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
		public uint? Elements;
		

		public SendTableProp(SourceDemo demoRef, BitStreamReader reader, SendTable tableRef) : base(demoRef, reader) {
			TableRef = tableRef;
		}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			SendPropType = UIntToSendPropertyType(DemoRef, bsr.ReadBitsAsUInt(5));
			Name = bsr.ReadNullTerminatedString();
			Flags = (SendPropFlags)bsr.ReadBitsAsUInt(DemoRef.Header.DemoProtocol == 2 ? 11 : 16);
			if (DemoSettings.NewEngine)
				Priority = bsr.ReadBitsAsSInt(11);
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
						Elements = bsr.ReadBitsAsUInt(10);
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
						iw.Append($"elements: {Elements}");
						break;
					default:
						iw.Append($"unknown prop type: {SendPropType}");
						break;
				}
				iw.PadLastLine(130, ' ');
			}
			iw.Append($"flags: {Flags}");
			iw.FutureIndent++;
			if (DemoSettings.NewEngine) {
				iw.PadLastLine(170, ' ');
				iw.Append($" priority: {Priority}");
			}

			iw.FutureIndent--;
		}


		public static SendPropType UIntToSendPropertyType(SourceDemo demoRef, uint i) {
			if (demoRef.Header.NetworkProtocol <= 14) {
				if (i >= 3) // vectorXY/vec2 doesn't exist in leak/3420 build of portal
					i++;
			}
			return (SendPropType)i;
		}


		public bool Equals(SendTableProp? other) {
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return TableRef.Equals(other.TableRef) 
				   && SendPropType == other.SendPropType 
				   && Name == other.Name 
				   && Flags == other.Flags 
				   && Priority == other.Priority 
				   && ExcludeDtName == other.ExcludeDtName 
				   && Nullable.Equals(LowValue, other.LowValue) 
				   && Nullable.Equals(HighValue, other.HighValue) 
				   && NumBits == other.NumBits 
				   && Elements == other.Elements;
		}


		public override bool Equals(object? obj) {
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			return obj.GetType() == GetType() && Equals((SendTableProp)obj);
		}


		[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")] // fields should only be set once
		public override int GetHashCode() {
			var hashCode = new HashCode();
			hashCode.Add(TableRef);
			hashCode.Add((int)SendPropType);
			hashCode.Add(Name);
			hashCode.Add((int)Flags);
			hashCode.Add(Priority);
			hashCode.Add(ExcludeDtName);
			hashCode.Add(LowValue);
			hashCode.Add(HighValue);
			hashCode.Add(NumBits);
			hashCode.Add(Elements);
			return hashCode.ToHashCode();
		}


		public static bool operator ==(SendTableProp left, SendTableProp right) {
			return Equals(left, right);
		}


		public static bool operator !=(SendTableProp left, SendTableProp right) {
			return !Equals(left, right);
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
		None                = 0,
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
		CoordMpIntegral     = 1 << 15
		
		// we read 16 bits, so there's probably an additional flag that i'm missing, but these are only used in csg
		
		// CellCoordMp             = 1 << 16,
		// CellCoordMpLowPrecision = 1 << 17,
		// CellCoordMpLowIntegral  = 1 << 18,
		// ChangesOften            = 1 << 18,
		// VarInt                  = 1 << 19
	}
}