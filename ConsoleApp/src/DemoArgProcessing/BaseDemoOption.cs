using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using ConsoleApp.GenericArgProcessing;
using DemoParser.Parser;

namespace ConsoleApp.DemoArgProcessing {
	
	/// <summary>
	/// Represents a base option for demo parsing that takes no arguments.
	/// </summary>
	public abstract class DemoOption : BaseOptionNoArg<DemoParsingSetupInfo, DemoParsingInfo> {
		protected DemoOption(IImmutableList<string> aliases, string description, bool hidden = false)
			: base(aliases, description, hidden) {}
	}
	
	
	/// <summary>
	/// Represents a base option for demo parsing that takes an optional or mandatory argument of a specific type.
	/// If Arity is set to 1, any value can be passed to defaultArg.
	/// </summary>
	/// <typeparam name="TArg">The type of the argument.</typeparam>
	public abstract class DemoOption<TArg> : BaseOption<DemoParsingSetupInfo, DemoParsingInfo, TArg> {
		protected DemoOption(IImmutableList<string> aliases, Arity arity, string description, string argName,
			ArgumentParser argParser, TArg defaultArg, bool hidden = false)
			: base(aliases, arity, description, argName, argParser, defaultArg, hidden) {}
	}


	/// <summary>
	/// A class used to set up various values during arg parsing.
	/// </summary>
	public class DemoParsingSetupInfo {
		public int ExecutableOptions {get;set;} // some options don't do anything on their own, like -f
		public bool ShouldSaveAllDemos {get;set;}
		public bool ShouldSearchForDemosRecursively {get;set;}
		public string? FolderOutput {get;set;}
		public bool FolderOutputRequired {get;set;}
	}


	/// <summary>
	/// A class that holds information used during option processing.
	/// </summary>
	public class DemoParsingInfo : IProcessObject, IDisposable {

		public DemoParsingSetupInfo SetupInfo {get;}
		public SourceDemo CurrentDemo => _demos[^1];
		public bool FailedLastParse {get;private set;}
		public bool OptionOutputRedirected => SetupInfo.FolderOutput != null;
		public int NumDemos {get;}

		private readonly List<SourceDemo> _demos;
		private readonly IParallelDemoParser _pdp;
		private TextWriter? _textWriter;
		private BinaryWriter? _binaryWriter;


		public DemoParsingInfo(DemoParsingSetupInfo setupInfo, IImmutableList<(FileInfo demoPath, string displayPath)> paths) {
			SetupInfo = setupInfo;
			_demos = new List<SourceDemo>(setupInfo.ShouldSaveAllDemos ? paths.Count : 1);
			_pdp = new ThreadPoolDemoParser(paths);
			NumDemos = paths.Count;
			// we're sort of in an invalid state until Advance is called
		}


		public bool CanAdvance() => _pdp.HasNext();


		public void Advance() {
			DisposeWriters();
			if (!SetupInfo.ShouldSaveAllDemos)
				_demos.Clear();
			Debug.Assert(_pdp.HasNext());
			// wait for the next demo
			Console.Write($"Parsing {Utils.QuoteIfHasSpaces(_pdp.GetCurrentParseInfo().DisplayPath)}... ");
			IProgressBar progressBar = _pdp.GetCurrentParseInfo().ProgressBar;
			if (!_pdp.NextReady())
				progressBar.StartShowing();
			(var demo, Exception? exception) = _pdp.GetNext();
			_demos.Add(demo);
			progressBar.Dispose();
			if (exception == null) {
				Utils.WriteColor("done.\n", ConsoleColor.Green);
			} else {
				Utils.PushForegroundColor(ConsoleColor.Red);
				Console.WriteLine("failed.");
				Console.WriteLine(exception.Message);
				Utils.PopForegroundColor();
			}
			FailedLastParse = exception != null;
		}


		public void DoneProcessing() {
			DisposeWriters();
			if (!SetupInfo.ShouldSaveAllDemos)
				_demos.Clear();
		}


		public void Dispose() {
			DisposeWriters();
			_pdp.Dispose();
		}


		/// <summary>
		/// Creates and returns a new text writer only if the folder output option is set, otherwise returns
		/// Console.Out. If you want an option to never print to console, set FolderOutputRequired in the setup object.
		/// </summary>
		public TextWriter InitTextWriter(string message, string suffix, string extension = ".txt", int bufferSize = 4096) {
			if (SetupInfo.ExecutableOptions > 1)
				Console.WriteLine(message);
			if (SetupInfo.FolderOutput == null)
				return Console.Out;
			DisposeWriters();
			return _textWriter = new StreamWriter(CreateFileStream(suffix, extension), Encoding.UTF8, bufferSize);
		}


		/// <summary>
		/// Creates and returns a new Binary writer.
		/// </summary>
		public BinaryWriter InitBinaryWriter(string message, string suffix, string extension) {
			if (SetupInfo.FolderOutput == null)
				throw new ArgProcessProgrammerException("Folder output not set but option is requesting a binary writer.");
			if (SetupInfo.ExecutableOptions > 1)
				Console.WriteLine(message);
			DisposeWriters();
			return _binaryWriter = new BinaryWriter(CreateFileStream(suffix, extension));
		}


		private FileStream CreateFileStream(string suffix, string extension) {
			Directory.CreateDirectory(SetupInfo.FolderOutput!);
			return new FileStream(
				Path.Combine(SetupInfo.FolderOutput!, $"{CurrentDemo.FileName![..^4]}--{suffix}{extension}"),
				FileMode.Create);
		}


		private void DisposeWriters() {
			_textWriter?.Dispose();
			_textWriter = null;
			_binaryWriter?.Dispose();
			_binaryWriter = null;
		}
	}
}
