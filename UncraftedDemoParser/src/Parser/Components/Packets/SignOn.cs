using System.Diagnostics;
using UncraftedDemoParser.Parser.Components.Abstract;

namespace UncraftedDemoParser.Parser.Components.Packets {
	
	public class SignOn : DemoPacket {

		public SignOn(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}


		protected override void ParseBytes() {
			Debug.WriteLine("signon not parsable yet");
		}

		public override void UpdateBytes() {
			Debug.WriteLine("signon packet not parsable yet");
		}
	}
}