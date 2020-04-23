using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcSounds : DemoMessage {

		public bool Reliable;
		public List<SoundInfo> Sounds;

		private static bool _searched; // if the lookup table wasn't found, don't look again 


		public SvcSounds(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Reliable = bsr.ReadBool();
			uint soundCount = Reliable ? (uint)1 : bsr.ReadByte();
			uint dataBitLen = bsr.ReadBitsAsUInt(Reliable ? 8 : 16);
			int indexBeforeSounds = bsr.CurrentBitIndex;
			Sounds = new List<SoundInfo>();
			for (int i = 0; i < soundCount; i++) {
				Sounds.Add(new SoundInfo(DemoRef, bsr));
				Sounds[^1].ParseStream(bsr);
			}
			bsr.CurrentBitIndex = indexBeforeSounds;
			bsr.SkipBits(dataBitLen);
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"reliable: {Reliable}");
			for (int i = 0; i < Sounds.Count; i++) {
				iw.AppendLine();
				iw.Append($"sound #{i + 1}:");
				iw.AddIndent();
				iw.AppendLine();
				Sounds[i].AppendToWriter(iw);
				iw.SubIndent();
			}
		}
	}
	
	
	public class SoundInfo : DemoComponent {

		public uint EntityIndex;
		public int SoundIndex;
		public SoundFlags Flags;
		private bool HasStopPacket => (Flags & SoundFlags.Stop) != 0;
		public uint Channel;
		public bool IsAmbient;
		public bool IsSentence;
		// if not STOP
		public uint? SequenceNumber;
		public float? Volume;
		public uint? SoundLevel;
		public byte? Pitch;
		public float? Delay;
		public float? X, Y, Z;
		public uint? SpeakerEntity;
		
		
		public SoundInfo(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			EntityIndex = bsr.ReadBool() ? bsr.ReadBitsAsUInt(bsr.ReadBool() ? 5 : 11) : 0; // MAX_EDICT_BITS
			SoundIndex = (int)(bsr.ReadBitsAsUIntIfExists(13) ?? 0); // MAX_SOUND_INDEX_BITS
			if (DemoRef.CStringTablesManager.TableReadable.GetValueOrDefault(TableNames.SoundPreCache)
				&& SoundIndex >= DemoRef.CStringTablesManager.Tables[TableNames.SoundPreCache].Entries.Count) 
			{
				DemoRef.AddError($"sound index out of range: {SoundIndex}");
			}

			Flags = (SoundFlags)(bsr.ReadBitsAsUIntIfExists(9) ?? 0); // SND_FLAG_BITS_ENCODE
			Channel = bsr.ReadBitsAsUIntIfExists(3) ?? 0;
			IsAmbient = bsr.ReadBool();
			IsSentence = bsr.ReadBool();
			if (!HasStopPacket) {
				if (bsr.ReadBool())
					SequenceNumber = 0;
				else if (bsr.ReadBool())
					SequenceNumber = 1;
				else
					SequenceNumber = bsr.ReadBitsAsUInt(10);
				Volume = bsr.ReadBitsAsUIntIfExists(7) / 127f ?? 0;
				SoundLevel = bsr.ReadBitsAsUIntIfExists(9) ?? 0;
				Pitch = (byte?)(bsr.ReadBitsAsUIntIfExists(8) ?? 0);
				Delay = bsr.ReadBitsAsUIntIfExists(13) ?? 0;
				if (Delay != 0) {
					if (Delay < 0)
						Delay *= 10;
					Delay -= 0.1f;
				}

				X = bsr.ReadBitsAsSIntIfExists(12) * 8 ?? 0;
				Y = bsr.ReadBitsAsSIntIfExists(12) * 8 ?? 0;
				Z = bsr.ReadBitsAsSIntIfExists(12) * 8 ?? 0;
				SpeakerEntity = bsr.ReadBitsAsUIntIfExists(12) ?? 0;
			}
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"entity index: {EntityIndex}");

			var mgr = DemoRef.CStringTablesManager;
			if (mgr.TableReadable.GetValueOrDefault(TableNames.SoundPreCache))
				iw.Append(SoundIndex < mgr.Tables[TableNames.SoundPreCache].Entries.Count
					? $"sound: {mgr.Tables[TableNames.SoundPreCache].Entries[SoundIndex].EntryName}"
					: "sound index (out of range):");
			else
				iw.Append("sound index:");
			
			iw.AppendLine($" ({SoundIndex})");
			
			iw.AppendLine($"flags: {Flags}");
			iw.AppendLine($"channel: {Channel}");
			iw.AppendLine($"is ambient: {IsAmbient}");
			iw.Append($"is sentence: {IsSentence}");
			if (!HasStopPacket) {
				iw.AppendLine($"\nsequence number: {SequenceNumber}");
				iw.AppendLine($"volume: {Volume}");
				iw.AppendLine($"sound level: {SoundLevel}");
				iw.AppendLine($"pitch: {Pitch}");
				iw.AppendLine($"x: {X}, y: {Y}, z: {Z}");
				iw.Append("speaker entity: " + SpeakerEntity);
			}
		}


		[Flags]
		public enum SoundFlags : uint {
			None           = 0,
			ChangeVol      = 1,
			ChangePitch    = 1 << 1,
			Stop           = 1 << 2,
			Spawning       = 1 << 3,
			Delay          = 1 << 4,
			StopLooping    = 1 << 5,
			Speaker        = 1 << 6,
			ShouldPause    = 1 << 7,
			IgnorePhonemes = 1 << 8,
			IgnoreName     = 1 << 9,
		}
	} 
}