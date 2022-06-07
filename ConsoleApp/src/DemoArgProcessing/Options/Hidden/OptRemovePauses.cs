using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {

	public class OptRemovePauses : DemoOption {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--remove-pauses"}.ToImmutableArray();


		public OptRemovePauses() : base(DefaultAliases, "Create a new demo without pauses", true) {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
			setupObj.EditsDemos = true;
		}


		public override void Process(DemoParsingInfo infoObj) {
			infoObj.PrintOptionMessage("removing pauses");
			Stream s = infoObj.StartWritingBytes("removed-pauses", ".dem");
			try {
				RemovePauses(infoObj.CurrentDemo, s);
			} catch (Exception) {
				Utils.Warning("Pause removal failed.\n");
				infoObj.CancelOverwrite = true;
			}
		}


		public static void RemovePauses(SourceDemo demo, Stream s) {
			int writeFrom = 0;
			byte[] data = demo.Reader.Data;
			foreach (PacketFrame frame in demo.Frames) {
				bool writeFrame = frame.Packet switch {
					Packet packet when packet.MessageStream.All(dm => dm is NetNop) => false,
					ConsoleCmd {Command: "gameui_activate"} => false,
					_ => true
				};
				if (writeFrame) {
					int writeTo = (frame.Reader.AbsoluteStart + frame.Reader.BitLength) / 8;
					s.Write(data, writeFrom, writeTo - writeFrom);
				}
				writeFrom = (frame.Reader.AbsoluteStart + frame.Reader.BitLength) / 8;
			}
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
