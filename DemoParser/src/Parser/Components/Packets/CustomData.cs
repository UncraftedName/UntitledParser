#nullable enable
using System;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets.CustomDataTypes;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {

	/// <summary>
	/// Contains a single custom game message.
	/// </summary>
	public class CustomData : DemoPacket {

		public CustomDataType DataType;
		public CustomDataMessage DataMessage;


		public CustomData(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			DataType = (CustomDataType)bsr.ReadSInt();
			uint size = bsr.ReadUInt();
			DataMessage = CustomDataFactory.CreateCustomDataMessage(DemoRef, DataType);
			try {
				DataMessage.ParseStream(bsr.SplitAndSkip(size * 8));
			} catch (Exception e) {
				DemoRef.LogError($"error while parsing custom data of type: {DataType}... {e.Message}");
				DataMessage = new UnknownCustomDataMessage(DemoRef);
			}
		}


		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.Append($"type: {DataType}");
			pw.FutureIndent++;
			pw.AppendLine();
			DataMessage.PrettyWrite(pw);
			pw.FutureIndent--;
		}
	}
}
