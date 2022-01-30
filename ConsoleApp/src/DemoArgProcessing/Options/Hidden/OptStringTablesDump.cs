using System;
using System.Collections.Immutable;
using System.IO;
using DemoParser.Parser;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {

	public class OptStringTablesDump : DemoOption {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--dump-stringtables"}.ToImmutableArray();


		public OptStringTablesDump() : base(
			DefaultAliases,
			$"Dumps string table packet(s)",
			true) {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}


		public override void Process(DemoParsingInfo infoObj) {
			infoObj.PrintOptionMessage("creating string table dump");
			TextWriter tw = infoObj.StartWritingText("stringtables");
			try {
				WriteStringTableDump((StreamWriter)tw, infoObj.CurrentDemo);
			} catch (Exception) {
				Utils.Warning("String table dump failed.\n");
			}
		}


		public static void WriteStringTableDump(StreamWriter sw, SourceDemo demo) {
			PrettyStreamWriter pw = new PrettyStreamWriter(sw.BaseStream);
			int stringTablesCount = 0; // spliced demos surely have more than one stringtables packet (unsure about regular demos)
			foreach (StringTables stringTablesPacket in demo.FilterForPacket<StringTables>()) {
				if (stringTablesCount++ > 0)
					pw.Append("\n\n\n");
				stringTablesPacket.PrettyWrite(pw);
			}
			// see note at PrettyStreamWriter
			pw.Flush();
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
