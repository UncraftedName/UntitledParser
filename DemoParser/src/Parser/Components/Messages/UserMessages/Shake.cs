using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages.UserMessages {

	public class Shake : UserMessage {

		public ShakeCommand Command;
		public float Amplitude;
		public float Frequency;
		public float Duration;


		public Shake(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Command = (ShakeCommand)bsr.ReadByte();
			Amplitude = bsr.ReadFloat();
			Frequency = bsr.ReadFloat();
			Duration = bsr.ReadFloat();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"command: {Command}");
			pw.AppendLine($"amplitude: {Amplitude}");
			pw.AppendLine($"frequency: {Frequency}");
			pw.Append($"duration: {Duration}");
		}
	}


	// src_main/public/shake.h
	public enum ShakeCommand : byte {
		Start = 0,  // Starts the screen shake for all players within the radius.
		Stop,       // Stops the screen shake for all players within the radius.
		Amplitude,  // Modifies the amplitude of an active screen shake for all players within the radius.
		Frequency,  // Modifies the frequency of an active screen shake for all players within the radius.
		RumbleOnly, // Starts a shake effect that only rumbles the controller, no screen effect.
		NoRumble    // Starts a shake that does NOT rumble the controller.
	}
}
