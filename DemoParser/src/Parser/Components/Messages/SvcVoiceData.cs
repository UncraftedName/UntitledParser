using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using System;

namespace DemoParser.Parser.Components.Messages;

public class SvcVoiceData : DemoMessage {

	public byte FromClient;
	public bool Proximity;
	public int BitLen;
	public bool[]? Unknown;

	public SvcVoiceData(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


	protected override void Parse(ref BitStreamReader bsr) {
		FromClient = bsr.ReadByte();
		Proximity = bsr.ReadByte() != 0;
		BitLen = bsr.ReadUShort();
		if (DemoRef.DemoInfo.Game.IsLeft4Dead()) {
			Unknown = new bool[4];
			for (int i = 0; i < 4; i++)
				Unknown[i] = bsr.ReadBool();
		}
		BitStreamReader dataBsr = bsr.ForkAndSkip(BitLen);
	}

	public override void PrettyWrite(IPrettyWriter pw) {
		pw.AppendLine($"client: {FromClient}");
		pw.AppendLine($"proximity: {Proximity}");
		pw.Append($"bit length: {BitLen}");
		if (Unknown != null ) {
			pw.AppendLine();
			pw.Append("unknown bits: ");
			foreach (var v in Unknown) {
				pw.Append($" {v}");
			}
		}
	}
}
