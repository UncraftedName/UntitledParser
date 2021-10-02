using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace ConsoleApp.GenericArgProcessing {
	
	// do not inherit from this, use one of the classes below
	public abstract class BaseOption<TSetup, TInfo> where TInfo : IProcessObject {
		
		public IImmutableList<string> Aliases {get;}

		public Arity Arity {get;}

		public string Description {get;}

		/// <summary>
		/// When set, specifies that "--help" should not show this option.
		/// </summary>
		public bool Hidden {get;}

		/// <summary>
		/// Specifies whether or not to execute the 'AfterParse', 'Process', and 'PostProcess' methods. 
		/// </summary>
		public bool Enabled {get;private set;}

		private string? _arg;


		protected BaseOption(IImmutableList<string> aliases, Arity arity, string description, bool hidden) {
			if (aliases.Count == 0)
				throw new Exception($"Option of type '{GetType().Name}' must have at least one alias.");
			Aliases = aliases;
			Arity = arity;
			Description = description;
			Hidden = hidden;
		}


		public void Enable(TSetup setupObj, string? arg) {
			_arg = arg;
			Enabled = true;
		}

		
		public abstract bool CanUseAsArg(string arg);


		/// <summary>
		/// Provides a way to reset the state of this object if anything was saved during processing.
		/// </summary>
		public virtual void Reset() {
			Enabled = false;
			_arg = null;
		}
		

		public virtual void AfterParse(TSetup setupObj) => AfterParse(setupObj, _arg);
		protected abstract void AfterParse(TSetup setupObj, string? arg);
		

		public virtual void Process(TInfo infoObj) => Process(infoObj, _arg);
		protected abstract void Process(TInfo infoObj, string? arg);
		

		public virtual void PostProcess(TInfo infoObj) => PostProcess(infoObj, _arg);
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


		public override bool CanUseAsArg(string arg) => false;


		public abstract override void AfterParse(TSetup setupObj);
		protected override void AfterParse(TSetup setupObj, string? arg) => throw new InvalidOperationException();


		public abstract override void Process(TInfo infoObj);
		protected override void Process(TInfo infoObj, string? arg) => throw new InvalidOperationException();


		public abstract override void PostProcess(TInfo infoObj);
		protected override void PostProcess(TInfo infoObj, string? arg) => throw new InvalidOperationException();
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


		public ArgumentParser ArgParser {get;}

		private readonly TArg _defaultArg;


		protected BaseOption(
			IImmutableList<string> aliases,
			Arity arity, string description,
			ArgumentParser argParser,
			TArg defaultArg,
			bool hidden = false)
			: base(aliases, arity, description, hidden)
		{
			if (arity == Arity.Zero)
				throw new Exception("Arity cannot be 0 for typed BaseOption.");
			ArgParser = argParser;
			_defaultArg = defaultArg;
		}


		public override bool CanUseAsArg(string arg) {
			try {
				ArgParser(arg);
				return true;
			} catch (Exception e) {
				Debug.WriteLine($"Failed to convert 'arg' to an argument for '{GetType().Name}', message: {e}");
				return false;
			}
		}


		// if arg is optional and doesn't exist this will pass the default arg to func, otherwise converts from string
		private void CheckArityAndCall<T>(Action<T, TArg, bool> func, T obj, string? arg) {
			if (arg == null) {
				if (Arity == Arity.One)
					throw new ArgProcessException($"Argument is null but arity is set to {Arity.One} for option {Aliases[0]}");
				func(obj, _defaultArg, true);
			} else {
				func(obj, ArgParser(arg!), false);
			}
		}


		protected abstract void AfterParse(TSetup setupObj, TArg arg, bool isDefault);
		protected override void AfterParse(TSetup setupObj, string? arg) => CheckArityAndCall(AfterParse, setupObj, arg);
		
		
		protected abstract void Process(TInfo infoObj, TArg arg, bool isDefault);
		protected override void Process(TInfo infoObj, string? arg) => CheckArityAndCall(Process, infoObj, arg);

		
		protected abstract void PostProcess(TInfo infoObj, TArg arg, bool isDefault);
		protected override void PostProcess(TInfo infoObj, string? arg) => CheckArityAndCall(PostProcess, infoObj, arg);
	}


	public enum Arity {
		Zero,
		ZeroOrOne,
		One
	}
}
