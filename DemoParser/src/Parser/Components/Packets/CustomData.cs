#nullable enable
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets.CustomDataTypes;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {

	/// <summary>
	/// Contains a single custom game message, only exists in Portal 2?
	/// </summary>
	public class CustomData : DemoPacket {

		public CustomDataType DataType;
		public CustomDataMessage DataMessage;


		public CustomData(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			DataType = (CustomDataType)bsr.ReadSInt();
			DataMessage = CustomDataFactory.CreateCustomDataMessage(DemoRef, DataType);
			if (DataMessage.GetType() == typeof(UnknownCustomDataMessage))
				DemoRef.LogError($"{GetType().Name}: unknown custom data type {DataType}");
			var cBsr = bsr.SplitAndSkip(bsr.ReadSInt() * 8);
			DataMessage.ParseStream(ref cBsr);
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
