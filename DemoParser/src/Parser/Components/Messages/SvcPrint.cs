using System.Text.RegularExpressions;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Messages {

	public class SvcPrint : DemoMessage {

		public string Str;


		public SvcPrint(SourceDemo? demoRef, byte value) : base(demoRef, value) {}


		protected override void Parse(ref BitStreamReader bsr) {
			Str = bsr.ReadNullTerminatedString();
			// HACK: to counter the horrid feats of L4D2 devs breaking protocol compatibility more than once without bumping any version,
			// we have to parse the first SvcPrint in the SignOn to identify versions of L4D2 that have changes. Check for the build number
			// on the string to identify the version
			if (DemoInfo.Game == SourceGame.L4D2_2042) {
				Match m = Regex.Match(Str, "Build: (\\d+)\nServer Number:");
				if (m.Success) {
					switch (m.Groups[1].Value) {
						case "4710":
							DemoInfo.Game = SourceGame.L4D2_2091;
							break;
						case "6403":
							DemoInfo.Game = SourceGame.L4D2_2147;
							break;
					}
				}
			}

		}


		public override void PrettyWrite(IPrettyWriter pw) {
			string trimmed = Str.Trim();
			pw.Append("string: ");
			if (trimmed.Contains("\n")) {
				pw.FutureIndent++;
				pw.AppendLine();
				pw.Append(trimmed);
				pw.FutureIndent--;
			} else {
				pw.Append(trimmed);
			}
		}
	}
}
