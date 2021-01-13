#nullable enable
using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class HudMsg : UserMessage {

		public HudChannel Channel;
		public HudMsgInfo? MsgInfo;
		

		public HudMsg(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Channel = (HudChannel)(bsr.ReadByte() % DemoSettings.MaxNetMessage);
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


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.Append($"channel: {Channel}");
			if (MsgInfo != null) {
				iw.AppendLine();
				MsgInfo.PrettyWrite(iw);
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


			public override void PrettyWrite(IPrettyWriter iw) {
				iw.AppendLine($"x: {X}, y: {Y}");
				iw.AppendLine($"RGBA1: {R1} {G1} {B1} {A1}");
				iw.AppendLine($"RGBA2: {R2} {G2} {B2} {A2}");
				iw.AppendLine($"effect: {Effect}");
				iw.AppendLine($"fade in: {FadeIn}");
				iw.AppendLine($"fade out: {FadeOut}");
				iw.AppendLine($"hold time: {HoldTime}");
				iw.AppendLine($"fx time: {FxTime}");
				iw.Append($"message: {Message}");
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