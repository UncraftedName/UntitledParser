#nullable enable
using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class HudMsg : UserMessage {

		private const int MaxNetMessage = 6;

		public HudChannel Channel;
		public HudMsgInfo? MsgInfo;


		public HudMsg(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Channel = (HudChannel)(bsr.ReadByte() % MaxNetMessage);
			// if ( !pNetMessage || !pNetMessage->pMessage )
			// return;
			// ^ Since this is what the game does, I will simply keep reading if there are more bytes left
			if (bsr.BitsRemaining >= 148) {
				MsgInfo = new HudMsgInfo(DemoRef);
				MsgInfo.ParseStream(ref bsr);
			}
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			bsw.WriteByte((byte)Channel);
			MsgInfo?.WriteToStreamWriter(bsw);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"channel: {Channel}");
			if (MsgInfo != null) {
				pw.AppendLine();
				MsgInfo.PrettyWrite(pw);
			}
		}


		public class HudMsgInfo : DemoComponent {

			public float X, Y; // 0-1 & resolution independent, -1 means center in each dimension
			public byte R1, G1, B1, A1;
			public byte R2, G2, B2, A2;
			public HudMsgEffect Effect;
			public float FadeIn, FadeOut, HoldTime, FxTime; // the fade times seem to be per character
			public string Message;


			public HudMsgInfo(SourceDemo? demoRef) : base(demoRef) {}


			protected override void Parse(ref BitStreamReader bsr) {
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
				Effect = (HudMsgEffect)bsr.ReadByte();
				FadeIn = bsr.ReadFloat();
				FadeOut = bsr.ReadFloat();
				HoldTime = bsr.ReadFloat();
				FxTime = bsr.ReadFloat();
				Message = bsr.ReadNullTerminatedString();
			}


			internal override void WriteToStreamWriter(BitStreamWriter bsw) {
				throw new NotImplementedException();
			}


			public override void PrettyWrite(IPrettyWriter pw) {
				pw.AppendLine($"x: {X}, y: {Y}");
				pw.AppendLine($"RGBA1: {R1} {G1} {B1} {A1}");
				pw.AppendLine($"RGBA2: {R2} {G2} {B2} {A2}");
				pw.AppendLine($"effect: {Effect}");
				pw.AppendLine($"fade in: {FadeIn}");
				pw.AppendLine($"fade out: {FadeOut}");
				pw.AppendLine($"hold time: {HoldTime}");
				pw.AppendLine($"fx time: {FxTime}");
				pw.Append($"message: {Message}");
			}
		}
	}


	public enum HudChannel : byte {
		Netmessage1 = 0,
		NetMessage2,
		NetMessage3,
		NetMessage4,
		NetMessage5,
		NetMessage6,
	}


	public enum HudMsgEffect: byte {
		Fade 		= 0,
		Flicker 	= 1,
		WriteOut 	= 2
	}
}
