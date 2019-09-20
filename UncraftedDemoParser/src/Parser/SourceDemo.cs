using System.Collections.Generic;
using System.IO;
using System.Linq;
using UncraftedDemoParser.Parser.Components;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Components.Packets;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.Parser {
	
	// has the same structure as DemoComponent - not a subclass because 'this' is passed as a ref to all DemoComponents
	public class SourceDemo {
		
		public Header Header;
		public List<PacketFrame> Frames;
		public SourceDemoSettings DemoSettings;
		public byte[] Bytes {get;protected set;}
		public string Name;


		public SourceDemo(DirectoryInfo directoryInfo, bool parse = true):
			this(File.ReadAllBytes(directoryInfo.FullName), parse, directoryInfo.Name) {}
		
		
		public SourceDemo(byte[] data, bool parse = true, string name = "") {
			Name = name;
			Bytes = data;
			if (parse)
				ParseBytes();
		}


		// get all packets, but only parse the consolecmd packet; used solely for printing the listdemo output
		public void QuickParse() {
			Header = (Header)new Header(Bytes.SubArray(0, 1072), this).TryParse();
			DemoSettings = new SourceDemoSettings(Header);
			Frames = new List<PacketFrame>();
			int index = 1072;
			do {
				Frames.Add(new PacketFrame(Bytes, ref index, this));
			} while (index < Bytes.Length);
			this.FilterForPacketType<ConsoleCmd>().ForEach(cmd => cmd.TryParse());
		}


		public void ParseBytes() { // header auto-parses
			QuickParse();
			Frames.ForEach(frame => frame.TryParse());
		}
		
		
		public void UpdateBytes() {
			Header.UpdateBytes();
			Frames.ForEach(p => p.UpdateBytes());
			List<byte> tmpBytes = new List<byte>(838 + Frames.Sum(frame => frame.Bytes.Length));
			tmpBytes.AddRange(Header.Bytes);
			Frames.ForEach(p => tmpBytes.AddRange(p.Bytes));
			Bytes = tmpBytes.ToArray();
		}
	}
}