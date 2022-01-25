using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace ConsoleApp.DemoArgProcessing.Options {

	public class OptInputs : DemoOption<OptInputs.InputDisplayMode> {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--inputs", "-i"}.ToImmutableArray();


		public enum InputDisplayMode {
			Text,
			Int,
			Flags
		}


		public OptInputs() : base(
			DefaultAliases,
			Arity.ZeroOrOne,
			"Print user inputs on every tick",
			"mode",
			Utils.ParseEnum<InputDisplayMode>,
			InputDisplayMode.Text) {}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, InputDisplayMode mode, bool isDefault) {
			setupObj.ExecutableOptions++;
		}


		protected override void Process(DemoParsingInfo infoObj, InputDisplayMode mode, bool isDefault) {
			infoObj.PrintOptionMessage("getting user inputs");
			try {
				foreach ((int tick, string repr) in GetUserInputs(infoObj.CurrentDemo, mode))
					Console.WriteLine($"[{tick}] {repr}");
			} catch (Exception) {
				Utils.Warning("Getting user inputs failed.\n");
			}
		}


		public static IEnumerable<(int tick, string repr)> GetUserInputs(SourceDemo demo, InputDisplayMode mode) {
			Buttons? prevButtons = null;
			int prevTick = int.MinValue;
			foreach (UserCmd u in demo.FilterForPacket<UserCmd>()) {
				Buttons? b = u.Buttons;
				// don't want pauses taking up all the space
				if (prevButtons == b && prevTick == u.Tick)
					continue;
				prevButtons = b;
				prevTick = u.Tick;
				yield return mode switch {
					InputDisplayMode.Text => (u.Tick, b?.ToString() ?? "none"),
					InputDisplayMode.Int => (u.Tick, b.HasValue ? ((uint)b.Value).ToString() : "0"),
					InputDisplayMode.Flags => (u.Tick, Convert.ToString(b.HasValue ? (uint)b.Value : 0, 2).PadLeft(32, '0')),
					_ => throw new ArgProcessProgrammerException($"invalid input display mode: \"{mode}\"")
				};
			}
		}


		protected override void PostProcess(DemoParsingInfo infoObj, InputDisplayMode mode, bool isDefault) {}
	}
}
