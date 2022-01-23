using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class Fade : UserMessage {

		public float Duration;
		public ushort HoldTime; // yeah idk what this is about
		public FadeFlags Flags;
		public byte R, G, B, A;


		public Fade(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Duration = bsr.ReadUShort() / (float)(1 << 9); // might be useful: #define SCREENFADE_FRACBITS 9
			HoldTime = bsr.ReadUShort();
			Flags = (FadeFlags)bsr.ReadUShort();
			R = bsr.ReadByte();
			G = bsr.ReadByte();
			B = bsr.ReadByte();
			A = bsr.ReadByte();
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"duration: {Duration}");
			pw.AppendLine($"hold time: {HoldTime}");
			pw.AppendLine($"flags: {Flags}");
			pw.Append($"RGBA: {R:D3}, {G:D3}, {B:D3}, {A:D3}");
		}
	}


	[Flags]
	public enum FadeFlags : ushort {
		None     = 0,
		FadeIn   = 1,
		FadeOut  = 1 << 1,
		Modulate = 1 << 2, // Modulate (don't blend)
		StayOut  = 1 << 3, // ignores the duration, stays faded out until new ScreenFade message received
		Purge    = 1 << 4  // Purges all other fades, replacing them with this one
	}
}
