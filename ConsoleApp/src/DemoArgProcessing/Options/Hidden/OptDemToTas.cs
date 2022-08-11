using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Packets;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {

	public class OptDemToTas : DemoOption<string> {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--demo-to-tas"}.ToImmutableArray();


		private static string SaveNameValidator(string s) {
			if (!Regex.IsMatch(s, "^[-_ 0-9a-zA-Z]+$"))
				throw new ArgProcessUserException("invalid save name");
			return s;
		}


		public OptDemToTas() : base(
			DefaultAliases,
			Arity.One,
			"Create a afterframes TAS script of the demo(s) to be run with the spt plugin (not a perfect replication and will often desync)",
			"save name",
			SaveNameValidator,
			null!,
			true) {}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, string arg, bool isDefault) {
			setupObj.ExecutableOptions++;
		}


		protected override void Process(DemoParsingInfo infoObj, string saveName, bool isDefault) {
			TextWriter tw = infoObj.StartWritingText("tas", ".cfg");
			try {
				WriteDemoToTas(infoObj.CurrentDemo, tw, saveName);
			} catch (Exception) {
				Utils.Warning("Could not convert demo to TAS.\n");
			}
		}


		public static void WriteDemoToTas(SourceDemo demo, TextWriter tw, string saveName) {
			tw.Write(
				"unpause\n" +
				$"load \"{saveName}\"\n" +
				"sv_cheats 1\n" +
				"host_framerate 0.015\n" +
				"fps_max 66.666\n" +
				"_y_spt_afterframes_await_load\n" +
				"_y_spt_afterframes_reset_on_server_activate 0\n" +
				"y_spt_pause 0\n\n"
			);
			Regex quoteRe = new Regex("\"([^ \"]+)\"", RegexOptions.Compiled);
			int lastCmdTick = int.MinValue;
			int lastUCmdTick = int.MinValue;
			string lastCmd = null!;
			foreach (PacketFrame frame in demo.Frames) {
				// offset client tick by 1, this seems to be more accurate with cycled mechanics e.g. CheckStuck
				int tick = frame.Tick - 1;
				if (tick < 0)
					continue;
				switch (frame.Packet) {
					case ConsoleCmd cmd when lastCmdTick != tick || lastCmd != cmd.Command:
						lastCmd = cmd.Command;
						string cmdNoQuotes = quoteRe.Replace(cmd.Command, match => match.Groups[1].Value);
						if (cmdNoQuotes.Contains('"'))
							Utils.Warning($"Could not remove quotes from command '{cmd.Command}' on tick {tick}\n");
						lastCmdTick = tick;
						tw.WriteLine($"_y_spt_afterframes {tick} \"{cmdNoQuotes}\"");
						break;
					case UserCmd uCmd when lastUCmdTick != tick:
						lastUCmdTick = tick;
						tw.WriteLine($"_y_spt_afterframes {tick} \"_y_spt_setangles {uCmd.ViewAngleX:R} {uCmd.ViewAngleY:R}\"");
						break;
				}
			}
		}


		protected override void PostProcess(DemoParsingInfo infoObj, string arg, bool isDefault) {}
	}
}
