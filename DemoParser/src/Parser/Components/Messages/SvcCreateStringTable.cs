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
		public StringTableUpdates? TableUpdates;
		

		public SvcCreateStringTable(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			TableName = bsr.ReadNullTerminatedString();
			MaxEntries = (short)bsr.ReadUShort();
			NumEntries = (int)bsr.ReadBitsAsUInt(BitUtils.HighestBitIndex(MaxEntries) + 1);
			uint dataLen = bsr.ReadBitsAsUInt(20);
			UserDataFixedSize = bsr.ReadBool();
			UserDataSize = (int)(UserDataFixedSize ? bsr.ReadBitsAsUInt(12) : 0);
			UserDataSizeBits = (int)(UserDataFixedSize ? bsr.ReadBitsAsUInt(4) : 0);
			if (DemoRef.Header.NetworkProtocol >= 15)
				Flags = (StringTableFlags)bsr.ReadBitsAsUInt(DemoSettings.NewDemoProtocol ? 2 : 1);
			
			DemoRef.CurStringTablesManager.CreateStringTable(this);
			TableUpdates = new StringTableUpdates(DemoRef, TableName, NumEntries);
			TableUpdates.ParseStream(bsr.SplitAndSkip(dataLen));
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
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
			if (TableUpdates == null)
				iw.Append("table update could not be parsed");
			else
				TableUpdates.PrettyWrite(iw);
			iw.FutureIndent--;
		}
	}
	
	
	[Flags]
	public enum StringTableFlags : uint { // hl2sdk-portal2  public/networkstringtabledefs.h   line 60
		None              = 0,
		DictionaryEnabled = 1,
		Unknown           = 1 << 1,
		
		Fake              = 1 << 30 // I created this SvcCreateStringTable object myself and it's fake
	} 
}