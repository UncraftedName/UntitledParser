using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcGameEvent : DemoMessage {

		public uint EventId;
		public GameEventDescription? EventDescription;
		public List<(string name, object descriptor)>? EventDescriptors;


		public SvcGameEvent(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			uint dataBitLen = bsr.ReadUInt(11);
			int indexBeforeData = bsr.CurrentBitIndex;

			EventId = bsr.ReadUInt(9);
			if (GameState.GameEventLookup == null) {
				// We failed to get the game event list earlier; we can't get the
				// game event but we should be able to continue parsing just fine.
				DemoRef.LogError($"{GetType().Name}: didn't get an {nameof(SvcGameEventList)} earlier, so we don't know what event this is");
			} else {
				if (!GameState.GameEventLookup.TryGetValue(EventId, out EventDescription)) {
					// okay now we know something went tragically wrong
					DemoRef.LogError($"{GetType().Name}: got invalid event ID '{EventId}'");
					bsr.SetOverflow();
					return;
				}
				EventDescriptors = new List<(string, object)>();

				foreach ((string Name, EventDescriptorType type) descriptor in EventDescription.Keys) {
					object o = descriptor.type switch {
						EventDescriptorType.String => bsr.ReadNullTerminatedString(),
						EventDescriptorType.Float  => bsr.ReadFloat(),
						EventDescriptorType.Int32  => bsr.ReadSInt(),
						EventDescriptorType.Int16  => bsr.ReadSShort(),
						EventDescriptorType.Int8   => bsr.ReadByte(),
						EventDescriptorType.Bool   => bsr.ReadBool(),
						EventDescriptorType.UInt64 => bsr.ReadULong(),
						_ => throw new ArgumentOutOfRangeException(nameof(descriptor.type), $"unknown descriptor type: {descriptor.type}")
					};
					EventDescriptors.Add((descriptor.Name, o));
				}
			}

			bsr.CurrentBitIndex = (int)dataBitLen + indexBeforeData;
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			if (EventDescription == null) {
				pw.Append($"Event ID: {EventId}, couldn't get event description");
				return;
			}
			pw.Append($"{EventDescription.Name} ({EventId})");
			if (EventDescriptors != null! && EventDescriptors.Count > 0) {
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
