using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;

namespace ConsoleApp.DemoSpecificArgProcessing {
	
	/// <summary>
	/// Represents a base option for demo parsing that takes no arguments.
	/// </summary>
	public abstract class DemoOption : BaseOptionNoArg<DemoParsingSetupInfo, DemoParsingInfo> {
		protected DemoOption(IImmutableList<string> aliases, string description, bool hidden = false)
			: base(aliases, description, hidden) {}
	}
	
	
	/// <summary>
	/// Represents a base option for demo parsing that takes an optional or mandatory argument of a specific type.
	/// </summary>
	/// <typeparam name="TArg">The type of the argument.</typeparam>
	public abstract class DemoOption<TArg> : BaseOption<DemoParsingSetupInfo, DemoParsingInfo, TArg> {
		protected DemoOption(IImmutableList<string> aliases, Arity arity, string description, ArgumentParser argParser, TArg defaultArg, bool hidden = false)
			: base(aliases, arity, description, argParser, defaultArg, hidden) {}
	}


	/// <summary>
	/// A class used to set up various values during arg parsing.
	/// </summary>
	public class DemoParsingSetupInfo {
		public int RunnableOptions {get;set;} // TODO rename this
		public bool ShouldSaveDemos {get;set;}
	}


	/// <summary>
	/// A class that holds information used during option processing.
	/// </summary>
	public class DemoParsingInfo : IProcessObject {

		public SourceDemo CurrentDemo => _demos[^1];
		public bool FailedLastParse {get;private set;}

		private readonly DemoParsingSetupInfo _setupInfo;
		private readonly List<SourceDemo> _demos;
		private readonly IParallelDemoParser _parallelDemoParser;


		public DemoParsingInfo(DemoParsingSetupInfo setupInfo, IReadOnlyList<(FileInfo demoPath, string displayPath)> paths) {
			_setupInfo = setupInfo;
			_demos = new List<SourceDemo>(setupInfo.ShouldSaveDemos ? 1 : paths.Count);
			_parallelDemoParser = new ThreadPoolDemoParser(paths);
			// we're in an invalid state until Advance is called
		}


		public bool CanAdvance() {
			return _parallelDemoParser.HasNext();
		}


		public void Advance() {
			if (!_setupInfo.ShouldSaveDemos)
				_demos.Clear();
			WaitForNextDemo();
		}


		public void DoneProcessing() {
			if (!_setupInfo.ShouldSaveDemos)
				_demos.Clear();
		}


		private void WaitForNextDemo() {
			Debug.Assert(_parallelDemoParser.HasNext());
			Console.WriteLine($"Parsing {_parallelDemoParser.GetCurrentParseInfo().DisplayPath}... ");
			IProgressBar progressBar = _parallelDemoParser.GetCurrentParseInfo().ProgressBar;
			progressBar.StartShowing();
			(var demo, Exception? exception) = _parallelDemoParser.GetNext();
			_demos.Add(demo);
			progressBar.Dispose();
			if (exception == null) {
				Utils.WriteColor("done.", ConsoleColor.Green);
			} else {
				Utils.PushForegroundColor(ConsoleColor.Red);
				Console.WriteLine("failed.");
				Console.WriteLine(exception.Message);
				Utils.PopForegroundColor();
			}
			FailedLastParse = exception != null;
		}
	}
}
