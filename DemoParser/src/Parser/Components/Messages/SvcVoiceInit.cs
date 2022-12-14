using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcVoiceInit : DemoMessage {

		public string Codec;
		public byte Quality; // if 255, this a new version of this message (from hl2_src)
		public int? SampleRate;

		public SvcVoiceInit(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Codec = bsr.ReadNullTerminatedString();
			Quality = bsr.ReadByte();
			if (Quality == 255)
				SampleRate = bsr.ReadSInt();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"codec: {Codec}");
			if (SampleRate.HasValue)
				pw.Append($"sample rate: {SampleRate}");
			else
				pw.Append($"quality: {Quality}");
		}
	}
}
