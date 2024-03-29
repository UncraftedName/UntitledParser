using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components.Messages.UserMessages;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options {

	public class OptTeleports : DemoOption<OptTeleports.FilterFlags> {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--teleports", "-T"}.ToImmutableArray();


		[Flags]
		public enum FilterFlags {
			PlayerOnly = 1,
			VerboseInfo = 2
		}


		public OptTeleports() : base(
			DefaultAliases,
			Arity.ZeroOrOne,
			"Find instances where entities got teleported by a portal." +
			"\nNote that this is only supported for portal 1 and will not check for camera teleports." +
			"\nThe flags can be combined.",
			"flags",
			Utils.ParseEnum<FilterFlags>,
			FilterFlags.VerboseInfo) {}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, FilterFlags flags, bool isDefault) {
			setupObj.ExecutableOptions++;
		}


		protected override void Process(DemoParsingInfo infoObj, FilterFlags flags, bool isDefault) {
			infoObj.PrintOptionMessage("looking for teleports");
			try {
				bool any = false;
				foreach ((EntityPortalled userMessage, int tick) in FindTeleports(infoObj.CurrentDemo, flags)) {
					any = true;
					Console.Write($"[{tick}]");
					if ((flags & FilterFlags.VerboseInfo) != 0) {
						Console.Write("\n");
						Console.WriteLine(userMessage.ToString());
					} else {
						Console.Write((flags & FilterFlags.PlayerOnly) != 0
							? " player"
							: $" entity {userMessage.Portalled}");
						Console.WriteLine($" went through portal {userMessage.Portal}");
					}
				}
				if (!any) {
					Console.WriteLine((flags & FilterFlags.PlayerOnly) != 0
						? "player never teleported"
						: "no entities were teleported");
				}
			} catch (Exception) {
				Utils.Warning("Search for teleports failed.\n");
			}
		}


		public static IEnumerable<(EntityPortalled userMessage, int tick)> FindTeleports(SourceDemo demo, FilterFlags flags) {
			var messages = demo.FilterForUserMessage<EntityPortalled>();
			if ((flags & FilterFlags.PlayerOnly) != 0)
				messages = messages.Where(t => t.userMessage.Portalled.EntIndex == 1);
			return messages;
		}


		protected override void PostProcess(DemoParsingInfo infoObj, FilterFlags flags, bool isDefault) {}
	}
}
