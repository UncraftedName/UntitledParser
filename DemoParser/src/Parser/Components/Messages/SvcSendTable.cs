using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcSendTable : DemoMessage {

		public bool NeedsDecoder;
		private BitStreamReader _props;
		public BitStreamReader Props => _props.FromBeginning();
		

		public SvcSendTable(SourceDemo? demoRef) : base(demoRef) {}
		
		
		// I don't think I've seen this in demos yet, I'll just log it for now and deal with it later
		protected override void Parse(ref BitStreamReader bsr) {
			NeedsDecoder = bsr.ReadBool();
			ushort len = bsr.ReadUShort();
			_props = bsr.Split(len);
			DemoRef.LogError($"unprocessed {GetType().Name} message"); // todo se2007/engine/dt_send_eng.cpp line 800
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"needs decoder: {NeedsDecoder}");
			pw.Append($"data length in bits: {_props.BitLength}");
		}
	}
}