#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UntitledParser.Parser.Components;
using UntitledParser.Parser.Components.Abstract;
using UntitledParser.Parser.HelperClasses;
using UntitledParser.Utils;

namespace UntitledParser.Parser {
	
	public class SourceDemo : DemoComponent {

		public readonly string? FileName;
		public SourceDemoSettings DemoSettings;
		public DemoHeader Header;
		public List<PacketFrame> Frames;
		private bool _exceptionDuringParsing;
		public List<string> ErrorList;
		
		// Helper classes, these are used by the demo components, are temporary and might be created/destroyed whenever.
		// Any classes that require the use of any lookup tables e.g. string tables, game event list, etc. should store
		// a local copy of any objects from those tables.
		internal GameEventManager GameEventManager;
		internal DataTableParser DataTableParser;
		internal C_StringTablesManager CStringTablesManager;
		private readonly IProgress<double>? _parseProgress;

		
		public SourceDemo(FileInfo info, IProgress<double> parseProgress = null) : this(info.FullName, parseProgress) {}
		
		
		public SourceDemo(string fileDir, IProgress<double> parseProgress = null) : this(File.ReadAllBytes(fileDir), parseProgress) {
			FileName = Path.GetFileName(fileDir);
		}


		public SourceDemo(byte[] data, IProgress<double> parseProgress = null) : base(null, new BitStreamReader(data)) {
			_parseProgress = parseProgress;
		}


		internal override void ParseStream(BitStreamReader bsr) {
			// it might be worth it to implement updating helper classes with listeners, but it's not a huge deal atm
			CStringTablesManager = new C_StringTablesManager(this); 
			ErrorList = new List<string>();
			Header = new DemoHeader(DemoRef, bsr);
			Header.ParseStream(bsr);
			DemoSettings = new SourceDemoSettings(Header);
			Frames = new List<PacketFrame>();
			try {
				do {
					Frames.Add(new PacketFrame(this, bsr));
					Frames[^1].ParseStream(bsr);
					_parseProgress?.Report((double)bsr.CurrentBitIndex / bsr.BitLength);
				} while (Frames[^1].Type != PacketType.Stop && bsr.BitsRemaining >= 24);
			}
			catch (Exception e) {
				_exceptionDuringParsing = true;
				Debug.WriteLine($"Exception after parsing {Frames.Count - 1} packets");
				AddError($"Exception after parsing {Frames.Count - 1} packets{e.Message}");
				throw;
			}
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal void AddError(string e) {
			string s = Frames[^1].Packet == null ? "[unknown]" : $"[{Frames[^1].Tick}] {e}";
			ErrorList.Add(s);
			Debug.WriteLine(s);
		}


		public void Parse() {
			ParseStream(Reader);
		}


		internal override void AppendToWriter(IndentedWriter iw) {
			if (FileName != null)
				iw.AppendLine($"file name: {FileName}");
			iw.Append($"Untitled parser by UncraftedName, version: {GetType().Assembly.GetName().Version}\n\n");
			
			Header.AppendToWriter(iw);
			foreach (PacketFrame frame in Frames) {
				iw.Append("\n\n");
				frame.AppendToWriter(iw);
			}
			if (_exceptionDuringParsing)
				iw.AppendLine("\n\nAn unhandled exception occured while parsing this demo, the rest could not be parsed...");
			if (ErrorList.Count > 0) {
				iw.AppendLine("\n\nList of errors while parsing: ");
				foreach (string s in ErrorList)
					iw.AppendLine(s);
			}
		}
	}
}