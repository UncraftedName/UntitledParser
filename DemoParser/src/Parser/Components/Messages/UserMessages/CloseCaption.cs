using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class CloseCaption : UserMessage {

		public string TokenName;
		public float Duration;
		public CloseCaptionFlags Flags;


		public CloseCaption(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			TokenName = bsr.ReadNullTerminatedString();
			Duration = bsr.ReadSShort() * 0.1f;
			Flags = (CloseCaptionFlags)bsr.ReadByte();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"token name: {TokenName}");
			pw.AppendLine($"duration: {Duration}");
			pw.Append($"flags: {Flags}");
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
