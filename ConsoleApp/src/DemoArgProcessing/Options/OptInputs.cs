using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
			"prints user inputs on every tick",
			Utils.ParseEnum<InputDisplayMode>,
			InputDisplayMode.Text) {}
		
		
		protected override void AfterParse(DemoParsingSetupInfo setupObj, InputDisplayMode mode, bool isDefault) {
			setupObj.ExecutableOptions++;
		}


		protected override void Process(DemoParsingInfo infoObj, InputDisplayMode mode, bool isDefault) {
			TextWriter tw = infoObj.InitTextWriter("getting user inputs", "inputs");
			foreach ((int tick, string repr) in GetUserInputs(infoObj.CurrentDemo, mode))
				tw.WriteLine($"[{tick}] {repr}");
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
				switch (mode) {
					case InputDisplayMode.Text:
						yield return (u.Tick, b?.ToString() ?? "none");
						break;
					case InputDisplayMode.Int:
						yield return (u.Tick, b.HasValue ? ((uint)b.Value).ToString() : "0");
						break;
					case InputDisplayMode.Flags:
						yield return (u.Tick, Convert.ToString(b.HasValue ? (uint)b.Value : 0, 2).PadLeft(32, '0'));
						break;
					default:
						throw new ArgProcessProgrammerException($"invalid input display mode: \"{mode}\"");
				}
			}
		}


		protected override void PostProcess(DemoParsingInfo infoObj, InputDisplayMode mode, bool isDefault) {}
	}
}
