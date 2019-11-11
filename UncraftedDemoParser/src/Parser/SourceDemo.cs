using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UncraftedDemoParser.Parser.Components;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Components.Packets;
using UncraftedDemoParser.Parser.Misc;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser {
	
	// has the same structure as DemoComponent - not a subclass because 'this' is passed as a ref to all DemoComponents
	public class SourceDemo {
		
		public Header Header;
		public List<PacketFrame> Frames;
		public SourceDemoSettings DemoSettings;
		public byte[] Bytes {get;protected set;}
		public string Name;
		private bool _fullParseCompleted = false; // you can't call updateBytes() until a full parse has been done


		public SourceDemo(string filePath, bool parse = true) :
			this(new DirectoryInfo(filePath), parse) {}


		public SourceDemo(DirectoryInfo directoryInfo, bool parse = true) :
			this(File.ReadAllBytes(directoryInfo.FullName), parse, directoryInfo.Name) {}


		public SourceDemo(byte[] data, bool parse = true, string name = "") {
			Name = name;
			Bytes = data;
			if (parse)
				ParseBytes();
		}


		// there's gonna be a bit of overhead to reparse the header but for now it's probably worth it to see what the errors are
		public void ParseHeader() {
			Header = (Header)new Header(Bytes.SubArray(0, 1072), this).TryParse();
		}


		// get all packets, but only parse the consolecmd packet; used solely for printing the listdemo output
		public void QuickParse() { // todo: remove parsing the consolecmd packet
			Header = (Header)new Header(Bytes.SubArray(0, 1072), this).TryParse();
			DemoSettings = new SourceDemoSettings(Header);
			Frames = new List<PacketFrame>();
			int index = 1072;
			try {
				do {
					Frames.Add(new PacketFrame(Bytes, ref index, this));
				} while (index < Bytes.Length);
			} catch (FailedToParseException e) {
				Console.WriteLine($"{e.Message}; frames so far: {Frames.Count}");
				throw;
			} catch (Exception e) {
				Debug.WriteLine($"Failed to quick parse, index: {index}; frames traversed: {Frames.Count}");
				Debug.WriteLine(e.ToString());
				throw new FailedToParseException("quick parse failed, this is probably not a playable demo");
			}
		}


		public void ParsePacketTypes<T>() where T : DemoPacket {
			if (typeof(T) == typeof(DemoPacket))
				Frames.ForEach(frame => frame.DemoPacket.TryParse(frame.Tick));
			else
				this.FilteredForPacketType<T>().ForEach(packet => packet.TryParse(packet.Tick));
		}


		public void ParseBytes() { // header auto-parses
			QuickParse();
			Frames.ForEach(frame => frame.TryParse());
			_fullParseCompleted = true;
		}
		
		
		public void UpdateBytes() {
			if (!_fullParseCompleted)
				throw new ApplicationException("Can't update bytes until a full parse has been done.");
			Header.UpdateBytes();
			Frames.ForEach(p => p.UpdateBytes());
			List<byte> tmpBytes = new List<byte>(838 + Frames.Sum(frame => frame.Bytes.Length));
			tmpBytes.AddRange(Header.Bytes);
			Frames.ForEach(p => tmpBytes.AddRange(p.Bytes));
			Bytes = tmpBytes.ToArray();
		}
	}
}