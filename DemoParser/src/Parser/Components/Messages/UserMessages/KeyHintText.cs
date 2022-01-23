using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class KeyHintText : UserMessage {

		public int Count; // should always be 1 lmao
		public string KeyString;


		public KeyHintText(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Count = bsr.ReadByte();
			if (Count != 1)
				DemoRef.LogError($"{GetType()} is borking, there should only be one string but count is {Count}");
			KeyString = bsr.ReadNullTerminatedString();
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"count: {Count}");
			pw.Append($"str: {KeyString}");
		}
	}
}
