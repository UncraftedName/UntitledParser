using System;
using System.Diagnostics;

namespace UncraftedDemoParser.DemoStructure.Packets {
	
	// unknown data
	public class SyncTick : DemoComponent {

		public SyncTick(byte[] data, SourceDemo demoRef) : base(data, demoRef) {}


		public override void ParseBytes() {
			Debug.WriteLine("sync tick not parsable yet");
		}

		public override void UpdateBytes() {
			Debug.WriteLine("sync tick not parsable yet");
		}


		public override string ToString() {
			return BitConverter.ToString(Bytes).Replace("-", " ").ToLower();
		}
	}
}