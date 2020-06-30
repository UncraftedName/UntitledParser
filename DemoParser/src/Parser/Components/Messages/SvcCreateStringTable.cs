using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcCreateStringTable : DemoMessage {

		public string TableName;
		public short MaxEntries;
		public int NumEntries;
		public bool UserDataFixedSize;
		public int UserDataSize;
		public int UserDataSizeBits;
		public StringTableFlags? Flags;
		public StringTableUpdate TableUpdate;
		

		public SvcCreateStringTable(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			TableName = bsr.ReadNullTerminatedString();
			MaxEntries = (short)bsr.ReadUShort();
			NumEntries = (int)bsr.ReadBitsAsUInt(BitUtils.HighestBitIndex(MaxEntries) + 1);
			uint dataLen = bsr.ReadBitsAsUInt(20);
			UserDataFixedSize = bsr.ReadBool();
			UserDataSize = (int)(UserDataFixedSize ? bsr.ReadBitsAsUInt(12) : 0);
			UserDataSizeBits = (int)(UserDataFixedSize ? bsr.ReadBitsAsUInt(4) : 0);
			if (DemoRef.Header.NetworkProtocol >= 15)
				Flags = (StringTableFlags)bsr.ReadBitsAsUInt(DemoSettings.NewDemoProtocol ? 2 : 1);
			
			DemoRef.CStringTablesManager.CreateStringTable(this);
			TableUpdate = new StringTableUpdate(DemoRef, bsr.SubStream(dataLen), TableName, NumEntries);
			TableUpdate.ParseOwnStream();
			
			bsr.SkipBits(dataLen);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"name: {TableName}");
			iw.AppendLine($"max entries: {MaxEntries}");
			iw.AppendLine($"number of entries: {NumEntries}");
			iw.AppendLine($"user data fixed size: {UserDataFixedSize}");
			iw.AppendLine($"user data size: {UserDataSize}");
			iw.AppendLine($"user data size in bits: {UserDataSizeBits}");
			if (DemoRef.Header.NetworkProtocol >= 15)
				iw.AppendLine($"flags: {Flags}");
			iw.Append("table update:");
			iw.FutureIndent++;
			iw.AppendLine();
			if (TableUpdate == null)
				iw.Append("table update could not be parsed");
			else
				TableUpdate.AppendToWriter(iw);
			iw.FutureIndent--;
		}
	}
	
	
	[Flags]
	public enum StringTableFlags : uint { // hl2sdk-portal2  public/networkstringtabledefs.h   line 60
		None              = 0,
		DictionaryEnabled = 1,
		Unknown           = 1 << 1,
		// V "I created this SvcCreateStringTable object myself and it's fake" V
		Fake              = 1 << 30
	} 
}