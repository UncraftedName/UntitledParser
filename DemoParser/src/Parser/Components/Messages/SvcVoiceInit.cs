using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcVoiceInit : DemoMessage {

		public string Codec;
		public byte Quality;
		public float? Unknown;

		public SvcVoiceInit(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Codec = bsr.ReadNullTerminatedString();
			Quality = bsr.ReadByte();
			if (Quality == 255)
				Unknown = bsr.ReadFloat();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"codec: {Codec}");
			pw.Append($"quality: {Quality}");
			if (Quality == 255)
				pw.Append($"\nunknown: {Unknown}");
		}
	}
}
