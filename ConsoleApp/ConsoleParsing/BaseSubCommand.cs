using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace ConsoleApp.ConsoleParsing {
	
	public abstract class BaseSubCommand<TSetup, TInfo> where TInfo : IProcessObject {
		
		private readonly IImmutableList<BaseOption<TSetup, TInfo>> _options;
		private readonly IImmutableDictionary<string, BaseOption<TSetup, TInfo>> _optionLookup;


		// order matters here, this is the same order in which the options will get processed
		protected BaseSubCommand(IImmutableList<BaseOption<TSetup, TInfo>> options) {
			_options = options;
			HashSet<string> aliasSet = new HashSet<string>();
			foreach (var option in options) {
				if (option.Aliases.Count == 0)
					throw new Exception($"Option '{option.GetType().Name}' has no aliases.");
				foreach (string al in option.Aliases)
					if (!aliasSet.Add(al))
						throw new Exception($"Alias '{al}' is present twice in subcommand '{GetType().Name}'.");
			}
			_optionLookup = options
				.SelectMany(o => o.Aliases, (option, alias) => (option, alias))
				.ToImmutableDictionary(tuple => tuple.alias, tuple => tuple.option);
		}


		/// <summary>
		/// Process an argument that was determined to not be passed to an option, throws if this argument is invalid.
		/// </summary>
		protected abstract void ParseDefaultArgument(string str);
		

		protected void ParseArgs(string[] args, TSetup setupObj, int argIdx = 1) {
			Debug.Assert(argIdx >= 0 && argIdx <= args.Length - 1);
			while (argIdx < args.Length) {
				string arg = args[argIdx];
				if (_optionLookup.TryGetValue(arg, out BaseOption<TSetup, TInfo> option)) {
					switch (option.Arity) {
						case Arity.Zero:
							option.Enable(setupObj, null);
							break;
						case Arity.ZeroOrOne:
						case Arity.One:
							if (argIdx + 1 < args.Length && option.CanUseAsArg(args[argIdx + 1])) {
								option.Enable(setupObj, args[++argIdx]); // advance so we skip arg later
							} else {
								if (option.Arity == Arity.One)
									throw new ArgProcessException($"Could not create argument for option '{option.Aliases[0]}'");
								option.Enable(setupObj, null);
							}
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				} else {
					ParseDefaultArgument(arg);
				}
				argIdx++;
			}
			foreach (var option in _options.Where(o => o.Enabled))
				option.AfterParse(setupObj);
		}


		protected void Process(TInfo infoObj) {
			var enabledOptions = _options.Where(o => o.Enabled).ToImmutableList();
			while (infoObj.CanAdvance()) {
				infoObj.Advance();
				foreach (var option in enabledOptions)
					option.Process(infoObj);
			}
			infoObj.DoneProcessing();
			foreach (var option in enabledOptions)
				option.PostProcess(infoObj);
		}


		public virtual void Reset() {
			foreach (var option in _options)
				option.Reset();
		}
	}

	/// <summary>
	/// A class that will be passed to options during processing. Note: A call will be made to advance before this is
	/// passed to options.
	/// </summary>
	public interface IProcessObject {
		public bool CanAdvance();
		public void Advance();
		public void DoneProcessing();
	}
}
