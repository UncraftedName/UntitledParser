using System;
using System.IO;
using UncraftedDemoParser.Parser;
using UncraftedDemoParser.Parser.Components.Packets;
using UncraftedDemoParser.Utils;

namespace CoordinateDumper {
	
	internal class Program {

		public static void Main(string[] args) {
			
			SourceDemo sd = new SourceDemo(new DirectoryInfo(args[0]), true);
			foreach (Packet packet in sd.FilterForPacketType<Packet>())
				Console.WriteLine($"{packet.Tick}|{String.Join(",", packet.PacketInfo[0].ViewOrigin)}|{String.Join(",", packet.PacketInfo[0].LocalViewAngles)}");
		}
	}
}