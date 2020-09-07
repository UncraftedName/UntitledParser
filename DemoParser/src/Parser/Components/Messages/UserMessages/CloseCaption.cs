using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class CloseCaption : UserMessage {

		public string TokenName;
		public float Duration;
		public CloseCaptionFlags Flags;
		
		
		public CloseCaption(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			TokenName = bsr.ReadNullTerminatedString();
			Duration = bsr.ReadSShort() * 0.1f;
			Flags = (CloseCaptionFlags)bsr.ReadByte();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IIndentedWriter iw) {
			iw.AppendLine($"token name: {TokenName}");
			iw.AppendLine($"duration: {Duration}");
			iw.Append($"flags: {Flags}");
		}
	}


	[Flags]
	public enum CloseCaptionFlags : byte {
		None          = 0,
		WarnIfMissing = 1,
		FromPlayer    = 1 << 1,
		GenderMale    = 1 << 2,
		GenderFemale  = 1 << 3
	}
}