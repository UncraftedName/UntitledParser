using System;
using System.Collections.Immutable;

namespace ConsoleApp.GenericArgProcessing {

	// do not inherit from this, use one of the classes below
	public abstract class BaseOption<TSetup, TInfo> where TInfo : IProcessObject {

		private protected static Exception InvalidFuncEx => new ArgProcessProgrammerException("this function shouldn't be called");

		public IImmutableList<string> Aliases {get;}

		public Arity Arity {get;}

		public string Description {get;}

		// if this has an argument, what is it called and optionally what values can be used?
		internal abstract string ArgDescription {get;}

		/// <summary>
		/// When set, specifies that "--help" should not show this option.
		/// </summary>
		public bool Hidden {get;}

		/// <summary>
		/// Specifies whether or not to execute the 'AfterParse', 'Process', and 'PostProcess' methods.
		/// </summary>
		public bool Enabled {get;private set;}

		// saves the arg passed to enable, user implemented classes shouldn't have to use this
		private protected virtual string? Arg {get;private set;}


		protected BaseOption(IImmutableList<string> aliases, Arity arity, string description, bool hidden) {
			if (aliases.Count == 0)
				throw new ArgProcessProgrammerException($"Option of type '{GetType().Name}' must have at least one alias.");
			Aliases = aliases;
			Arity = arity;
			Description = description;
			Hidden = hidden;
		}


		public void Enable(string? arg) {
			if (Arity == Arity.Zero && arg != null)
				throw new ArgProcessProgrammerException($"argument passed to option \"{Aliases[0]}\" with Arity {Arity.Zero}");
			Arg = arg;
			Enabled = true;
		}


		// just for option parsing, msg is non-null if we return false
		internal abstract bool CanUseAsArg(string arg, out string? failReason);


		/// <summary>
		/// Provides a way to reset the state of this object if anything was saved during processing.
		/// </summary>
		public virtual void Reset() {
			Enabled = false;
			Arg = null;
		}


		public override string ToString() => string.Join(", ", Aliases);


		public virtual void AfterParse(TSetup setupObj) => AfterParse(setupObj, Arg);
		protected abstract void AfterParse(TSetup setupObj, string? arg);


		public virtual void Process(TInfo infoObj) => Process(infoObj, Arg);
		protected abstract void Process(TInfo infoObj, string? arg);


		public virtual void PostProcess(TInfo infoObj) => PostProcess(infoObj, Arg);
		protected abstract void PostProcess(TInfo infoObj, string? arg);
	}


	/// <summary>
	/// A base implementation of an option that does not accept any arguments.
	/// </summary>
	/// <typeparam name="TSetup">The type of the setup object passed to 'AfterParse'</typeparam>
	/// <typeparam name="TInfo">The type of object passed to 'Process' and 'PostProcess'</typeparam>
	public abstract class BaseOptionNoArg<TSetup, TInfo> : BaseOption<TSetup, TInfo> where TInfo : IProcessObject {


		protected BaseOptionNoArg(IImmutableList<string> aliases,string description, bool hidden = false)
			: base(aliases, Arity.Zero, description, hidden) {}


		internal sealed override bool CanUseAsArg(string arg, out string? failReason) => throw InvalidFuncEx;
		private protected override string Arg => throw InvalidFuncEx;

		internal override string ArgDescription => throw InvalidFuncEx;


		public abstract override void AfterParse(TSetup setupObj);
		protected sealed override void AfterParse(TSetup setupObj, string? arg) => throw InvalidFuncEx;


		public abstract override void Process(TInfo infoObj);
		protected sealed override void Process(TInfo infoObj, string? arg) => throw InvalidFuncEx;


		public abstract override void PostProcess(TInfo infoObj);
		protected sealed override void PostProcess(TInfo infoObj, string? arg) => throw InvalidFuncEx;
	}


	/// <summary>
	/// A base implementation of an option that accepts an optional argument of a specific type.
	/// </summary>
	/// <typeparam name="TSetup">The type of the setup object passed to 'AfterParse'</typeparam>
	/// <typeparam name="TInfo">The type of object passed to 'Process' and 'PostProcess'</typeparam>
	/// <typeparam name="TArg">The type that this option takes as a parameter when converted from a string.</typeparam>
	public abstract class BaseOption<TSetup, TInfo, TArg> : BaseOption<TSetup, TInfo> where TInfo : IProcessObject {

		/// <summary>
		/// Converts a string to an argument of type 'TArg'; throws an exception if the conversion failed.
		/// </summary>
		/// <param name="strArg">The input argument as a string.</param>
		/// <returns>The input argument as a 'TArg' if the input argument was successfully converted.</returns>
		public delegate TArg ArgumentParser(string strArg);

		private protected override string Arg => throw InvalidFuncEx;

		public ArgumentParser ArgParser {get;}

		public string ArgName {get;}

		private readonly TArg _defaultArg;

		internal override string ArgDescription {
			get {
				if (typeof(TArg).IsEnum) {
					string s = $"{ArgName} {string.Join("|", typeof(TArg).GetEnumNames())}";
					if (Arity == Arity.One)
						return s;
					if (_defaultArg!.ToString() == "0") // not sure how else to check if we have a default
						return $"{s} (def=None)";
					return $"{s} (def='{_defaultArg}')";
				}
				if (Arity == Arity.One)
					return ArgName;
				return $"{ArgName} (def='{_defaultArg}')";
			}
		}


		// if arity is set to 1, default arg can be set to whatever
		protected BaseOption(
			IImmutableList<string> aliases,
			Arity arity, string description,
			string argName,
			ArgumentParser argParser,
			TArg defaultArg,
			bool hidden = false)
			: base(aliases, arity, description, hidden)
		{
			if (arity == Arity.Zero)
				throw new ArgProcessProgrammerException($"Arity cannot be {Arity.Zero} for typed BaseOption.");
			ArgParser = argParser;
			_defaultArg = defaultArg;
			ArgName = argName;
		}


		internal sealed override bool CanUseAsArg(string arg, out string? failReason) {
			try {
				ArgParser(arg);
				failReason = null;
				return true;
			} catch (ArgProcessUserException e) {
				failReason = e.Message;
				return false;
			}
		}


		// if arg is optional and doesn't exist this will pass the default arg to func, otherwise converts from string
		private void CheckArityAndCall<T>(Action<T, TArg, bool> func, T obj) {
			if (base.Arg == null) {
				if (Arity == Arity.One)
					throw new ArgProcessProgrammerException($"Argument is null but arity is set to {Arity.One} for option {Aliases[0]}");
				func(obj, _defaultArg, true);
			} else {
				func(obj, ArgParser(base.Arg!), false);
			}
		}


		protected abstract void AfterParse(TSetup setupObj, TArg arg, bool isDefault);
		public sealed override void AfterParse(TSetup setupObj) => CheckArityAndCall(AfterParse, setupObj);
		protected sealed override void AfterParse(TSetup setupObj, string? arg) => throw InvalidFuncEx;


		protected abstract void Process(TInfo infoObj, TArg arg, bool isDefault);
		public sealed override void Process(TInfo infoObj) => CheckArityAndCall(Process, infoObj);
		protected sealed override void Process(TInfo infoObj, string? arg) => throw InvalidFuncEx;


		protected abstract void PostProcess(TInfo infoObj, TArg arg, bool isDefault);
		public sealed override void PostProcess(TInfo infoObj) => CheckArityAndCall(PostProcess, infoObj);
		protected sealed override void PostProcess(TInfo infoObj, string? arg) => throw InvalidFuncEx;
	}


	public enum Arity {
		Zero,
		ZeroOrOne,
		One
	}
}
