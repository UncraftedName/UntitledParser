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
			if (Quality == 255) {
				SampleRate = bsr.ReadSShort();
			} else {
				SampleRate = Codec == "vaudio_celt" ? 22050 : 11025;
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"codec: {Codec}");
			if (Quality != 255)
				pw.AppendLine($"quality: {Quality}");
			pw.Append(SampleRate == 0 && Codec == "steam" ? "sample rate: optimal" : $"sample rate: {SampleRate}");
		}
	}
}
