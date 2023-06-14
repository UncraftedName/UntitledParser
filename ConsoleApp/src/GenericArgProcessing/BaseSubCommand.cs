using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ConsoleApp.GenericArgProcessing {

	/// <summary>
	/// A subcommand that can have some number of options and default arguments.
	/// </summary>
	/// <typeparam name="TSetup">The type of setup object that will be passed to the options when parsing arguments.</typeparam>
	/// <typeparam name="TInfo">The type of object that will be passed to the options when executing options.</typeparam>
	public abstract class BaseSubCommand<TSetup, TInfo> where TInfo : IProcessObject {

		private readonly IImmutableList<BaseOption<TSetup, TInfo>> _options;
		private readonly IImmutableDictionary<string, BaseOption<TSetup, TInfo>> _optionLookup;
		private readonly OptHelp _optHelp = new OptHelp();
		private readonly OptHelpAll _optHelpAll = new OptHelpAll();
		private readonly OptVersion _optVersion = new OptVersion();
		public abstract string VersionString {get;}
		public abstract string UsageString {get;}


		// order matters here, this is the same order in which the options will get processed
		protected BaseSubCommand(IImmutableList<BaseOption<TSetup, TInfo>> options) {
			_options = new BaseOption<TSetup, TInfo>[] {_optHelp, _optHelpAll, _optVersion}.Concat(options).ToImmutableArray();
			HashSet<string> aliasSet = new HashSet<string>();
			foreach (var option in options) {
				if (option.Aliases.Count == 0)
					throw new ArgProcessProgrammerException($"Option '{option.GetType().Name}' has no aliases.");
				foreach (string al in option.Aliases)
					if (!aliasSet.Add(al))
						throw new ArgProcessProgrammerException($"Alias '{al}' overlaps with a different option.");
			}
			_optionLookup = options
				.SelectMany(o => o.Aliases, (option, alias) => (option, alias))
				.ToImmutableDictionary(tuple => tuple.alias, tuple => tuple.option);
		}


		public bool TryGetOption(string alias, out BaseOption<TSetup, TInfo> option)
			=> _optionLookup.TryGetValue(alias, out option!);


		public int TotalEnabledOptions => _options.Count(o => o.Enabled);


		/// <summary>
		/// Process an argument that was determined to not be passed to an option. Return true on success and false on failure.
		/// </summary>
		protected abstract bool ParseDefaultArgument(string arg, out string? failReason);


		/// <summary>
		/// Parses the arguments, passing in the setup object to each option that is enabled.
		/// </summary>
		protected void ParseArgs(string[] args, TSetup setupObj) {
			Reset();
			args = args.Select(s => s.Trim()).ToArray();
			int argIdx = 0;
			string? argParseFailReason = null; // a bit of spaghetti so we can print a more informative message
			while (argIdx < args.Length) {
				string arg = args[argIdx];
				if (_optionLookup.TryGetValue(arg, out BaseOption<TSetup, TInfo>? option)) {
					argParseFailReason = null;
					switch (option.Arity) {
						case Arity.Zero:
							option.Enable(null);
							break;
						case Arity.ZeroOrOne:
							// check if the next arg looks like an option first
							if (argIdx + 1 < args.Length && _optionLookup.ContainsKey(args[argIdx + 1]))
								option.Enable(null);
							else
								goto case Arity.One;
							break;
						case Arity.One:
							if (argIdx + 1 < args.Length && option.CanUseAsArg(args[argIdx + 1], out argParseFailReason)) {
								// we can use the next string as an arg to the option, advance so we skip it on the next iteration
								option.Enable(args[++argIdx]);
							} else {
								// can't use arg for this option
								if (option.Arity == Arity.One) {
									if (argIdx + 1 >= args.Length)
										throw new ArgProcessUserException($"no argument specified for option '{arg}'");
									else
										throw new ArgProcessUserException($"\"{args[argIdx + 1]}\" is not a valid argument for option '{arg}': {argParseFailReason}.");
								}
								option.Enable(null);
							}
							break;
						default:
							throw new ArgProcessProgrammerException( $"invalid arity \"{option.Arity}\"");
					}
				} else {
					if (!ParseDefaultArgument(arg, out string? failReason)) {
						if (Regex.IsMatch(arg, "^-+[a-zA-Z]+.*"))
							throw new ArgProcessUserException($"{failReason!} and is not a valid option.");
						if (argParseFailReason == null)
							throw new ArgProcessUserException($"{failReason!}.");
						// we also tried to use this as an argument for the previous option, so we actually have 2 errors
						throw new ArgProcessUserException($"{failReason} and is not a valid argument for option '{args[argIdx - 1]}': {argParseFailReason}.");
					}
				}
				argIdx++;
			}
			foreach (var option in _options.Where(o => o.Enabled))
				option.AfterParse(setupObj);
		}


		// entry point from the scary outside world
		public abstract void Execute(params string[] args);
		public void Execute(int skip, params string[] args) => Execute(args.Skip(skip).ToArray());


		/// <summary>
		/// Parses the given arguments for --help or --version and handles that accordingly. Should be run before
		/// ParseArgs(), and the program should stop after calling this if this function returned true.
		/// </summary>
		/// <returns>Returns true if --help or --version are set and have been handled, false otherwise.</returns>
		protected bool CheckHelpAndVersion(string[] args) {
			var argSet = args.ToImmutableHashSet();
			if (argSet.Overlaps(_optHelpAll.Aliases)) {
				PrintHelp(true);
				return true;
			}
			if (argSet.Overlaps(_optHelp.Aliases)) {
				PrintHelp();
				return true;
			}
			if (argSet.Overlaps(_optVersion.Aliases)) {
				Console.WriteLine(VersionString);
				return true;
			}
			return false;
		}


		/// <summary>
		/// Prints the full help message for this subcommand.
		/// </summary>
		/// <param name="showHiddenOptions">If disabled, only shows options that don't have the "Hidden" property.</param>
		public void PrintHelp(bool showHiddenOptions = false) {

			int width;
			try {
				width = Console.WindowWidth;
			} catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is IOException) {
				width = 80;
			}

			Utils.WriteColor($"Usage: {UsageString}\n", ConsoleColor.DarkYellow);
			const string indentStr = "  ";
			bool any = false;
			foreach (var option in _options) {
				if (option.Hidden && !showHiddenOptions)
					continue;
				if (any)
					Console.WriteLine();
				any = true;
				Utils.PushForegroundColor(option.Hidden ? ConsoleColor.DarkGreen : ConsoleColor.Cyan);
				Console.Write(string.Join(", ", option.Aliases.OrderBy(s => s.Length)));
				switch (option.Arity) {
					case Arity.Zero:
						break;
					case Arity.ZeroOrOne:
						Console.Write($" [{option.ArgDescription}]");
						break;
					case Arity.One:
						Console.Write($" <{option.ArgDescription}>");
						break;
					default:
						throw new ArgProcessProgrammerException($"invalid arity: {option.Arity}");
				}
				if (option.Hidden)
					Console.Write(" (hidden)");
				Utils.PopForegroundColor();
				// Wrap the description string at the console width by removing at most width chars from the description
				// string each pass until it's empty.
				string desc = option.Description;
				while (desc.Length > 0) {
					Console.WriteLine();
					Console.Write(indentStr);
					int charsToFitOnThisLine = Math.Min(width - indentStr.Length - 1, desc.Length);
					string descTrunc = desc.Substring(0, charsToFitOnThisLine);
					// separate by new line always
					int sepIdx = descTrunc.IndexOf('\n');
					if (sepIdx == -1) {
						if (desc.Length + indentStr.Length <= width) {
							// rest of the description can fit on this line
							Console.Write(desc);
							break;
						}
						// no new line, try separating by space
						sepIdx = descTrunc.LastIndexOf(' ');
					}
					if (sepIdx == -1) {
						// no separator, truncate
						Console.Write(descTrunc);
						desc = desc.Substring(charsToFitOnThisLine);
					} else {
						Console.Write(desc.Substring(0, sepIdx));
						desc = desc.Substring(sepIdx + 1);
					}
				}
			}
		}


		/// <summary>
		/// Executes all options, Dispose will be called on infoObj after completion.
		/// </summary>
		/// <param name="infoObj">The object passed to all options during processing and post-processing</param>
		protected void Process(TInfo infoObj) {
			var enabledOptions = _options.Where(o => o.Enabled).ToImmutableList();
			while (infoObj.CanAdvance()) {
				infoObj.Advance();
				foreach (var option in enabledOptions) {
					option.Process(infoObj);
					infoObj.DoneWithOption();
				}
			}
			infoObj.DoneProcessing();
			foreach (var option in enabledOptions)
				option.PostProcess(infoObj);
		}


		/// <summary>
		/// Resets the state of this subcommand and all options before parsing new arguments.
		/// </summary>
		protected virtual void Reset() {
			foreach (var option in _options)
				option.Reset();
		}


		#region built-in options

		// this is just a lazy way of storing the description of these options


		private abstract class OptBuiltIn : BaseOptionNoArg<TSetup, TInfo> {
			protected OptBuiltIn(IImmutableList<string> aliases, string description, bool hidden = false)
				: base(aliases, description, hidden) {}
			public sealed override void AfterParse(TSetup setupObj) {}
			public sealed override void Process(TInfo infoObj) {}
			public sealed override void PostProcess(TInfo infoObj) {}
		}


		private class OptHelp : OptBuiltIn {
			public OptHelp() : base(new[] {"--help"}.ToImmutableArray(),
				$"Print help text and exit, to see all options use '{OptHelpAll.DefaultAlias}'") {}
		}


		private class OptHelpAll : OptBuiltIn {
			public const string DefaultAlias = "--help-all";
			public OptHelpAll() : base(new[] {DefaultAlias}.ToImmutableArray(),
				"Print help text including hidden options and exit", true) {}
		}


		private class OptVersion : OptBuiltIn {
			public OptVersion() : base(new[] {"--version"}.ToImmutableArray(),
				"Print program version and exit") {}
		}

		#endregion
	}

	/// <summary>
	/// A class that will be passed to options during processing.
	/// </summary>
	public interface IProcessObject {
		// returns true if we can go to the "next thing" to be processed (e.g. a demo)
		public bool CanAdvance();
		// Do all the setup required before processing the next round of options. Called once before rounds of processing begin.
		public void Advance();
		// Advance() is called after all options are done for their current pass, this is called after every option
		public void DoneWithOption();
		// resource cleanup if necessary (called after process but before post process)
		public void DoneProcessing();
	}
}
