using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class HudText : UserMessage {

		public string? Str;
		// sst plugin hides the messages in here - it prevents the game from crashing or screaming
		public byte[]? SstEncryptedMessage;
		
		
		public HudText(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			if (bsr.ReadByte() == 0 && bsr.BitsRemaining > 0) {
				SstEncryptedMessage = bsr.ReadRemainingBits().bytes;
			} else {
				bsr.CurrentBitIndex -= 8;
				Str = bsr.ReadNullTerminatedString();
			}
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append(Str ?? $"sst: {BitConverter.ToString(SstEncryptedMessage!).Replace('-', ' ')}");
		}
	}
}