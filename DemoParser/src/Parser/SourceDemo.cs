#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets;
using DemoParser.Parser.HelperClasses;
using DemoParser.Parser.HelperClasses.EntityStuff;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser {
	
	/// <summary>
	/// Once parsed, contains an in-memory representation of a demo file.
	/// </summary>
	public class SourceDemo : DemoComponent {

		public readonly string? FileName;
		public new DemoSettings DemoSettings;
		public DemoHeader Header;
		public List<PacketFrame> Frames;
		private bool _exceptionDuringParsing;
		// these are set in the packet packet and from console commands
		public int StartTick = -1, EndTick = -1, StartAdjustmentTick = -1, EndAdjustmentTick = -1;
		internal uint ClientSoundSequence; // increases with each reliable sound
		
		// Helper classes, these are used by the demo components, are temporary and might be created/destroyed whenever.
		// Any classes that require the use of any lookup tables e.g. string tables, game event list, etc. should store
		// a local copy of any objects from those tables.
		public List<string> ErrorList;
		internal GameEventManager GameEventManager;
		public DataTableParser DataTableParser;
		internal CurStringTablesManager CurStringTablesManager;
		internal CurEntitySnapshot? CurEntitySnapshot;
		internal CurBaseLines? CBaseLines;
		private readonly IProgress<double>? _parseProgress;

		
		public SourceDemo(FileInfo info, IProgress<double>? parseProgress = null) 
			: this(info.FullName, parseProgress) {}
		
		
		public SourceDemo(string fileDir, IProgress<double>? parseProgress = null) 
			: this(File.ReadAllBytes(fileDir), parseProgress, Path.GetFileName(fileDir)) {}


		public SourceDemo(byte[] data, IProgress<double>? parseProgress = null, string demoName = "") 
			: base(null!, new BitStreamReader(data))
		{
			_parseProgress = parseProgress;
			FileName = demoName;
		}


		internal override void ParseStream(BitStreamReader bsr) {
			// make sure we set the demo settings first
			Header = new DemoHeader(DemoRef, bsr);
			Header.ParseStream(bsr);
			DemoSettings = new DemoSettings(Header);
			// it might be worth it to implement updating helper classes with listeners, but it's not a huge deal atm
			CurStringTablesManager = new CurStringTablesManager(this); 
			ErrorList = new List<string>();
			Frames = new List<PacketFrame>();
			StartTick = 0;
			try {
				do {
					Frames.Add(new PacketFrame(this, bsr));
					Frames[^1].ParseStream(bsr);
					_parseProgress?.Report((double)bsr.CurrentBitIndex / bsr.BitLength);
				} while (Frames[^1].Type != PacketType.Stop && bsr.BitsRemaining >= 24); // would be 32 but the last byte is often cut off
				
				StartAdjustmentTick = StartAdjustmentTick == -1 ? 0 : StartAdjustmentTick;
				EndTick = this.FilterForPacket<Packet>().Select(packet => packet.Tick).Where(i => i >= 0).Max();
			}
			catch (Exception e) {
				_exceptionDuringParsing = true;
				Debug.WriteLine($"Exception after parsing {Frames.Count - 1} packets");
				LogError($"Exception after parsing {Frames.Count - 1} packets: {e.Message}");
				throw;
			}
			EndAdjustmentTick = EndAdjustmentTick == -1 ? this.TickCount() - 1 : EndAdjustmentTick;
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		internal void LogError(string e) {
			string s = Frames[^1].Packet == null ? "[unknown]" : $"[{Frames[^1].Tick}] {e}";
			ErrorList.Add(s);
			Debug.WriteLine(s);
		}


		public void Parse() {
			ParseStream(Reader);
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.AppendLine("Untitled parser by UncraftedName, build date: " +
						  $"{BuildDateAttribute.GetBuildDate(Assembly.GetExecutingAssembly()):R}");
			if (FileName != null)
				iw.AppendLine($"file name: {FileName}");
			iw.Append($"predicted game: {DemoSettings.Game}\n\n");
			
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
			} else {
				iw.AppendLine();
			}
		}


		public override string ToString() {
			return $"{FileName}, {Reader.BitLength / 8} bytes long";
		}

		
		// close the writer yourself
		public void WriteVerboseString(TextWriter textWriter, string indentStr = "\t") {
			IndentedWriter iw = new IndentedWriter();
			AppendToWriter(iw);
			iw.WriteLines(textWriter, indentStr);
		}
		

		// todo iterate over ent snapshots
	}
}