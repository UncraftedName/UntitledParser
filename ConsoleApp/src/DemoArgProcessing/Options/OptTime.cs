using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;
using DemoParser.Parser.Components;
using DemoParser.Utils;
using static System.Text.RegularExpressions.RegexOptions;

namespace ConsoleApp.DemoArgProcessing.Options {

	public class OptTime : DemoOption<OptTime.TimeFlags> {

		public static readonly ImmutableArray<string> DefaultAliases = new[] {"--time", "-t"}.ToImmutableArray();

		private const int FmtIdt = -25;

		// a hack to allow only p1 & l4d to time w/ first tick
		private ISet<SourceGame> _forceFirstTickTiming = null!;


		[Flags]
		public enum TimeFlags {
			NoHeader = 1,
			TimeFirstTick = 2,
			AlwaysShowTotalTime = 4,
		}


		private SimpleDemoTimer _sdt;


		public OptTime() : base(
			DefaultAliases,
			Arity.ZeroOrOne,
			"Print demo header and time info, enabled automatically if no other options are set." +
			"\nBy default, 1 tick is added for Portal 1 and L4D games." +
			$"\nNote that flags can be combined, e.g. \"{TimeFlags.NoHeader | TimeFlags.AlwaysShowTotalTime}\" or \"5\".",
			"flags",
			Utils.ParseEnum<TimeFlags>,
			default)
		{
			_sdt = null!; // 'AfterParse' will initialize this
		}


		public override void Reset() {
			base.Reset();
			_sdt = null!;
			_forceFirstTickTiming = new HashSet<SourceGame>();
		}


		protected override void AfterParse(DemoParsingSetupInfo setupObj, TimeFlags arg, bool isDefault) {
			setupObj.ExecutableOptions++;
			_sdt = new SimpleDemoTimer((arg & TimeFlags.TimeFirstTick) != 0);
		}


		protected override void Process(DemoParsingInfo infoObj, TimeFlags arg, bool isDefault) {
			infoObj.PrintOptionMessage("timing demo");
			try {
				if ((arg & TimeFlags.NoHeader) == 0)
					WriteHeader(infoObj.CurrentDemo, Console.Out, infoObj.SetupInfo.ExecutableOptions != 1);
				_sdt.Consume(infoObj.CurrentDemo);
				if (!infoObj.FailedLastParse) {
					WriteAdjustedTime(
						infoObj.CurrentDemo,
						Console.Out,
						(arg & TimeFlags.TimeFirstTick) != 0 || _forceFirstTickTiming.Contains(infoObj.CurrentDemo.DemoInfo.Game)
					);
					Console.WriteLine();
				}
			} catch (Exception) {
				Utils.Warning("Timing demo failed.\n");
			}
		}


		protected override void PostProcess(DemoParsingInfo infoObj, TimeFlags arg, bool isDefault) {
			bool showTotal = _sdt.ValidFlags.HasFlag(SimpleDemoTimer.Flags.TotalTimeValid);
			bool showAdjusted = _sdt.ValidFlags.HasFlag(SimpleDemoTimer.Flags.AdjustedTimeValid);
			bool overwrite = (arg & TimeFlags.AlwaysShowTotalTime) != 0;
			if (!overwrite && infoObj.NumDemos == 1)
				return;

			// show message if only one is invalid, or if we're overwriting and either is invalid
			if ((showTotal ^ showAdjusted) || (overwrite && (!showTotal || !showAdjusted))) {
				string which;
				if (!showTotal && !showAdjusted)
					which = "Total and adjusted";
				else if (!showTotal)
					which = "Total";
				else
					which = "Adjusted";
				Utils.Warning($"{which} time may not be valid.\n\n");
			}
			Utils.PushForegroundColor(ConsoleColor.Green);
			if (showTotal || overwrite) {
				Console.WriteLine($"{"Total measured time", FmtIdt}: {Utils.FormatTime(_sdt.TotalTime)}");
				Console.WriteLine($"{"Total measured ticks", FmtIdt}: {_sdt.TotalTicks}");
				showAdjusted &= _sdt.TotalTicks != _sdt.AdjustedTicks;
			}
			if (showAdjusted || overwrite) {
				Console.WriteLine($"{"Total adjusted time", FmtIdt}: {Utils.FormatTime(_sdt.AdjustedTime)}");
				Console.WriteLine($"{"Total adjusted ticks", FmtIdt}: {_sdt.AdjustedTicks}");
			}
			Utils.PopForegroundColor();
			if (!showTotal && !showAdjusted && !overwrite)
				Console.WriteLine($"Not showing total time from {DefaultAliases[0]} since it may not be valid. Use '{DefaultAliases[0]} {TimeFlags.AlwaysShowTotalTime}' to show it regardless.");
		}


