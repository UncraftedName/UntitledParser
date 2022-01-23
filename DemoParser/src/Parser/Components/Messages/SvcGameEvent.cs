using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcGameEvent : DemoMessage {

		public uint EventId;
		public GameEventDescription EventDescription;
		public List<(string name, object descriptor)> EventDescriptors;


		public SvcGameEvent(SourceDemo? demoRef) : base(demoRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			uint dataBitLen = bsr.ReadBitsAsUInt(11);
			int indexBeforeData = bsr.CurrentBitIndex;

			EventId = bsr.ReadBitsAsUInt(9);
			EventDescription = DemoRef.GameEventManager.EventDescriptions[EventId];
			EventDescriptors = new List<(string, object)>();

			foreach ((string Name, EventDescriptorType type) descriptor in EventDescription.Keys) {
				object o = descriptor.type switch {
					EventDescriptorType.String => bsr.ReadNullTerminatedString(),
					EventDescriptorType.Float  => bsr.ReadFloat(),
					EventDescriptorType.Int32  => bsr.ReadSInt(),
					EventDescriptorType.Int16  => bsr.ReadSShort(),
					EventDescriptorType.Int8   => bsr.ReadByte(),
					EventDescriptorType.Bool   => bsr.ReadBool(),
					_ => throw new ArgumentOutOfRangeException(nameof(descriptor.type), $"unknown descriptor type: {descriptor.type}")
				};
				EventDescriptors.Add((descriptor.Name, o));
			}

			bsr.CurrentBitIndex = (int)dataBitLen + indexBeforeData;
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"{EventDescription.Name} ({EventId})");
			if (EventDescriptors != null && EventDescriptors.Count > 0) {
				pw.FutureIndent++;
				foreach ((string name, object descriptor) in EventDescriptors) {
					pw.AppendLine();
					pw.Append($"{name}: {descriptor}");
				}
				pw.FutureIndent--;
			}
		}
	}
}
