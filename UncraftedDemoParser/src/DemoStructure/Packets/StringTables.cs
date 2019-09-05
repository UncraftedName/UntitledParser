using System.Diagnostics;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure.Packets {
	
	public class StringTables : DemoComponent {

		public StringTables(byte[] data, SourceDemo demoRef) : base(data, demoRef) {}


		public override void ParseBytes() {
			Debug.WriteLine("string tables packet not parsable yet");
		}

		public override void UpdateBytes() {
			Debug.WriteLine("string tables packet not parsable yet");
		}


		public override string ToString() {
			return $"\tData length: {Bytes.Length}";
		}
	}


	public class StringTable : DemoComponent {
		
		public int TableCount;
		public string TableName;
		public short EntryCount;
		public string EntryName;
		// short? EntrySize					// optional, not stored for simplicity
		public byte[] EntryData; 			// optional, (set if entry size is set)
		public short? NumOfClientEntries;	// optional
		public string ClientEntryName; 		// optional
		// short? ClientEntrySize			// optional, not stored for simplicity
		public byte[] ClientEntryData; 		// optional, (set if client entry size is set)


		public StringTable(byte[] data, SourceDemo demoRef) : base(data, demoRef) {}


		public override void ParseBytes() {
			BitFieldReader bfr = new BitFieldReader(Bytes);
			TableCount = bfr.ReadInt();
			TableName = bfr.ReadNullTerminatedString();
			EntryCount = bfr.ReadShort();
			EntryName = bfr.ReadNullTerminatedString();
			short? entrySize = bfr.ReadShortIfExists();
			if (entrySize.HasValue)
				EntryData = bfr.ReadBytes(entrySize.Value);
			NumOfClientEntries = bfr.ReadShortIfExists();
			ClientEntryName = bfr.ReadStringIfExists();
			short? clientEntrySize = bfr.ReadShortIfExists();
			if (clientEntrySize.HasValue)
				ClientEntryData = bfr.ReadBytes(clientEntrySize.Value);
		}

		public override void UpdateBytes() {
			BitFieldWriter bfw = new BitFieldWriter(EntryData.Length + ClientEntryData.Length + 20);
			bfw.WriteInt(TableCount);
			bfw.WriteString(TableName);
			bfw.WriteShort(EntryCount);
			bfw.WriteString(EntryName);
			bfw.WriteShortIfExists(EntryData == null ? null : (short?)EntryData.Length);
			bfw.WriteByteArrIfExists(EntryData);
			bfw.WriteShortIfExists(NumOfClientEntries);
			bfw.WriteStringIfExists(ClientEntryName);
			bfw.WriteShortIfExists(ClientEntryData == null ? null : (short?)ClientEntryData.Length);
			bfw.WriteByteArrIfExists(ClientEntryData);
			Bytes = bfw.Data;
		}
	}
}