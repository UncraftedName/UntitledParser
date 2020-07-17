#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.DemoSettings;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcSounds : DemoMessage {
		
		public bool Reliable;
		public List<SoundInfo>? Sounds;
		
		
		public SvcSounds(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			Reliable = bsr.ReadBool();
			uint soundCount = Reliable ? (uint)1 : bsr.ReadByte();
			int dataBitLen = (int)bsr.ReadBitsAsUInt(Reliable ? 8 : 16);
			
			BitStreamReader soundBsr = bsr.SubStream(dataBitLen);
			bsr.SkipBits(dataBitLen);
			SetLocalStreamEnd(bsr);
			
			Exception? e = null;
			try {
				Sounds = new List<SoundInfo>();
				for (int i = 0; i < soundCount; i++) {
					SoundInfo info = new SoundInfo(DemoRef, soundBsr);
					info.ParseStream(soundBsr);
					if (Reliable) { // client is incrementing the reliable sequence numbers itself
						DemoRef.ClientSoundSequence = ++DemoRef.ClientSoundSequence & SndSeqNumMask;
						if (info.SequenceNumber != 0)
							throw new ArgumentException($"expected sequence number 0, got: {info.SequenceNumber}");
						info.SequenceNumber = DemoRef.ClientSoundSequence;
					}
					Sounds.Add(info);
				}
			} catch (Exception exp) {
				e = exp;
			}
			if (e != null) {
				Sounds = null;
				DemoRef.LogError($"exception while parsing {nameof(SoundInfo)}: {e.Message}");
			} else if (soundBsr.BitsRemaining != 0) {
				Sounds = null;
				DemoRef.LogError($"exception while parsing {nameof(SoundInfo)}: {soundBsr.BitsRemaining} bits left to read");
			}
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}
		
		
		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"reliable: {Reliable}");
			if (Sounds != null) {
				for (int i = 0; i < Sounds.Count; i++) {
					iw.AppendLine();
					iw.Append($"sound #{i + 1}:");
					iw.FutureIndent++;
					iw.AppendLine();
					Sounds[i].AppendToWriter(iw);
					iw.FutureIndent--;
				}
			} else {
				iw.AppendLine();
				iw.Append("sound parsing failed");
			}
		}
	}
	
	
	public class SoundInfo : DemoComponent {
		
		public uint EntityIndex;
		public uint SoundNum; // either index or possibly hash in demo protocol 4
		public string? Name;
		private bool _soundTableReadable;
		public SoundFlags Flags;
		public Channel Chan;
		public bool IsAmbient;
		public bool IsSentence;
		public uint SequenceNumber;
		public float Volume;
		public uint SoundLevel;
		public uint Pitch;
		public int? RandomSeed; // demo protocol 4 only
		public float Delay;
		public Vector3 Origin;
		public int SpeakerEntity;
		
		
		public SoundInfo(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		private void SetDefault() {
			Delay = 0.0f;
			Volume = 1.0f;
			SoundLevel = 75;
			Pitch = 100;
			RandomSeed = DemoSettings.NewDemoProtocol ? 0 : (int?)null;
			EntityIndex = 0;
			SpeakerEntity = -1;
			Chan = Channel.Static;
			SoundNum = 0;
			Flags = SoundFlags.None;
			SequenceNumber = 0;
			IsSentence = false;
			IsAmbient = false;
			Origin = default;
		}
		
		
		private void ClearStopFields() {
			Volume = 0;
			SoundLevel = 0;
			Pitch = 0;
			RandomSeed = DemoSettings.NewDemoProtocol ? 0 : (int?)null;
			Delay = 0.0f;
			SequenceNumber = 0;
			Origin = default;
			SpeakerEntity = -1;
		}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			SetDefault();
			EntityIndex = bsr.ReadBool() ? bsr.ReadBitsAsUInt(bsr.ReadBool() ? 5 : MaxEdictBits) : EntityIndex;
			
#pragma warning disable 8629
			if (DemoSettings.NewDemoProtocol) {
				Flags = (SoundFlags?)bsr.ReadBitsAsUIntIfExists(DemoSettings.SoundFlagBits) ?? Flags;
				SoundNum = (Flags & SoundFlags.IsScriptHandle) != 0
					? bsr.ReadUInt() // scriptable sounds are written as a hash, uses full 32 bits
					: bsr.ReadBitsAsUIntIfExists(MaxSndIndexBits) ?? SoundNum;
			} else {
				SoundNum = bsr.ReadBitsAsUIntIfExists(MaxSndIndexBits) ?? SoundNum;
				Flags = (SoundFlags?)bsr.ReadBitsAsUIntIfExists(DemoSettings.SoundFlagBits) ?? Flags;
			}
			Chan = (Channel?)bsr.ReadBitsAsUIntIfExists(3) ?? Chan;
#pragma warning restore 8629
			
			#region get sound name

			if ((Flags & SoundFlags.IsScriptHandle) == 0) {
				var mgr = DemoRef.CurStringTablesManager;

				if (mgr.TableReadable.GetValueOrDefault(TableNames.SoundPreCache)) {
					_soundTableReadable = true;
					if (SoundNum >= mgr.Tables[TableNames.SoundPreCache].Entries.Count)
						DemoRef.LogError($"sound index out of range: {SoundNum}");
					else if (SoundNum != 0)
						Name = mgr.Tables[TableNames.SoundPreCache].Entries[(int)SoundNum].EntryName;
				}
			}

			#endregion
			
			IsAmbient = bsr.ReadBool();
			IsSentence = bsr.ReadBool();
			
			if (Flags != SoundFlags.Stop) {
				if (!bsr.ReadBool()) {
					if (bsr.ReadBool())
						SequenceNumber++;
					else
						SequenceNumber = bsr.ReadBitsAsUInt(SndSeqNumberBits);
				}
				Volume = bsr.ReadBitsAsUIntIfExists(7) / 127.0f ?? Volume;
				SoundLevel = bsr.ReadBitsAsUIntIfExists(MaxSndLvlBits) ?? SoundLevel;
				Pitch = bsr.ReadBitsAsUIntIfExists(8) ?? Pitch;
				
				if (DemoSettings.NewDemoProtocol)
					 RandomSeed = bsr.ReadBitsAsSIntIfExists(6) ?? RandomSeed; // 6, 18, or 29
				
				if (bsr.ReadBool()) {
					Delay = bsr.ReadBitsAsSInt(MaxSndDelayMSecEncodeBits) / 1000.0f;
					if (Delay < 0)
						Delay *= 10.0f;
					Delay -= SndDelayOffset;
				}
				
				Origin = new Vector3 {
					X = bsr.ReadBitsAsSIntIfExists(PropDecodeConsts.CoordIntBits - 2) * 8 ?? Origin.X,
					Y = bsr.ReadBitsAsSIntIfExists(PropDecodeConsts.CoordIntBits - 2) * 8 ?? Origin.Y,
					Z = bsr.ReadBitsAsSIntIfExists(PropDecodeConsts.CoordIntBits - 2) * 8 ?? Origin.Z
				};
				SpeakerEntity = bsr.ReadBitsAsSIntIfExists(MaxEdictBits + 1) ?? SpeakerEntity;
			} else {
				ClearStopFields();
			}
			SetLocalStreamEnd(bsr);
		}
		
		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}
		
		
		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine($"entity index: {EntityIndex}");

			if ((Flags & SoundFlags.IsScriptHandle) != 0) {
				iw.Append("scriptable sound hash:");
			} else {
				if (_soundTableReadable && Name != null)
					iw.Append($"sound: \"{Name}\"");
				else
					iw.Append("sound num:");
			}
			iw.AppendLine($" ({SoundNum})");

			iw.AppendLine($"flags: {Flags}");
			iw.AppendLine($"channel: {Chan}");
			iw.AppendLine($"is ambient: {IsAmbient}");
			iw.AppendLine($"is sentence: {IsSentence}");
			iw.AppendLine($"sequence number: {SequenceNumber}");
			iw.AppendLine($"volume: {Volume}");
			iw.AppendLine($"sound level: {SoundLevel}");
			iw.AppendLine($"pitch: {Pitch}");
			if (DemoSettings.NewDemoProtocol)
				iw.AppendLine($"random seed: {RandomSeed}");
			iw.AppendLine($"origin: {Origin}");
			iw.Append($"speaker entity: {SpeakerEntity}");
		}
		
		
		[Flags]
		public enum SoundFlags : uint {
			None                 = 0,
			ChangeVol            = 1,
			ChangePitch          = 1 << 1,
			Stop                 = 1 << 2,
			Spawning             = 1 << 3, // we're spawning, used in some cases for ambients, not sent over net
			Delay                = 1 << 4,
			StopLooping          = 1 << 5,
			Speaker              = 1 << 6, // being played again by a microphone through a speaker
			ShouldPause          = 1 << 7, // this sound should be paused if the game is paused
			IgnorePhonemes       = 1 << 8,
			IgnoreName           = 1 << 9,
			// not present in portal 1
			IsScriptHandle       = 1 << 10,
			UpdateDelayForChoreo = 1 << 11, // True if we have to update snd_delay_for_choreo with the IO latency
			GenerateGuid         = 1 << 12, // True if we generate the GUID when we send the sound
			OverridePitch        = 1 << 13
		}
		
		
		public enum Channel {
			Replace = -1,
			Auto,
			Weapon,
			Voice,
			Item,
			Body,
			Stream,    // allocate stream channel from the static or dynamic area
			Static,    // allocate channel from the static area 
			VoiceBase, // allocate channel for network voice data
			
			UserBase = VoiceBase + 128 // Anything >= this number is allocated to game code.
		}
	} 
}
