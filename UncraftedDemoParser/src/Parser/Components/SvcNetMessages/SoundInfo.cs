using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Utils;

// literally stealing code
// https://github.com/NeKzor/sdp.js/blob/master/src/types/SoundInfo.js

namespace UncraftedDemoParser.Parser.Components.SvcNetMessages {
	
	// not actually an svc message, but easier to store as one
	public sealed class SoundInfo : SvcNetMessage {

		
		public bool? BitCountForEntIndex; // true => 5, false => 11. null => entity index = 0
		public int EntityIndex;
		public int SoundNumber;
		public int Flags;
		public int Channel;
		public bool IsAmbient;
		public bool IsSentence;
		public int SequenceNumber;
		// the following are set if this isn't a "stop sound" packet
		public float? Volume;
		public int? SoundLevel;
		public int? Pitch;
		public float? Delay;
		public int? OriginX, OriginY, OriginZ;
		public int? SpeakerEntity;


		// auto parses, saves the bit field reader so it automatically points to the next SoundInfo
		public SoundInfo(SourceDemo demoRef, int tick, BitFieldReader bfr) : base(bfr.Data, demoRef, tick) {
			ParseBytes(bfr);
		}
		

		protected override void ParseBytes(BitFieldReader bfr) {
			BitCountForEntIndex = bfr.ReadBoolIfExists();
			EntityIndex = BitCountForEntIndex.HasValue ? bfr.ReadBitsAsInt(BitCountForEntIndex.Value ? 5 : 11) : 0;
			SoundNumber = bfr.ReadBitsAsIntIfExists(13).GetValueOrDefault();
			Flags = bfr.ReadBitsAsIntIfExists(9).GetValueOrDefault();
			Channel = bfr.ReadBitsAsIntIfExists(3).GetValueOrDefault();
			IsAmbient = bfr.ReadBool();
			IsSentence = bfr.ReadBool();
			if (Flags != (int)SoundFlags.Stop) {
				SequenceNumber = bfr.ReadBool() ? 0 : (bfr.ReadBool() ? 1 : bfr.ReadBitsAsInt(10));
				Volume = bfr.ReadBitsAsIntIfExists(7).GetValueOrDefault() / 127.0f;
				SoundLevel = bfr.ReadBitsAsIntIfExists(9).GetValueOrDefault();
				Pitch = bfr.ReadBitsAsIntIfExists(9).GetValueOrDefault();
				if (bfr.ReadBool()) {
					Delay = bfr.ReadBitsAsInt(13) / 1000.0f;
					if (Delay < 0)
						Delay *= 10;
					Delay -= 0.1f;
				} else {
					Delay = 0;
				}
				OriginX = bfr.ReadBitsAsIntIfExists(12).GetValueOrDefault();
				OriginY = bfr.ReadBitsAsIntIfExists(12).GetValueOrDefault();
				OriginZ = bfr.ReadBitsAsIntIfExists(12).GetValueOrDefault();
				SpeakerEntity = bfr.ReadBitsAsIntIfExists(12).GetValueOrDefault();
			}
		}
	}


	public enum SoundFlags {
		NoFlags 		= 0,
		ChangeVol 		= 1,
		ChangePitch 	= 1 << 1,
		Stop 			= 1 << 2,
		Spawning 		= 1 << 3,
		Delay 			= 1 << 4,
		StopLooping 	= 1 << 5,
		Speaker 		= 1 << 6,
		ShouldPause 	= 1 << 7,
		IgnorePause 	= 1 << 8,
		IgnorePhonemes 	= 1 << 9,
		IgnoreName 		= 1 << 10
	}
}