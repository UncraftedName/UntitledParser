using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ConsoleApp.ConsoleParsing {
	
	public abstract class BaseSubCommand<TSetup, TInfo> {
		
		private readonly IImmutableList<BaseOption<TSetup, TInfo>> _options;
		private readonly IImmutableDictionary<string, BaseOption<TSetup, TInfo>> _optionLookup;


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
	}
}
