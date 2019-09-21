using System.Collections.Generic;
using System.Text;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	public class SvcSounds : SvcNetMessage {

		public bool ReliableSound;
		// public byte SoundCount;  <-  not stored for simplicity
		private byte _soundCount; // tmp
		public short Length;
		public byte[] Data;
		public List<SoundInfo> Sounds = new List<SoundInfo>();
		
		
		public SvcSounds(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}
		
		
		protected override void ParseBytes(BitFieldReader bfr) {
			ReliableSound = bfr.ReadBool();
			byte soundCount = ReliableSound ? (byte)1 : bfr.ReadByte();
			_soundCount = soundCount; // tmp
			Length = ReliableSound ? bfr.ReadByte() : bfr.ReadShort();
			Data = bfr.ReadBits(Length);

			if (DemoRef.Header.DemoProtocol == 3) { // p2 seems to contains extra flags (sometimes). but maybe the flags can be mixed as well
				for (int i = 0; i < soundCount; i++) {
					bfr.DiscardPreviousBits();
					Sounds.Add(new SoundInfo(DemoRef, Tick, bfr));
				}
			}
		}


		protected override void PopulatedBuilder(StringBuilder builder) {
			builder.AppendLine($"\t\treliable sound: {ReliableSound}");
			builder.AppendLine($"\t\texpected sounds: {_soundCount}");
			// skipping over data here
			for (int i = 0; i < Sounds.Count; i++) {
				builder.AppendLine($"\t\tsound #{i}");
				builder.Append(Sounds[i]);
				if (i < Sounds.Count - 1)
					builder.AppendLine();
			}
		}
	}
}