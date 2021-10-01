using System;
using System.Collections.Immutable;
using System.Diagnostics;

namespace ConsoleApp.ConsoleParsing {
	
	// do not inherit from this, use one of the classes below
	public abstract class BaseOption<TSetup, TInfo> {
		
		public IImmutableList<string> Aliases {get;}

		public Arity Arity {get;}

		public string Description {get;}

		/// <summary>
		/// When set, specifies that the "--help" option should not show this option.
		/// </summary>
		public bool Hidden {get;}

		/// <summary>
		/// Specifies whether or not to execute the 'Process' and 'PostProcess' methods. 
		/// </summary>
		public bool Enabled {get;protected set;}


		public BaseOption(IImmutableList<string> aliases, Arity arity, string description, bool hidden) {
			Aliases = aliases;
			Arity = arity;
			Description = description;
			Hidden = hidden;
		}


		public void Enable(TSetup setupObj, string? arg) {
			AfterParse(setupObj, arg);
			Enabled = true;
		}

		
		public abstract bool CanUseAsArg(string arg);
		
		
		/// <summary>
		/// Provides a way to reset the state of this object if anything was saved during processing.
		/// </summary>
		public virtual void Reset() {}
		

		public abstract void AfterParse(TSetup setupObj, string? arg);
		

		public abstract void Process(TInfo infoObj, string? arg);
		

		public abstract void PostProcess(TInfo infoObj, string? arg);
	}


	/// <summary>
	/// A base implementation of an option that does not accept any arguments.
	/// </summary>
	/// <typeparam name="TSetup">The type of the setup object passed to 'AfterParse'</typeparam>
	/// <typeparam name="TInfo">The type of object passed to 'Process' and 'PostProcess'</typeparam>
	public abstract class BaseOptionNoArg<TSetup, TInfo> : BaseOption<TSetup, TInfo> {
		
		protected BaseOptionNoArg(IImmutableList<string> aliases,string description, bool hidden = false)
			: base(aliases, Arity.Zero, description, hidden) {}


		public override bool CanUseAsArg(string arg) => false;


		public abstract void AfterParse(TSetup setupObj);
		public override void AfterParse(TSetup setupObj, string? arg) {
			Debug.Assert(arg == null);
			AfterParse(setupObj);
		}
		
		public abstract void Process(TInfo infoObj);
		public override void Process(TInfo infoObj, string? arg) {
			Debug.Assert(arg == null);
			Process(infoObj);
		}

		public abstract void PostProcess(TInfo infoObj);
		public override void PostProcess(TInfo infoObj, string? arg) {
			Debug.Assert(arg == null);
			PostProcess(infoObj);
		}
	}


	/// <summary>
	/// A base implementation of an option that accepts an optional argument of a specific type.
	/// </summary>
	/// <typeparam name="TSetup">The type of the setup object passed to 'AfterParse'</typeparam>
	/// <typeparam name="TInfo">The type of object passed to 'Process' and 'PostProcess'</typeparam>
	/// <typeparam name="TArg">The type that this option takes as a parameter when converted from a string.</typeparam>
	public abstract class BaseOption<TSetup, TInfo, TArg> : BaseOption<TSetup, TInfo> {

		/// <summary>
		/// Converts a string to an argument of type 'TArg'; throws an exception if the conversion failed.
		/// </summary>
		/// <param name="strArg">The input argument as a string.</param>
		/// <returns>The input argument as a 'TArg' if the input argument was successfully converted.</returns>
		public delegate TArg ArgumentParser(string strArg);


		public ArgumentParser ArgParser {get;}


		protected BaseOption(
			IImmutableList<string> aliases,
			Arity arity, string description,
			ArgumentParser argParser,
			bool hidden = false)
			: base(aliases, arity, description, hidden)
		{
			if (arity == Arity.Zero)
				throw new Exception("Arity cannot be 0 for typed BaseOption.");
			ArgParser = argParser;
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


		public abstract void AfterParse(TSetup setupObj, TArg arg);
		public override void AfterParse(TSetup setupObj, string? arg) => AfterParse(setupObj, ArgParser(arg!));

		public abstract void Process(TInfo infoObj, TArg arg);
		public override void Process(TInfo infoObj, string? arg) => Process(infoObj, ArgParser(arg!));

		public abstract void PostProcess(TInfo infoObj, TArg arg);
		public override void PostProcess(TInfo infoObj, string? arg) => PostProcess(infoObj, ArgParser(arg!));
	}


	public enum Arity {
		Zero,
		ZeroOrOne,
		One
	}
}
