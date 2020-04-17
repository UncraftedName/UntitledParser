#nullable enable
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class HudMsg : SvcUserMessage {

		public byte Channel;
		public float? X, Y; // 0-1 & resolution independent, -1 means center in each dimension
		public byte? R1, G1, B1, A1;
		public byte? R2, G2, B2, A2;
		public HudMsgEffect? Effect;
		public float? FadeIn, FadeOut, HoldTime, FxTime; // the fade times seem to be per character
		public string? Message;
		

		public HudMsg(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Channel = (byte)(bsr.ReadByte() % 6); // MAX_NETMESSAGE
			// if ( !pNetMessage || !pNetMessage->pMessage )
			// return;
			// ^ Since this is defined in the engine, I will simply keep reading if there are more bytes left ^
			if (bsr.BitsRemaining >= 148) {
				X = bsr.ReadFloat();
				Y = bsr.ReadFloat();
				R1 = bsr.ReadByte();
				G1 = bsr.ReadByte();
				B1 = bsr.ReadByte();
				A1 = bsr.ReadByte();
				R2 = bsr.ReadByte();
				G2 = bsr.ReadByte();
				B2 = bsr.ReadByte();
				A2 = bsr.ReadByte();
				Effect = (HudMsgEffect?)bsr.ReadByte();
				FadeIn = bsr.ReadFloat();
				FadeOut = bsr.ReadFloat();
				HoldTime = bsr.ReadFloat();
				FxTime = bsr.ReadFloat();
				Message = bsr.ReadNullTerminatedString(); // this might be a char array of length 512
			}
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append("channel: " + Channel);
			if (X.HasValue) { // assume all the others are defined
				iw.AppendLine($"\nx: {X}, y: {Y}");
				iw.AppendLine($"RGBA1: {R1} {G1} {B1} {A1}");
				iw.AppendLine($"RGBA2: {R2} {G2} {B2} {A2}");
				iw.AppendLine($"effect: {Effect}");
				iw.AppendLine("fade in: " + FadeIn);
				iw.AppendLine("fade out: " + FadeOut);
				iw.AppendLine("hold time: " + HoldTime);
				iw.AppendLine("fx time: " + FxTime);
				iw.Append("message: " + Message);
			}
		}
	}


	public enum HudMsgEffect: byte {
		Fade 		= 0,
		Flicker 	= 1,
		WriteOut 	= 2
	}
}