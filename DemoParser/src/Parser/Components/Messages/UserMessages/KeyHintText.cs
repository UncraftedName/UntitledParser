using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class KeyHintText : UserMessage {

		public int Count; // should always be 1 lmao
		public string KeyString;


		public KeyHintText(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Count = bsr.ReadByte();
			if (Count != 1)
				DemoRef.LogError($"{GetType().Name}: expected 1 string but got {Count}");
			KeyString = bsr.ReadNullTerminatedString();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"count: {Count}");
			pw.Append($"str: {KeyString}");
		}
	}
}