		public static void WriteHeader(SourceDemo demo, TextWriter tw, bool writeDemoName = true) {
			DemoHeader h = demo.Header;
			if (writeDemoName)
				tw.WriteLine($"{"File name ",FmtIdt}: {demo.FileName}");
			tw.Write(
				$"{"Demo protocol ",      FmtIdt}: {h.DemoProtocol}"                   +
				$"\n{"Network protocol ", FmtIdt}: {h.NetworkProtocol}"                +
				$"\n{"Server name ",      FmtIdt}: {h.ServerName}"                     +
				$"\n{"Client name ",      FmtIdt}: {h.ClientName}"                     +
				$"\n{"Map name ",         FmtIdt}: {h.MapName}"                        +
				$"\n{"Game directory ",   FmtIdt}: {h.GameDirectory}"                  +
				$"\n{"Playback time ",    FmtIdt}: {Utils.FormatTime(h.PlaybackTime)}" +
				$"\n{"Ticks ",            FmtIdt}: {h.TickCount}"                      +
				$"\n{"Frames ",           FmtIdt}: {h.FrameCount}"                     +
				$"\n{"SignOn Length ",    FmtIdt}: {h.SignOnLength}\n\n");
		}


		// there's some dragons here, for now they keep me good company
		public static void WriteAdjustedTime(SourceDemo demo, TextWriter tw, bool timeFirstTick) {
			bool tfs = timeFirstTick;
			(string str, Regex re)[] customRe = {
				("Autosave",                new Regex(@"^\s*autosave\s*$",                IgnoreCase)),
				("Autosavedangerous",       new Regex(@"^\s*autosavedangerous\s*$",       IgnoreCase)),
				("Autosavedangerousissafe", new Regex(@"^\s*autosavedangerousissafe\s*$", IgnoreCase))
			};

			var customGroups = customRe.SelectMany(
					t => demo.CmdRegexMatches(t.re),
					(t, matchTup) => (t.str, matchTup.cmd.Tick))
				.OrderBy(reInfo => reInfo.Tick)
				.GroupBy(reInfo => reInfo.str, reInfo => reInfo.Tick)
				.OrderBy(grouping => grouping.Min())
				.ToList();

			Regex flagRe = new Regex(@"^\s*echo\s+#(?<flag_name>.*)#\s*$", IgnoreCase);

			var flagGroups = demo.CmdRegexMatches(flagRe)
				.OrderBy(t => t.cmd.Tick)
				.GroupBy(t => t.matches[0].Groups["flag_name"].Value.ToUpper(), t => t.cmd.Tick)
				.OrderBy(grouping => grouping.Min())
				.ToList();

			double tickInterval = demo.DemoInfo.TickInterval;

			List<int> ticks = customGroups.SelectMany(g => g)
				.Concat(flagGroups.SelectMany(g => g)).ToList();

			List<string>
				descriptions = customGroups.Select(g => Enumerable.Repeat(g.Key, g.Count()))
					.Concat(flagGroups.Select(g => Enumerable.Repeat($"'{g.Key}' flag", g.Count())))
					.SelectMany(e => e).ToList(),
				tickStrs = ticks.Select(i => i.ToString()).ToList(),
				times = ticks.Select(i => Utils.FormatTime(i * tickInterval)).ToList();

			if (descriptions.Any()) {
				int maxDescPad = descriptions.Max(s => s.Length),
					tickMaxPad = tickStrs.Max(s => s.Length),
					timeMadPad = times.Max(s => s.Length);

				int customCount = customGroups.SelectMany(g => g).Count();
				string previousDesc = null!;
				Utils.PushForegroundColor();
				for (int i = 0; i < descriptions.Count; i++) {
					if (i < customCount) {
						Console.ForegroundColor = ConsoleColor.DarkYellow;
					} else {
						if (previousDesc == null || descriptions[i] != previousDesc) {
							Console.ForegroundColor = Console.ForegroundColor == ConsoleColor.Yellow
								? ConsoleColor.Magenta
								: ConsoleColor.Yellow;
						}
					}
					previousDesc = descriptions[i];
					tw.WriteLine($"{descriptions[i].PadLeft(maxDescPad)} on tick " +
											 $"{tickStrs[i].PadLeft(tickMaxPad)}, time: {times[i].PadLeft(timeMadPad)}");
				}
				Utils.PopForegroundColor();
				tw.WriteLine();
			}

			Utils.PushForegroundColor(ConsoleColor.Cyan);
			tw.WriteLine($"{"Measured time ",FmtIdt}: {Utils.FormatTime(demo.TickCount(tfs) * tickInterval)}");
			tw.WriteLine($"{"Measured ticks ",FmtIdt}: {demo.TickCount(tfs)}");

			if (demo.TickCount(tfs) != demo.AdjustedTickCount(tfs)) {
				tw.WriteLine($"{"Adjusted time ",FmtIdt}: {Utils.FormatTime(demo.AdjustedTickCount(tfs) * tickInterval)}");
				tw.Write($"{"Adjusted ticks ",FmtIdt}: {demo.AdjustedTickCount(tfs)}");
				tw.WriteLine($" ({demo.StartAdjustmentTick}-{demo.EndAdjustmentTick})");
			}
			Utils.PopForegroundColor();
		}


