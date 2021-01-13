using System;
using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcGameEventList : DemoMessage {

		public int EventCount;
		public List<GameEventDescription> Descriptors;
		

		public SvcGameEventList(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			EventCount = (int)bsr.ReadBitsAsUInt(9);
			int dataLen = (int)bsr.ReadBitsAsUInt(20);
			int indexBeforeData = bsr.CurrentBitIndex;
			
			Descriptors = new List<GameEventDescription>(EventCount);
			for (int i = 0; i < EventCount; i++) {
				Descriptors.Add(new GameEventDescription(DemoRef));
				Descriptors[^1].ParseStream(ref bsr);
			}
			
			bsr.CurrentBitIndex = dataLen + indexBeforeData;
			
			// used for parsing SvcGameEvent
			DemoRef.GameEventManager = new GameEventManager(Descriptors);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.Append($"{EventCount} events:");
			iw.FutureIndent++;
			foreach (GameEventDescription descriptor in Descriptors) {
				iw.AppendLine();
				descriptor.PrettyWrite(iw);
			}
			iw.FutureIndent--;
		}
	}


	public class GameEventDescription : DemoComponent {

		public uint EventId;
		public string Name;
		public List<(string Name, EventDescriptorType type)> Keys;
		
		
		public GameEventDescription(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			EventId = bsr.ReadBitsAsUInt(9);
			Name = bsr.ReadNullTerminatedString();
			Keys = new List<(string Name, EventDescriptorType type)>();
			uint type;
			while ((type = bsr.ReadBitsAsUInt(3)) != 0)
				Keys.Add((bsr.ReadNullTerminatedString(), (EventDescriptorType)type));
		}

		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.Append($"{EventId}: {Name}");
			iw.FutureIndent++;
			iw.AppendLine();
			iw.Append($"keys: {Keys.Select(tup => $"{tup.type.ToString().ToLower()} {tup.Name}").SequenceToString()}");
			iw.FutureIndent--;
		}
	}


	public enum EventDescriptorType : byte {
		String = 1,
		Float,
		Int32,
		Int16,
		Int8,
		Bool
	}
}