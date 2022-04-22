using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcSendTable : DemoMessage {

		public bool NeedsDecoder;
		private BitStreamReader _props;
		public BitStreamReader Props => _props.FromBeginning();


		public SvcSendTable(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		// I don't think I've seen this in demos yet, I'll just log it for now and deal with it later
		protected override void Parse(ref BitStreamReader bsr) {
			NeedsDecoder = bsr.ReadBool();
			ushort len = bsr.ReadUShort();
			_props = bsr.Fork(len);
			DemoRef.LogError($"{GetType().Name}: unimplemented"); // todo se2007/engine/dt_send_eng.cpp line 800
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"needs decoder: {NeedsDecoder}");
			pw.Append($"data length in bits: {_props.BitLength}");
		}
	}
}
