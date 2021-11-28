using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
			TextWriter tw = infoObj.StartWritingText("looking for pauses", "pauses");
			try {
				bool any = false;
				foreach (int pauseTick in GetPauseTicks(infoObj.CurrentDemo)) {
					any = true;
					tw.Write($"[{pauseTick}] pause");
					if (pauseTick > infoObj.CurrentDemo.EndAdjustmentTick)
						tw.Write(" (after last adjusted tick)");
					else if (pauseTick < infoObj.CurrentDemo.StartAdjustmentTick)
						tw.Write(" (before first adjusted tick)");
					tw.WriteLine();
				}
				if (!any)
					tw.WriteLine("no pauses found");
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
