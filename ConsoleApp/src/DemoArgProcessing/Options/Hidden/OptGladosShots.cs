using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using DemoParser.Parser;
using DemoParser.Parser.Components.Messages;
using DemoParser.Utils;
using static DemoParser.Parser.Components.Messages.SoundInfo.SoundFlags;

namespace ConsoleApp.DemoArgProcessing.Options.Hidden {

	public class OptGladosShots : DemoOption {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--glados-shots"}.ToImmutableArray();


		public OptGladosShots() : base(
			DefaultAliases,
			"Search for ticks where GLaDOS' pitch speeds up from taking damage",
			true) {}


		public override void AfterParse(DemoParsingSetupInfo setupObj) {
			setupObj.ExecutableOptions++;
		}


		public override void Process(DemoParsingInfo infoObj) {
			infoObj.PrintOptionMessage("searching for GLaDOS pitch changes");
			try {
				bool any = false;
				foreach (int tick in GetGladosShotTicks(infoObj.CurrentDemo)) {
					any = true;
					Console.WriteLine($"[{tick}]");
				}
				if (!any)
					Console.WriteLine("GLaDOS did not get shot");
			} catch (Exception) {
				Utils.Warning("Search for GLaDOS pitch changes failed.\n");
			}
		}


		public static IEnumerable<int> GetGladosShotTicks(SourceDemo demo) {
			// no filtering for specific sounds, idk of anything else in the game that changes sound pitch
			return from msgTup in demo.FilterForMessage<SvcSounds>()
				where msgTup.message.Sounds != null
				from sound in msgTup.message.Sounds
				where (sound.Flags & (ChangePitch | Delay)) == ChangePitch && sound.Pitch == 250 // remove the delay check if some ticks are skipped
				select msgTup.tick;
		}


		public override void PostProcess(DemoParsingInfo infoObj) {}
	}
}
