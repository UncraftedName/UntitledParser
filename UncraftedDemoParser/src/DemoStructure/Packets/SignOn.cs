using System.Diagnostics;

namespace UncraftedDemoParser.DemoStructure.Packets {
	
	public class SignOn : DemoComponent {

		public SignOn(byte[] data, SourceDemo demoRef) : base(data, demoRef) {}


		public override void ParseBytes() {
			Debug.WriteLine("signon not parsable yet");
		}

		public override void UpdateBytes() {
			Debug.WriteLine("signon packet not parsable yet");
		}
	}
}