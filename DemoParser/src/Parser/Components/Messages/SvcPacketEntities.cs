using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcPacketEntities : DemoMessage {

		public ushort MaxEntries;
		public bool IsDelta;
		public int? DeltaFrom;
		public bool BaseLine;
		public ushort UpdatedEntries;
		public bool UpdateBaseline;
		private BitStreamReader _data;
		public BitStreamReader Data => _data.FromBeginning();
		

		public SvcPacketEntities(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			MaxEntries = (ushort)bsr.ReadBitsAsUInt(11);
			IsDelta = bsr.ReadBool();
			DeltaFrom = IsDelta ? (int)bsr.ReadUInt() : -1;
			BaseLine = bsr.ReadBool();
			UpdatedEntries = (ushort)bsr.ReadBitsAsUInt(11);
			uint dataLen = bsr.ReadBitsAsUInt(20);
			UpdateBaseline = bsr.ReadBool();
			_data = bsr.SubStream(dataLen);
			bsr.SkipBits(dataLen);
			SetLocalStreamEnd(bsr);

			// src_main/engine/cl_ents_parse.cpp line 544
			if (!IsDelta) {
				// Clear out the client's entity states..
			}

			if (UpdateBaseline) { // server requested to use this snapshot as baseline update
				// src_main/engine/baseclientstate.cpp   line 1420
			}
			// se2007/engine/baseclientstate.cpp   line 1247
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"max entries: {MaxEntries}");
			iw.AppendLine($"is delta: {IsDelta}");
			if (IsDelta)
				iw.AppendLine($"delta from: {DeltaFrom}");
			iw.AppendLine($"baseline: {BaseLine}");
			iw.AppendLine($"updated entries: {UpdatedEntries}");
			iw.AppendLine($"updated baseline: {UpdateBaseline}");
			iw.Append($"length in bits: {_data.BitLength}");
		}
	}
	
	
	public enum UpdateType { // src_main/common/protocol.h
		EnterPvs = 0, 	// Entity came back into pvs, create new entity if one doesn't exist
		LeavePvs, 		// Entity left pvs
		DeltaEnt,    	// There is a delta for this entity.
		PreserveEnt, 	// Entity stays alive but no delta ( could be LOD, or just unchanged )
		Finished, 		// finished parsing entities successfully
		Failed,   		// parsing error occured while reading entities
	}
	

	// Flags for delta encoding header
	public enum DeltaHeaderFHDR {
	Zero			= 0x0000,
	LeavePvs		= 0x0001,
	Delete			= 0x0002,
	EnterPvs		= 0x0004,
	}
}