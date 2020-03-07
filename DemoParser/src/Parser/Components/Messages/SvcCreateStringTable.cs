using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcCreateStringTable : DemoMessage {

		public string Name;
		public ushort MaxEntries; // todo pass to string tables (count/max)
		public int NumEntries;
		public bool UserDataFixedSize;
		public int UserDataSize;
		public uint UserDataSizeBits;
		public StringTableFlags? Flags;
		public StringTableUpdate TableUpdate;
		

		public SvcCreateStringTable(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Name = bsr.ReadNullTerminatedString();
			MaxEntries = bsr.ReadUShort();
			NumEntries = (int)bsr.ReadBitsAsUInt(BitUtils.HighestBitIndex(MaxEntries) + 1);
			uint dataLen = bsr.ReadBitsAsUInt(20);
			UserDataFixedSize = bsr.ReadBool();
			UserDataSize = (int)(UserDataFixedSize ? bsr.ReadBitsAsUInt(12) : 0);
			UserDataSizeBits = UserDataFixedSize ? bsr.ReadBitsAsUInt(4) : 0;
			if (DemoRef.Header.NetworkProtocol >= 15)
				Flags = (StringTableFlags)bsr.ReadBitsAsUInt(DemoRef.DemoSettings.NewEngine ? 2 : 1);
			
			DemoRef.CStringTablesManager.CreateStringTable(this);
			TableUpdate = new StringTableUpdate(DemoRef, bsr.SubStream(dataLen), Name, NumEntries);
			TableUpdate.ParseOwnStream();
			
			bsr.SkipBits(dataLen);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"name: {Name}");
			iw.AppendLine($"max entries: {MaxEntries}");
			iw.AppendLine($"number of entries: {NumEntries}");
			iw.AppendLine($"user data fixed size: {UserDataFixedSize}");
			iw.AppendLine($"user data size: {UserDataSize}");
			iw.AppendLine($"user data size in bits: {UserDataSizeBits}");
			if (DemoRef.Header.NetworkProtocol >= 15)
				iw.AppendLine($"flags: {Flags}");
			iw.Append("table update:");
			iw.AddIndent();
			iw.AppendLine();
			TableUpdate.AppendToWriter(iw);
			iw.SubIndent();
		}
	}
	
	
	[Flags]
	public enum StringTableFlags : uint { // hl2sdk-portal2  public/networkstringtabledefs.h   line 60
		None 				= 0,
		DictionaryEnabled 	= 1, // probably for lookup
		// idk what 2 is yet
	} 
}