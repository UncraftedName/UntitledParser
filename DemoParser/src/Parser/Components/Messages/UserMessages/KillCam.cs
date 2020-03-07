using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Messages.UserMessages {
	
	public class KillCam : SvcUserMessage {

		public byte NewMode;
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


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"new mode: {NewMode}");
			iw.AppendLine($"target 1: {Target1}");
			iw.AppendLine($"target 2: {Target2}");
			iw.Append($"unknown: {Unknown}");
		}
	}


	// these are compared to new mode, but I don't think that's what it corresponds to
	public enum SpectatorMode: byte {
		OBS_MODE_NONE = 0,  // not in spectator mode
		OBS_MODE_DEATHCAM,  // special mode for death cam animation
		OBS_MODE_FREEZECAM, // zooms to a target, and freeze-frames on them
		OBS_MODE_FIXED,     // view from a fixed camera position
		OBS_MODE_IN_EYE,    // follow a player in first person view
		OBS_MODE_CHASE,     // follow a player in third person view
		OBS_MODE_ROAMING,   // free roaming

		NUM_OBSERVER_MODES,
	}
}