using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class KillCam : SvcUserMessage {

		public byte NewMode; // todo
		public byte Target1;
		public byte Target2;
		public byte Unknown;
		
		
		public KillCam(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			NewMode = bsr.ReadByte();
			Target1 = bsr.ReadByte();
			Target2 = bsr.ReadByte();
			Unknown = bsr.ReadByte();
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"new mode: {NewMode}");
			iw.AppendLine($"target 1: {Target1}");
			iw.AppendLine($"target 2: {Target2}");
			iw.Append($"unknown: {Unknown}");
		}
	}


	// these are compared to new mode, but I don't think that's what it corresponds to
	public enum SpectatorMode : byte {
		None,      // not in spectator mode
		DeathCam,  // special mode for death cam animation
		FreezeCam, // zooms to a target, and freeze-frames on them
		Fixed,     // view from a fixed camera position
		InEye,     // follow a player in first person view
		Chase,     // follow a player in third person view
		Roaming    // free roaming
	}
}