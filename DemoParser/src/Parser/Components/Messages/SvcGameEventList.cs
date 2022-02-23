using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.GameState;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcGameEventList : DemoMessage {

		public int EventCount;
		public List<GameEventDescription> Descriptors;


		public SvcGameEventList(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			EventCount = (int)bsr.ReadUInt(9);
			int dataLen = (int)bsr.ReadUInt(20);
			int indexBeforeData = bsr.CurrentBitIndex;

			Descriptors = new List<GameEventDescription>(EventCount);
			for (int i = 0; i < EventCount; i++) {
				Descriptors.Add(new GameEventDescription(DemoRef));
				Descriptors[^1].ParseStream(ref bsr);
			}

			bsr.CurrentBitIndex = dataLen + indexBeforeData;

			// used for parsing SvcGameEvent
			GameState.GameEventManager = new GameEventManager(Descriptors);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"{EventCount} events:");
			pw.FutureIndent++;
			foreach (GameEventDescription descriptor in Descriptors) {
				pw.AppendLine();
				descriptor.PrettyWrite(pw);
			}
			pw.FutureIndent--;
		}
	}


	public class GameEventDescription : DemoComponent {

		public uint EventId;
		public string Name;
		public List<(string Name, EventDescriptorType type)> Keys;


		public GameEventDescription(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			EventId = bsr.ReadUInt(9);
			Name = bsr.ReadNullTerminatedString();
			Keys = new List<(string Name, EventDescriptorType type)>();
			uint type;
			while ((type = bsr.ReadUInt(3)) != 0)
				Keys.Add((bsr.ReadNullTerminatedString(), (EventDescriptorType)type));
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"{EventId}: {Name}");
			pw.FutureIndent++;
			pw.AppendLine();
			pw.Append($"keys: {Keys.Select(tup => $"{tup.type.ToString().ToLower()} {tup.Name}").SequenceToString()}");
			pw.FutureIndent--;
		}
	}


	public enum EventDescriptorType : byte {
		String = 1,
		Float,
		Int32,
		Int16,
		Int8,
		Bool,
		UInt64
	}
}
