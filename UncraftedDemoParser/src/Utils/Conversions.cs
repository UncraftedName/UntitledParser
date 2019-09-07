using System;
using UncraftedDemoParser.DemoStructure;
using UncraftedDemoParser.DemoStructure.Packets;
using UncraftedDemoParser.DemoStructure.Packets.Abstract;

namespace UncraftedDemoParser.Utils {
	
	public static class Conversions {
		
		// gets the packet type associated with this byte value
		public static PacketType ToPacketType(this byte b, bool newEngine) {
			if (b > 0 && b < 8)
				return (PacketType)b;
			if (newEngine) {
				if (b == 8)
					return PacketType.CustomData;
				if (b == 9)
					return PacketType.StringTables;
			}
			if (b == 8)
				return PacketType.StringTables;
			throw new ArgumentException($"unknown packet type value: {b}");
		}


		// gets the byte value associated with this packet type
		public static byte ToByte(this PacketType packetType, bool newEngine) {
			byte byteVal = (byte)packetType;
			if (byteVal < 8)
				return byteVal;
			if (packetType == PacketType.StringTables)
				return (byte)(newEngine ? 9 : 8);
			if (newEngine && packetType == PacketType.CustomData)
				return 8;
			throw new ArgumentException($"unknown packet type: {packetType}");
		}
		

		// creates a new packet of the given packet type with the given data
		public static DemoPacket ToDemoPacket(this PacketType packetType, byte[] data, SourceDemo demoRef, int tick) {
			switch (packetType) {
				case PacketType.SignOn:
					return new SignOn(data, demoRef, tick);
				case PacketType.Packet:
					return new Packet(data, demoRef, tick);
				case PacketType.SyncTick:
					return new SyncTick(data, demoRef, tick);
				case PacketType.ConsoleCmd:
					return new ConsoleCmd(data, demoRef, tick);
				case PacketType.UserCmd:
					return new UserCmd(data, demoRef, tick);
				case PacketType.DataTables:
					return null;
				case PacketType.Stop:
					return new Stop(demoRef, tick);
				case PacketType.CustomData:
					return new CustomData(data, demoRef, tick);
				case PacketType.StringTables:
					return new StringTables(data, demoRef, tick);
				default:
					throw new ArgumentOutOfRangeException(nameof(packetType), packetType, $"unknown packet type: {packetType}");
			}
		}


		public static Packet.SvcMessageType ToSvcMessageType(this byte b, bool newEngine) {
			if (!newEngine) {
				switch (b) {
					case 3:
						return Packet.SvcMessageType.NetTick;
					case 4:
						return Packet.SvcMessageType.NetStringCmd;
					case 5:
						return Packet.SvcMessageType.NetSetConVar;
					case 6:
						return Packet.SvcMessageType.NetSignOnState;
					case 7:
						return Packet.SvcMessageType.SvcPrint;
					case 16:
					case 22:
					case 33:
						throw new ArgumentException($"unknown svc message type: {b}");
				}
			}
			return (Packet.SvcMessageType)b;
		}


		public static byte ToByte(this Packet.SvcMessageType svcMessage, bool newEngine) {
			if (!newEngine) {
				switch (svcMessage) {
					case Packet.SvcMessageType.NetTick:
						return 3;
					case Packet.SvcMessageType.NetStringCmd:
						return 4;
					case Packet.SvcMessageType.NetSetConVar:
						return 5;
					case Packet.SvcMessageType.NetSignOnState:
						return 6;
					case Packet.SvcMessageType.SvcPrint:
						return 7;
					case Packet.SvcMessageType.NetSplitScreenUser:
					case Packet.SvcMessageType.SvcSplitScreen:
					case Packet.SvcMessageType.SvcPaintmapData:
						throw new ArgumentException($"unknown svc message type: {svcMessage}");
				}
			}
			return (byte)svcMessage;
		}
	}
}