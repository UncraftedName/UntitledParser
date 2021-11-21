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

		private BitStreamReader _privateReader;
		public override BitStreamReader Reader => _privateReader.FromBeginning();
		
		public readonly string? FileName;
		public new DemoInfo DemoInfo;
		public DemoHeader Header;
		public List<PacketFrame> Frames;
		private bool _exceptionDuringParsing;
		// initialized to null, these are set in the packet packet and from console commands
		public int? StartTick, EndTick, StartAdjustmentTick, EndAdjustmentTick;
		internal uint ClientSoundSequence; // increases with each reliable sound
		
		// Helper classes, these are used by the demo components, are temporary and might be created/destroyed whenever.
		// Any classes that require the use of any lookup tables e.g. string tables, game event list, etc. should store
		// a local copy of any objects from those tables.
		public List<string> ErrorList;
		internal GameEventManager GameEventManager;
		public DataTableParser? DataTableParser;
		internal CurStringTablesManager CurStringTablesManager;
		internal CurEntitySnapshot? CurEntitySnapshot;
		internal CurBaseLines? CBaseLines;
		private readonly IProgress<double>? _parseProgress;

		
		public SourceDemo(FileInfo info, IProgress<double>? parseProgress = null) 
			: this(info.FullName, parseProgress) {}
		
		
		public SourceDemo(string fileDir, IProgress<double>? parseProgress = null) 
			: this(File.ReadAllBytes(fileDir), parseProgress, Path.GetFileName(fileDir)) {}


		public SourceDemo(byte[] data, IProgress<double>? parseProgress = null, string demoName = "") 
			: base(null)
		{
			_parseProgress = parseProgress;
			FileName = demoName;
			_privateReader = new BitStreamReader(data);
		}


		protected override void Parse(ref BitStreamReader bsr) {
			// make sure we set the demo settings first
			Header = new DemoHeader(this);
			Header.ParseStream(ref bsr);
			DemoInfo = new DemoInfo(this);
			// it might be worth it to implement updating helper classes with listeners, but it's not a huge deal atm
			CurStringTablesManager = new CurStringTablesManager(this); 
			ErrorList = new List<string>();
			Frames = new List<PacketFrame>();
			StartTick = 0;
			try {
				do {
					Frames.Add(new PacketFrame(this));
					Frames[^1].ParseStream(ref bsr);
					_parseProgress?.Report((double)bsr.CurrentBitIndex / bsr.BitLength);
				} while (Frames[^1].Type != PacketType.Stop && bsr.BitsRemaining >= 24); // would be 32 but the last byte is often cut off
				StartAdjustmentTick ??= 0;
				EndTick = this.FilterForPacket<Packet>().Select(packet => packet.Tick).Where(i => i >= 0).Max();
			}
			catch (Exception e) {
				_exceptionDuringParsing = true;
				Debug.WriteLine($"Exception after parsing {Frames.Count - 1} packets");
				LogError($"Exception after parsing {Frames.Count - 1} packets: {e.Message}");
				throw;
			}
			EndAdjustmentTick ??= EndTick;
			DemoInfo.DemoParseResult |= DemoParseResult.Success;
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
			_privateReader.CurrentBitIndex = 0;
			Parse(ref _privateReader);
		}


		public override void PrettyWrite(IPrettyWriter iw) {
			iw.AppendLine("Untitled parser by UncraftedName, build date: " +
						  $"{BuildDateAttribute.GetBuildDate(Assembly.GetExecutingAssembly()):R}");
			if (FileName != null)
				iw.AppendLine($"file name: {FileName}");
			iw.Append($"predicted game: {DemoInfo.Game}\n\n");
			
			Header.PrettyWrite(iw);
			foreach (PacketFrame frame in Frames) {
				iw.Append("\n\n");
				frame.PrettyWrite(iw);
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

		
		public void WriteVerboseString(Stream stream, string indentStr = "\t", bool disposeAfterWriting = false) {
			PrettyStreamWriter iw = new PrettyStreamWriter(stream, indentStr: indentStr, bufferSize: 100000);
			PrettyWrite(iw);
			if (disposeAfterWriting) {
				iw.Flush();
				iw.Dispose();
			}
		}
		
		
		internal BitStreamReader ReaderFromOffset(int offset, int bitLength) {
			return new BitStreamReader(_privateReader.Data, bitLength, offset);
		}
		

		// todo iterate over ent snapshots
	}
}