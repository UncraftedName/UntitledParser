using System.Collections.Generic;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class SvcSounds : SvcNetMessage {

		public bool ReliableSound;
		// public byte SoundCount;  <-  not stored for simplicity
		public short Length;
		public byte[] Data;
		public List<SoundInfo> Sounds = new List<SoundInfo>();
		
		
		public SvcSounds(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			ReliableSound = bfr.ReadBool();
			byte soundCount = ReliableSound ? (byte)1 : bfr.ReadByte();
			Length = ReliableSound ? bfr.ReadByte() : bfr.ReadShort();
			Data = bfr.ReadBits(Length);

			if (DemoRef.Header.DemoProtocol == 3) {
				for (int i = 0; i < soundCount; i++) {
					bfr.DiscardPreviousBits();
					Sounds.Add(new SoundInfo(DemoRef, Tick, bfr));
				}
			}
		}
	}
}