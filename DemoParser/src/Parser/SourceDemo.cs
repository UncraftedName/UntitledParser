#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DemoParser.Parser.Components;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser {

	/// <summary>
	/// Once parsed, contains an in-memory representation of a demo file.
	/// </summary>
	public class SourceDemo : PrettyClass {

		private BitStreamReader _bsr;
		public BitStreamReader Reader => _bsr.FromBeginning();

		public readonly string? FileName;
		public DemoInfo DemoInfo;
		public DemoHeader Header;
		public List<PacketFrame> Frames;
		public int? StartTick, EndTick, StartAdjustmentTick, EndAdjustmentTick;
		public GameState.GameState State;
		public List<string> ErrorList;

		private readonly IProgress<double>? _parseProgress;
		private PacketFrame _lastFrame;

		// a way to tell what info did/didn't get parsed
		public DemoParseResult DemoParseResult;


		public SourceDemo(string fileDir, IProgress<double>? parseProgress = null)
			: this(new FileInfo(fileDir), parseProgress) {}


		public SourceDemo(FileInfo file, IProgress<double>? parseProgress = null) {
			if (file.Length > BitStreamReader.MaxDataSize) {
				DemoParseResult |= DemoParseResult.DataTooLong | DemoParseResult.ReaderOverflowed;
			} else {
				_parseProgress = parseProgress;
				FileName = file.Name;
				_bsr = new BitStreamReader(File.ReadAllBytes(file.FullName));
			}
		}


		public SourceDemo(byte[] data, IProgress<double>? parseProgress = null, string demoName = "") {
			if (data.Length > BitStreamReader.MaxDataSize) {
				DemoParseResult |= DemoParseResult.DataTooLong | DemoParseResult.ReaderOverflowed;
			} else {
				_parseProgress = parseProgress;
				FileName = demoName;
				_bsr = new BitStreamReader(data);
			}
		}


		private void Reset() {
			Frames = new List<PacketFrame>();
			_lastFrame = null!;
			DemoParseResult = default;
			StartTick = EndTick = StartAdjustmentTick = EndAdjustmentTick = null;
			Header = null!;
			DemoInfo = null!;
			ErrorList = new List<string>();
		}


		public void Parse() {
			_bsr.CurrentBitIndex = 0;
			if ((DemoParseResult & DemoParseResult.DataTooLong) != 0)
				goto end;
			Reset();
			Header = new DemoHeader(this);
			Header.ParseStream(ref _bsr);
			if (_bsr.HasOverflowed)
				goto end;
			DemoInfo = new DemoInfo(this);
			State = new GameState.GameState(this);
			StartTick = 0;

			do {
				_lastFrame = new PacketFrame(this);
				_lastFrame.ParseStream(ref _bsr);
				if (_bsr.HasOverflowed)
					goto end;
				Frames.Add(_lastFrame);
				_parseProgress?.Report((double)_bsr.CurrentBitIndex / _bsr.BitLength);
			} while (_lastFrame.Type != PacketType.Stop && _bsr.BitsRemaining >= 24); // would be 32 but the last byte is often cut off

			StartAdjustmentTick ??= 0;
			// filter for packet explicitly to ignore SignOn packets
			EndTick = this.FilterForPacket<Packet>().Where(p => p.GetType() == typeof(Packet)).Select(p => p.Tick).Where(i => i >= 0).Max();
			EndAdjustmentTick ??= EndTick;
			DemoParseResult |= DemoParseResult.Success;
			end:
			if (_bsr.HasOverflowed)
				DemoParseResult |= DemoParseResult.ReaderOverflowed;
		}


		internal void LogError(string e) {
			string s = $"[{_lastFrame.Tick}] {e}";
			ErrorList.Add(s);
			Debug.WriteLine(s);
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine("Generated by Untitled parser");
			if (FileName != null)
				pw.AppendLine($"file name: {FileName}");
			pw.Append($"predicted game: {DemoInfo.Game}\n\n");

			Header.PrettyWrite(pw);
			foreach (PacketFrame frame in Frames) {
				pw.Append("\n\n");
				frame.PrettyWrite(pw);
			}
			if ((DemoParseResult & DemoParseResult.Success) == 0)
				pw.AppendLine("\n\nMore data remaining, parsing failed...");
			if (ErrorList.Count > 0) {
				pw.AppendLine("\n\nList of errors while parsing: ");
				foreach (string s in ErrorList)
					pw.AppendLine(s);
			} else {
				pw.AppendLine();
			}
		}


		public override string ToString() {
			return $"{FileName}, {Reader.BitLength / 8} bytes long";
		}
	}


	// flags that get set during parsing, gives a way for other projects to figure out how much data we got, very WIP
	[Flags]
	public enum DemoParseResult {
		Success           = 1,
		EntParsingEnabled = 1 << 1,
		EntParsingFailed  = 1 << 2,
		UnknownGame       = 1 << 3,
		DataTooLong       = 1 << 4,
		ReaderOverflowed  = 1 << 5,
	}
}
