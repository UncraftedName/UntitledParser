using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages {
	
	public class SvcVoiceInit : DemoMessage {

		public string Codec;
		public byte Quality;
		public float? Unknown;
		
		public SvcVoiceInit(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Codec = bsr.ReadNullTerminatedString();
			Quality = bsr.ReadByte();
			if (Quality == 255)
				Unknown = bsr.ReadFloat();
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"codec: {Codec}");
			iw.Append($"quality: {Quality}");
			if (Quality == 255)
				iw.Append($"\nunknown: {Unknown}");
		}
	}
}