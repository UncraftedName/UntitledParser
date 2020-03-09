using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	// might be useful: #define SCREENFADE_FRACBITS		9
	public class Fade : SvcUserMessage {

		public float Duration;
		public ushort HoldTime; // yeah idk what this is about
		public FadeFlags Flags;
		public byte R, G, B, A;
		
		
		public Fade(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Duration = bsr.ReadUShort() / (float)(1 << 9);
			HoldTime = bsr.ReadUShort();
			Flags = (FadeFlags)bsr.ReadUShort();
			R = bsr.ReadByte();
			G = bsr.ReadByte();
			B = bsr.ReadByte();
			A = bsr.ReadByte();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"duration: {Duration}");
			iw.AppendLine($"hold time: {HoldTime}");
			iw.AppendLine($"flags: {Flags}");
			iw.Append($"RGBA: {R:D3}, {G:D3}, {B:D3}, {A:D3}");
		}
	}


	[Flags]
	public enum FadeFlags : ushort {
		None 		= 0,
		FadeIn 		= 1,
		FadeOut		= 1 << 1, 
		Modulate	= 1 << 2, // Modulate (don't blend)
		StayOut		= 1 << 3, // ignores the duration, stays faded out until new ScreenFade message received
		Purge		= 1 << 4  // Purges all other fades, replacing them with this one
	}
}