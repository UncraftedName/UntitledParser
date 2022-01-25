using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DemoParser.Parser;
using DemoParser.Parser.Components.Messages;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options {

	public class OptPauses : DemoOption {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--pauses", "-p"}.ToImmutableArray();


		public OptPauses() : base(DefaultAliases, "Find pauses") {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}


		public override void Process(DemoParsingInfo infoObj) {
			infoObj.PrintOptionMessage("looking for pauses");
			try {
				bool any = false;
				foreach (int pauseTick in GetPauseTicks(infoObj.CurrentDemo)) {
					any = true;
					Console.Write($"[{pauseTick}] pause");
					if (pauseTick > infoObj.CurrentDemo.EndAdjustmentTick)
						Console.Write(" (after last adjusted tick)");
					else if (pauseTick < infoObj.CurrentDemo.StartAdjustmentTick)
						Console.Write(" (before first adjusted tick)");
					Console.WriteLine();
				}
				if (!any)
					Console.WriteLine("no pauses found");
			} catch (Exception) {
				Utils.Warning("Search for pauses failed.\n");
			}
		}


		public static IEnumerable<int> GetPauseTicks(SourceDemo demo) {
			return
				from packet in demo.FilterForPacket<Packet>()
				from messageTup in packet.MessageStream
				where messageTup.message?.GetType() == typeof(SvcSetPause)
				where ((SvcSetPause)messageTup.message).IsPaused
				select packet.Tick;
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