		// a hack to allow only p1 & l4d to time w/ first tick
		internal void SetForceFirstTickTiming(IEnumerable<SourceGame> games) {
			var gameSet = games.ToImmutableHashSet();
			_forceFirstTickTiming = gameSet;
			_sdt.SetForceFirstTickTiming(gameSet);
		}
	}


	/// <summary>
	/// Accepts one demo at a time to calculate the total time.
	/// </summary>
	public class SimpleDemoTimer {

		public int TotalTicks {get;private set;}
		public double TotalTime {get;private set;}
		public int AdjustedTicks {get;private set;}
		public double AdjustedTime {get;private set;}
		private readonly bool _timeFirstTick;
		public Flags ValidFlags {get;private set;}
		private int? _firstHash;

		// a hack to allow only p1 & l4d to time w/ first tick
		private SourceDemo _curDemo = null!;
		private ISet<SourceGame> _forceFirstTickTiming;
		public bool TimeFirstTick => _timeFirstTick || _forceFirstTickTiming.Contains(_curDemo.DemoInfo.Game);


		public SimpleDemoTimer(bool timeFirstTick) {
			_timeFirstTick = timeFirstTick;
			ValidFlags = Flags.TotalTimeValid | Flags.AdjustedTimeValid;
			_forceFirstTickTiming = new HashSet<SourceGame>();
		}


		public SimpleDemoTimer(IEnumerable<SourceDemo> demos, bool timeFirstTick) : this(timeFirstTick) {
			foreach (SourceDemo demo in demos)
				Consume(demo);
		}


		internal void SetForceFirstTickTiming(IEnumerable<SourceGame> games) => _forceFirstTickTiming = games.ToImmutableHashSet();


		/// <summary>
		/// Calculates the total time of the given demo and adds it to the total time. Sets ValidFlags if necessary.
		/// </summary>
		public void Consume(SourceDemo demo) {
			_curDemo = demo;
			try {
				_firstHash ??= QuickHash(demo);
				// consider this demo to be part of a different run if the hash doesn't match
				if (_firstHash.Value != QuickHash(demo))
					ValidFlags &= ~(Flags.TotalTimeValid | Flags.AdjustedTimeValid);

				if (TimingAdjustment.ExcludedMaps.Contains((demo.DemoInfo.Game, demo.Header.MapName)))
					return;

				int newTicks, newAdjustedTicks;
				if (demo.TotalTimeValid()) {
					newTicks = demo.TickCount(TimeFirstTick);
				} else {
					ValidFlags &= ~Flags.TotalTimeValid;
					// pull tick count straight from the header if parsing failed
					newTicks = demo.Header.TickCount + (TimeFirstTick ? 1 : 0);
				}
				TotalTicks += newTicks;
				TotalTime += newTicks * demo.DemoInfo.TickInterval;

				if (demo.AdjustedTimeValid()) {
					newAdjustedTicks = demo.AdjustedTickCount(TimeFirstTick);
				} else {
					ValidFlags &= ~Flags.AdjustedTimeValid;
					newAdjustedTicks = newTicks;
				}
				AdjustedTicks += newAdjustedTicks;
				AdjustedTime += newAdjustedTicks * demo.DemoInfo.TickInterval;
			} catch (NullReferenceException) {
				ValidFlags &= ~(Flags.TotalTimeValid | Flags.AdjustedTimeValid);
				throw;
			}
		}


		private static int QuickHash(SourceDemo demo)
			=> HashCode.Combine(
				demo.Header.NetworkProtocol,
				demo.DemoInfo.TickInterval,
				demo.Header.DemoProtocol,
				demo.Header.GameDirectory);


		[Flags]
		public enum Flags {
			TotalTimeValid = 1,
			AdjustedTimeValid = 2
		}
	}
}
