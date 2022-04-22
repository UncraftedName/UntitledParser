#nullable enable
using System;
using System.Numerics;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.Components.Messages.SvcSounds;

namespace DemoParser.Parser.Components.Messages {

	public class SvcSounds : DemoMessage {

		internal const int SndSeqNumberBits = 10;
		internal const int SndSeqNumMask = (1 << SndSeqNumberBits) - 1;
		internal const int MaxSndLvlBits = 9;
		internal const int MaxSndDelayMSecEncodeBits = 13;
		internal const float SndDelayOffset = 0.1f;

		public bool Reliable;
		public SoundInfo[]? Sounds;


		public SvcSounds(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Reliable = bsr.ReadBool();
			int soundCount = Reliable ? 1 : bsr.ReadByte();
			int dataBitLen = (int)bsr.ReadUInt(Reliable ? 8 : 16);

			BitStreamReader soundBsr = bsr.ForkAndSkip(dataBitLen);

			SoundInfo sound = new SoundInfo(DemoRef);
			SoundInfo delta = new SoundInfo(DemoRef);
			delta.SetDefault();

			Sounds = new SoundInfo[soundCount];
			for (int i = 0; i < soundCount; i++) {
				sound.ParseDelta(ref soundBsr, delta);
				delta = sound;
				if (Reliable) { // client is incrementing the reliable sequence numbers itself
					GameState.ClientSoundSequence = ++GameState.ClientSoundSequence & SndSeqNumMask;
					if (sound.SequenceNumber != 0) {
						Sounds = null;
						DemoRef.LogError($"{GetType().Name}: expected sequence number 0, got {sound.SequenceNumber}");
						return;
					}
					sound.SequenceNumber = GameState.ClientSoundSequence;
				}
				Sounds[i] = new SoundInfo(sound);

				if (soundBsr.HasOverflowed) {
					Sounds = null;
					DemoRef.LogError($"{GetType().Name}: reader overflowed at sound index {i}");
					return;
				}
			}
			if (soundBsr.BitsRemaining > 0) {
				Sounds = null;
				DemoRef.LogError($"{GetType().Name}: did not read all bits");
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"reliable: {Reliable}");
			if (Sounds != null) {
				for (int i = 0; i < Sounds.Length; i++) {
					pw.AppendLine();
					pw.Append($"sound #{i + 1}:");
					pw.FutureIndent++;
					pw.AppendLine();
					Sounds[i].PrettyWrite(pw);
					pw.FutureIndent--;
				}
			} else {
				pw.AppendLine();
				pw.Append("sound parsing failed");
			}
		}
	}


	public class SoundInfo : DemoComponent {

		public uint EntityIndex;
		public int? SoundNum;
		public uint? ScriptHash;
		public string? SoundName;
		private bool _soundTableReadable;
		public SoundFlags Flags;
		public Channel Chan;
		public bool IsAmbient;
		public bool IsSentence;
		public uint SequenceNumber;
		public float Volume;
		public uint SoundLevel;
		public uint Pitch;
		public int? SpecialDspCount; // old protocol
		public int? RandomSeed;      // new protocol
		public float Delay;
		public Vector3 Origin;
		public int SpeakerEntity;

		private SoundInfo? _deltaTmp;


		public SoundInfo(SourceDemo? demoRef) : base(demoRef) {}


		public SoundInfo(SoundInfo si) : base(si.DemoRef) {
			EntityIndex = si.EntityIndex;
			SoundNum = si.SoundNum;
			SoundName = si.SoundName;
			_soundTableReadable = si._soundTableReadable;
			Flags = si.Flags;
			Chan = si.Chan;
			IsAmbient = si.IsAmbient;
			IsSentence = si.IsSentence;
			SequenceNumber = si.SequenceNumber;
			Volume = si.Volume;
			SoundLevel = si.SoundLevel;
			Pitch = si.Pitch;
			RandomSeed = si.RandomSeed;
			Delay = si.Delay;
			Origin = si.Origin;
			SpeakerEntity = si.SpeakerEntity;
		}


		internal void SetDefault() {
			Delay = 0.0f;
			Volume = 1.0f;
			SoundLevel = 75;
			Pitch = 100;
			SpecialDspCount = DemoInfo.NewDemoProtocol ? (int?)null : 0;
			RandomSeed = DemoInfo.NewDemoProtocol ? 0 : (int?)null;
			EntityIndex = 0;
			SpeakerEntity = -1;
			Chan = Channel.Static;
			SoundNum = 0;
			Flags = SoundFlags.None;
			SequenceNumber = 0;
			IsSentence = false;
			IsAmbient = false;
			Origin = Vector3.Zero;
		}


		private void ClearStopFields() {
			Volume = 0;
			SoundLevel = 0;
			Pitch = 100;
			SoundName = null;
			Delay = 0.0f;
			SequenceNumber = 0;
			Origin = Vector3.Zero;
			SpeakerEntity = -1;
		}


		public new void ParseStream(ref BitStreamReader bsr) {
			throw new InvalidOperationException();
		}


		// ReadDelta(SoundInfo_t *this,SoundInfo_t *delta,bf_read *buf)
		public void ParseDelta(ref BitStreamReader bsr, SoundInfo delta) {
			_deltaTmp = delta;
			base.ParseStream(ref bsr);
			_deltaTmp = null;
		}


