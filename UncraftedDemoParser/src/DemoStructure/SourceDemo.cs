using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UncraftedDemoParser.DemoStructure.Packets;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure {
	
	// has the same structure as DemoComponent - not a subclass because 'this' is passed as a ref to all DemoComponents
	public class SourceDemo {
		
		public Header Header;
		public List<PacketFrame> Frames;
		public SourceDemoSettings DemoSettings;
		public byte[] Bytes {get;protected set;}


		public SourceDemo(DirectoryInfo directoryInfo, bool parse = true) : this(File.ReadAllBytes(directoryInfo.FullName), parse) {}
		
		
		public SourceDemo(byte[] data, bool parse = true) {
			Bytes = data;
			if (parse)
				ParseBytes();
		}


		// get all packets, but only parse the consolecmd packet; used solely for printing the custom parser output
		public void QuickParse() {
			Header = new Header(Bytes.SubArray(0, 1072), this); // header auto-parses
			DemoSettings = new SourceDemoSettings(Header);
			Frames = new List<PacketFrame>();
			int index = 1072;
			do {
				Frames.Add(new PacketFrame(Bytes, ref index, this));
			} while (index < Bytes.Length);
			this.FilterForPacketType<ConsoleCmd>().ForEach(cmd => cmd.ParseBytes());
		}


		public void ParseBytes() { // header auto-parses
			QuickParse();
			Frames.ForEach(frame => frame.ParseBytes());
		}
		
		
		public void UpdateBytes() {
			Header.UpdateBytes();
			Frames.ForEach(delegate(PacketFrame p) {p.UpdateBytes();});
			List<byte> tmpBytes = new List<byte>(838 + Frames.Sum(frame => frame.Bytes.Length));
			tmpBytes.AddRange(Header.Bytes);
			Frames.ForEach(delegate(PacketFrame p) {tmpBytes.AddRange(p.Bytes);});
			Bytes = tmpBytes.ToArray();
		}


		public string AsVerboseString() {
			StringBuilder output = new StringBuilder(300 * Frames.Count);
			output.AppendLine(Header.ToString());
			output.AppendLine();
			
			Frames.ForEach(frame => {
				output.Append(frame);
				if (frame.Type != PacketType.Stop)
					output.AppendLine();
			});
			return output.ToString();
		}
	}
}