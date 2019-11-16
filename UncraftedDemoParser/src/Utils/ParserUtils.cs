using System;
using System.Collections.Generic;
using System.Linq;
using UncraftedDemoParser.Parser;
using UncraftedDemoParser.Parser.Components;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Components.Packets;

namespace UncraftedDemoParser.Utils {
	
	public static class ParserUtils {
		
		
		// The packets are never null by default b/c if they were that would correspond to an unknown packet type,
		// which I wouldn't know how to skip over. Still skips over null packets in case user set a packet to null.
		public static List<T> FilteredForPacketType<T>(this SourceDemo sd) where T : DemoPacket {
			return sd.Frames.Where(frame => frame.DemoPacket?.GetType() == typeof(T))
				.Select(frame =>(T)frame.DemoPacket).ToList();
		}


		// here the message could be null by default due to unimplemented message type
		public static List<T> FilteredForNetMessageType<T>(this SourceDemo sd) where T : SvcNetMessage {
			return sd.FilteredForPacketType<Packet>().Where(packet => packet.SvcNetMessage?.GetType() == typeof(T))
				.Select(packet => (T)packet.SvcNetMessage).ToList();
		}


		// returns a dictionary that maps every svc packet to the amount of times that svc packet appears
		public static Dictionary<SvcMessageType, int> GetSvcMessageDict(this SourceDemo sd) {
			var messageCounter = new Dictionary<SvcMessageType, int>();
			foreach (Packet packet in sd.FilteredForPacketType<Packet>()) {
				SvcMessageType messageType = packet.MessageType;
				if (messageCounter.ContainsKey(messageType))
					messageCounter[messageType]++;
				else
					messageCounter.Add(messageType, 1);
			}
			return messageCounter;
		}


		public static List<PacketFrame> ControlPacketFrameList(this SourceDemo sd) {
			return sd.Frames.Where(frame =>
				frame.Type == PacketType.SignOn || frame.Type == PacketType.StringTables ||
				frame.Type == PacketType.Packet || frame.Type == PacketType.Stop).ToList();
		}


		public static int TickCount(this SourceDemo sd) {
			List<int> packetTicks = sd.FilteredForPacketType<Packet>().Select(packet => packet.Tick).Where(i => i >= 0).ToList();
			return packetTicks.Max() - packetTicks.Min() + 1; // accounts for 0th tick
		}


		public static void ParseForPacketTypes(this SourceDemo sd, params Type[] types) {
			if (types.Contains(typeof(DemoPacket))) {
				sd.Frames.ForEach(frame => frame.DemoPacket.TryParse(frame.Tick));
			} else {
				foreach (PacketFrame packetFrame in sd.Frames.Where(frame => types.Contains(frame.DemoPacket.GetType())))
					packetFrame.DemoPacket.TryParse(packetFrame.Tick);
			}
		}
	}
}