		protected override void Parse(ref BitStreamReader bsr) {
			EntityIndex = bsr.ReadBool() ? bsr.ReadUInt(bsr.ReadBool() ? 5 : DemoInfo.MaxEdictBits) : _deltaTmp.EntityIndex;

#pragma warning disable 8629
			if (DemoInfo.NewDemoProtocol) {
				Flags = (SoundFlags?)bsr.ReadUIntIfExists(DemoInfo.SoundFlagBitsEncode) ?? _deltaTmp.Flags;
				if ((Flags & SoundFlags.IsScriptHandle) != 0)
					ScriptHash = bsr.ReadUInt();
				else
					SoundNum = (int?)bsr.ReadUIntIfExists(DemoInfo.MaxSndIndexBits) ?? _deltaTmp.SoundNum;
			} else {
				SoundNum = (int?)bsr.ReadUIntIfExists(DemoInfo.MaxSndIndexBits) ?? _deltaTmp.SoundNum;
				Flags = (SoundFlags?)bsr.ReadUIntIfExists(DemoInfo.SoundFlagBitsEncode) ?? _deltaTmp.Flags;
			}
			Chan = (Channel?)bsr.ReadUIntIfExists(3) ?? _deltaTmp.Chan;
#pragma warning restore 8629

			#region get sound name

			if (SoundNum.HasValue) {
				var mgr = GameState.StringTablesManager;

				if (mgr.IsTableStateValid(TableNames.SoundPreCache)) {
					_soundTableReadable = true;
					if (SoundNum >= mgr.Tables[TableNames.SoundPreCache].Entries.Count)
						DemoRef.LogError($"{GetType().Name}: sound index {SoundNum} out of range");
					else if (SoundNum != 0)
						SoundName = mgr.Tables[TableNames.SoundPreCache].Entries[SoundNum.Value].Name;
				}
			}

			#endregion

			IsAmbient = bsr.ReadBool();
			IsSentence = bsr.ReadBool();

			if (Flags != SoundFlags.Stop) {

				if (bsr.ReadBool())
					SequenceNumber = _deltaTmp.SequenceNumber;
				else if (bsr.ReadBool())
					SequenceNumber = _deltaTmp.SequenceNumber + 1;
				else
					SequenceNumber = bsr.ReadUInt(SndSeqNumberBits);

				Volume = bsr.ReadUIntIfExists(7) / 127.0f ?? _deltaTmp.Volume;
				SoundLevel = bsr.ReadUIntIfExists(MaxSndLvlBits) ?? _deltaTmp.SoundLevel;
				Pitch = bsr.ReadUIntIfExists(8) ?? _deltaTmp.Pitch;
				if (!DemoInfo.NewDemoProtocol && DemoRef.Header.NetworkProtocol > 21)
					SpecialDspCount = bsr.ReadByteIfExists() ?? _deltaTmp.SpecialDspCount;

				if (DemoInfo.NewDemoProtocol) {
					RandomSeed = bsr.ReadSIntIfExists(6) ?? _deltaTmp.RandomSeed; // 6, 18, or 29
					Delay = bsr.ReadFloatIfExists() ?? _deltaTmp.Delay;
				} else {
					if (bsr.ReadBool()) {
						Delay = bsr.ReadSInt(MaxSndDelayMSecEncodeBits) / 1000.0f;
						if (Delay < 0)
							Delay *= 10.0f;
						Delay -= SndDelayOffset;
					}
					else {
						Delay = _deltaTmp.Delay;
					}
				}

				Origin = new Vector3 {
					X = bsr.ReadSIntIfExists(PropDecodeConsts.CoordIntBits - 2) * 8 ?? _deltaTmp.Origin.X,
					Y = bsr.ReadSIntIfExists(PropDecodeConsts.CoordIntBits - 2) * 8 ?? _deltaTmp.Origin.Y,
					Z = bsr.ReadSIntIfExists(PropDecodeConsts.CoordIntBits - 2) * 8 ?? _deltaTmp.Origin.Z
				};
				SpeakerEntity = bsr.ReadSIntIfExists(DemoInfo.MaxEdictBits + 1) ?? _deltaTmp.SpeakerEntity;
			} else {
				ClearStopFields();
			}
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine($"entity index: {EntityIndex}");

			if ((Flags & SoundFlags.IsScriptHandle) != 0) {
				pw.Append($"scriptable sound hash: {ScriptHash}");
			} else {
				if (_soundTableReadable && SoundName != null)
					pw.Append($"sound: \"{SoundName}\"");
				else
					pw.Append("sound num:");
				pw.AppendLine($" [{SoundNum}]");
			}

			pw.AppendLine($"flags: {Flags}");
			pw.AppendLine($"channel: {Chan}");
			pw.AppendLine($"is ambient: {IsAmbient}");
			pw.AppendLine($"is sentence: {IsSentence}");
			pw.AppendLine($"sequence number: {SequenceNumber}");
			pw.AppendLine($"volume: {Volume}");
			pw.AppendLine($"sound level: {SoundLevel}");
			pw.AppendLine($"pitch: {Pitch}");
			if (SpecialDspCount.HasValue)
				pw.AppendLine($"nSpecialDSP: {SpecialDspCount.Value}");
			if (DemoInfo.NewDemoProtocol)
				pw.AppendLine($"random seed: {RandomSeed}");
			pw.AppendLine($"origin: {Origin}");
			pw.Append($"speaker entity: {SpeakerEntity}");
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
