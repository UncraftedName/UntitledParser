using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages;

public class SvcVoiceData : DemoMessage {

	public byte FromClient;
	public bool Proximity;
	public int BitLen;

	public SvcVoiceData(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


	protected override void Parse(ref BitStreamReader bsr) {
		FromClient = bsr.ReadByte();
		Proximity = bsr.ReadByte() != 0;
		BitLen = bsr.ReadUShort();

		BitStreamReader dataBsr = bsr.ForkAndSkip(BitLen);
	}

	public override void PrettyWrite(IPrettyWriter pw) {
		pw.AppendLine($"client: {FromClient}");
		pw.AppendLine($"proximity: {Proximity}");
		pw.Append($"bit length: {BitLen}");
	}
}
