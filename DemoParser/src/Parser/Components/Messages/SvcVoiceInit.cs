using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcVoiceInit : DemoMessage {

		public string Codec;
		public byte Quality;
		public float? Unknown;
		
		public SvcVoiceInit(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Codec = bsr.ReadNullTerminatedString();
			Quality = bsr.ReadByte();
			if (Quality == 255)
				Unknown = bsr.ReadFloat();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.AppendLine($"codec: {Codec}");
			iw.Append($"quality: {Quality}");
			if (Quality == 255)
				iw.Append($"\nunknown: {Unknown}");
		}
	}
}