using System.Collections.Generic;
using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Parser.HelperClasses;
using UntitledParser.Utils;

namespace UntitledParser.Parser.Components.Messages {
	
	public class SvcGameEventList : DemoMessage {

		public int EventCount;
		public List<GameEventDescription> Descriptors;
		

		public SvcGameEventList(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			EventCount = (int)bsr.ReadBitsAsUInt(9);
			int dataLen = (int)bsr.ReadBitsAsUInt(20);
			int indexBeforeData = bsr.CurrentBitIndex;
			
			Descriptors = new List<GameEventDescription>(EventCount);
			for (int i = 0; i < EventCount; i++) {
				Descriptors.Add(new GameEventDescription(DemoRef, bsr));
				Descriptors[^1].ParseStream(bsr);
			}
			
			bsr.CurrentBitIndex = dataLen + indexBeforeData;
			SetLocalStreamEnd(bsr);
			
			// used for parsing SvcGameEvent
			DemoRef.GameEventManager = new GameEventManager(Descriptors);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"{EventCount} events:");
			iw.AddIndent();
			foreach (GameEventDescription descriptor in Descriptors) {
				iw.AppendLine();
				descriptor.AppendToWriter(iw);
			}
			iw.SubIndent();
		}
	}


	public class GameEventDescription : DemoComponent {

		public uint EventID;
		public string Name;
		public List<(string Name, EventDescriptorType type)> Keys;
		
		
		public GameEventDescription(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			EventID = bsr.ReadBitsAsUInt(9);
			Name = bsr.ReadNullTerminatedString();
			Keys = new List<(string Name, EventDescriptorType type)>();
			uint type;
			while ((type = bsr.ReadBitsAsUInt(3)) != 0)
				Keys.Add((bsr.ReadNullTerminatedString(), (EventDescriptorType)type));
		}

		
		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"ID: {EventID}, name: {Name}");
			iw.AddIndent();
			iw.AppendLine();
			iw.Append($"keys: {Keys.SequenceToString()}");
			iw.SubIndent();
		}
	}


	public enum EventDescriptorType : uint {
		String = 1,
		Float,
		Int32,
		Int16,
		Int8,
		Bool
	}
}