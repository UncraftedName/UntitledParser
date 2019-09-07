using System.Collections.Generic;
using System.Linq;
using UncraftedDemoParser.DemoStructure;
using UncraftedDemoParser.DemoStructure.Packets;
using UncraftedDemoParser.DemoStructure.Packets.Abstract;

namespace UncraftedDemoParser.Utils {
	
	public static class ParserUtils {
		
		// filters for the given packet type
		public static List<T> FilterForPacketType<T>(this SourceDemo sd) where T : DemoPacket {
			return sd.Frames.Where(frame => frame.DemoPacket.GetType() == typeof(T))
				.Select(frame =>(T)frame.DemoPacket).ToList();
		}


		// returns a dictionary that maps every svc packet to the amount of times that svc packet appears
		public static Dictionary<Packet.SvcMessageType, int> GetSvcMessageDict(this SourceDemo sd) {
			var messageCounter = new Dictionary<Packet.SvcMessageType, int>();
			foreach (Packet packet in sd.FilterForPacketType<Packet>()) {
				Packet.SvcMessageType messageType = packet.MessageType;
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


		public static int Length(this SourceDemo sd) {
			List<int> packetTicks = sd.FilterForPacketType<Packet>().Select(packet => packet.Tick).Where(i => i >= 0).ToList();
			return packetTicks.Max() - packetTicks.Min() + 1;
		}
	}
}