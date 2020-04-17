using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {
	
	public class SvcGameEvent : DemoMessage {

		public uint EventID;
		public GameEventDescription EventDescription; // i initialize this while parsing and keep a local copy
		public List<(string, object)> EventDescriptors;
		

		public SvcGameEvent(SourceDemo demoRef, BitStreamReader reader) : base(demoRef, reader) {}
		
		
		internal override void ParseStream(BitStreamReader bsr) {
			uint dataBitLen = bsr.ReadBitsAsUInt(11);
			int indexBeforeData = bsr.CurrentBitIndex;
			
			EventID = bsr.ReadBitsAsUInt(9);
			EventDescription = DemoRef.GameEventManager.DescriptorsForEvents.Find(description => description.EventID == EventID);
			EventDescriptors = new List<(string, object)>();
			
			foreach ((string Name, EventDescriptorType type) descriptor in EventDescription.Keys) {
				object o = descriptor.type switch {
					EventDescriptorType.String => bsr.ReadNullTerminatedString(),
					EventDescriptorType.Float => bsr.ReadFloat(),
					EventDescriptorType.Int32 => bsr.ReadSInt(),
					EventDescriptorType.Int16 => bsr.ReadSShort(),
					EventDescriptorType.Int8 => bsr.ReadByte(),
					EventDescriptorType.Bool => bsr.ReadBool(),
					_ => throw new ArgumentOutOfRangeException(nameof(descriptor.type), $"unknown descriptor type: {descriptor.type}")
				};
				EventDescriptors.Add((descriptor.Name, o));
			}
			
			bsr.CurrentBitIndex = (int)dataBitLen + indexBeforeData;
			SetLocalStreamEnd(bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append($"{EventDescription.Name} ({EventID}):");
			if (EventDescriptors != null && EventDescriptors.Count > 0) {
				iw.AddIndent();
				foreach ((var key, object value) in EventDescriptors) {
					iw.AppendLine();
					iw.Append($"{key}: {value}");
				}
				iw.SubIndent();
			} else {
				iw.Append(" (no descriptors)");
			}
		}
	}
}