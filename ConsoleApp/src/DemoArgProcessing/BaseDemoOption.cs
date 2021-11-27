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
		// The number of options that perform a task. Pretty much every option should increment this. The output folder
		// option is an example of an option that does not do anything on its own.
		public int ExecutableOptions {get;set;}
		public bool ShouldSaveAllDemos {get;set;}
		public bool ShouldSearchForDemosRecursively {get;set;} // set by recursive option
		public string? FolderOutput {get;set;} // set by folder output option
		// options that dump large amounts of text or write to special binary files must set this flag
		public bool FolderOutputRequired {get;set;}
		public bool EditsDemos {get;set;} // options that create new demos must set this flag
		public bool OverWriteDemos {get;set;} // set by overwrite option
		public bool OverwritePrompt {get;set;} = true; // set by overwrite option
	}


	/// <summary>
	/// A class that holds information used during option processing.
	/// </summary>
	public class DemoParsingInfo : IProcessObject, IDisposable {

		public DemoParsingSetupInfo SetupInfo {get;}
		// this allows us to chain multiple overwrites
		public SourceDemo CurrentDemo => _curOptionOverwriting ? _overwriteProgress : _demos[^1];
		public bool FailedLastParse {get;private set;}
		public bool OptionOutputRedirected => SetupInfo.FolderOutput != null;
		public int NumDemos {get;}

		private readonly List<SourceDemo> _demos;
		private readonly IParallelDemoParser _pdp;
		private TextWriter? _textWriter;
		private Stream? _binaryStream;
		
		// keep track of the overwritten demo in memory, don't write it to disk until we're doing with the current process loop
		private SourceDemo _overwriteProgress = null!;
		private int _overwriteCount; // how many times have we overwritten this demo?
		private FileInfo _fileInfo = null!; // full path to current demo
		private bool _curOptionOverwriting;
		// public bool CancelOverwrite {get;set;} // TODO


		public DemoParsingInfo(DemoParsingSetupInfo setupInfo, IImmutableList<(FileInfo demoPath, string displayPath)> paths) {
			SetupInfo = setupInfo;
			_demos = new List<SourceDemo>(setupInfo.ShouldSaveAllDemos ? paths.Count : 1);
			_pdp = new ThreadPoolDemoParser(paths);
			NumDemos = paths.Count;
			_overwriteCount = 0;
			_curOptionOverwriting = false;
			// CancelOverwrite = false;
			// we're sort of in an invalid state until Advance is called
		}


		public bool CanAdvance() => _pdp.HasNext();


		public void DoneWithOption() {
			if (SetupInfo.OverWriteDemos && _binaryStream != null) {
				// try parsing the edited version, if it succeeds then it'll get passed to any other edit options
				try {
					SourceDemo demo = new SourceDemo(((MemoryStream)_binaryStream!).ToArray());
					demo.Parse();
					_overwriteProgress = demo;
					_overwriteCount++;
				} catch (Exception) {
					Console.WriteLine("Editing demo failed, will not overwrite.");
					_overwriteProgress = CurrentDemo;
				}
			}
			_curOptionOverwriting = false;
		}


		private void CheckDemoOverwrite() {
			if (SetupInfo.OverWriteDemos && _overwriteCount > 0) {
				// we've gotten to this point with no exceptions, now we can overwrite the demo
				Console.Write("Overwriting demo... ");
				try {
					File.WriteAllBytes(_fileInfo.FullName, _overwriteProgress.Reader.Data);
					Utils.WriteColor("done.\n", ConsoleColor.Green);
				} catch (Exception e) {
					Utils.WriteColor($"failed: {e.Message}.\n", ConsoleColor.Red);
				}
			}
		}


		public void Advance() {
			CheckDemoOverwrite();
			DisposeWriters();
			if (!SetupInfo.ShouldSaveAllDemos)
				_demos.Clear();
			Debug.Assert(_pdp.HasNext());
			// wait for the next demo
			Console.Write($"Parsing {Utils.QuoteIfHasSpaces(_pdp.GetCurrentParseInfo().DisplayPath)}... ");
			IProgressBar progressBar = _pdp.GetCurrentParseInfo().ProgressBar;
			if (!_pdp.NextReady())
				progressBar.StartShowing();
			(SourceDemo demo, FileInfo fileInfo, Exception? exception) = _pdp.GetNext();
			_demos.Add(demo);
			_fileInfo = fileInfo; // save the full file info in case we are overwriting
			_overwriteProgress = demo;
			_overwriteCount = 0;
			_curOptionOverwriting = false;
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
			CheckDemoOverwrite();
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
		public TextWriter StartWritingText(string message, string suffix, string extension = ".txt", int bufferSize = 4096) {
			if (SetupInfo.ExecutableOptions > 1)
				Console.WriteLine($"{message}...");
			if (SetupInfo.FolderOutput == null)
				return Console.Out;
			DisposeWriters();
			return _textWriter = new StreamWriter(CreateFileStream(suffix, extension), Encoding.UTF8, bufferSize);
		}


		/// <summary>
		/// Similar to InitTextWriter, creates and returns a new Stream. Also used as a way to detect if we're
		/// overwriting a demo with the current option - do not access CurrentDemo before calling this.
		/// </summary>
		public Stream StartWritingBytes(string message, string suffix, string extension) {
			if (SetupInfo.FolderOutput == null && !SetupInfo.EditsDemos)
				throw new ArgProcessProgrammerException("Folder output not set but option is requesting a binary stream.");
			if (SetupInfo.ExecutableOptions > 1)
				Console.WriteLine($"{message}...");
			DisposeWriters();
			// if we're overwriting, write to a memory stream and don't overwrite the file we're done with the current process loop
			if (SetupInfo.OverWriteDemos && extension == ".dem") {
				_curOptionOverwriting = true;
				return _binaryStream = new MemoryStream(_overwriteProgress.Reader.ByteLength);
			}
			// don't think you need to flush the writer, just the file stream
			return _binaryStream = new BinaryWriter(CreateFileStream(suffix, extension)).BaseStream;
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
			_binaryStream?.Dispose();
			_binaryStream = null;
		}
	}
}
