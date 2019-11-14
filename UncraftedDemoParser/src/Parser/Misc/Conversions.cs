using System;
using UncraftedDemoParser.Parser.Components.Abstract;
using UncraftedDemoParser.Parser.Components.Packets;
using UncraftedDemoParser.Parser.Components.SvcNetMessages;

namespace UncraftedDemoParser.Parser.Misc {
	
	public static class Conversions {
		
		// gets the packet type associated with this byte value
		public static PacketType ToPacketType(this byte b, SourceDemoSettings demoSettings) {
			if (b > 0 && b < 8)
				return (PacketType)b;
			if (demoSettings.NewEngine) {
				if (b == 8)
					return PacketType.CustomData;
				if (b == 9)
					return PacketType.StringTables;
			}
			if (b == 8)
				return PacketType.StringTables;
			if (b == 0 && demoSettings.Game == SourceDemoSettings.SourceGame.PORTAL_1_3420)
				return PacketType.StringTables;
			throw new FailedToParseException($"This demo has an unknown packet type. Value: {b}");
		}


		// gets the byte value associated with this packet type
		public static byte ToByte(this PacketType packetType, SourceDemoSettings demoSettings) {
			if (packetType == PacketType.StringTables &&
				demoSettings.Game == SourceDemoSettings.SourceGame.PORTAL_1_3420)
				return 0;
			byte byteVal = (byte)packetType;
			if (byteVal < 8)
				return byteVal;
			if (packetType == PacketType.StringTables)
				return (byte)(demoSettings.NewEngine ? 9 : 8);
			if (demoSettings.NewEngine && packetType == PacketType.CustomData)
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
					return new DataTables(data, demoRef, tick);
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


		public static SvcMessageType ToSvcMessageType(this byte b, SourceDemoSettings demoSettings) {
			if (!demoSettings.NewEngine) {
				switch (b) {
					case 3:
						return SvcMessageType.NetTick;
					case 4:
						return SvcMessageType.NetStringCmd;
					case 5:
						return SvcMessageType.NetSetConVar;
					case 6:
						return SvcMessageType.NetSignOnState;
					case 7:
						return SvcMessageType.SvcPrint;
					case 16:
						if (demoSettings.Game == SourceDemoSettings.SourceGame.PORTAL_1_STEAMPIPE)
							return SvcMessageType.NetSignOnState;
						return SvcMessageType.Unknown;
					case 22:
					case 33:
						return SvcMessageType.Unknown; // this prevents throwing more exceptions if paired with a check for null from ToSvcNetMessage()
				}
			}
			// might add 3420 & leak support, but for now my memes will stay dreams
			return (SvcMessageType)b;
		}


		public static byte ToByte(this SvcMessageType messageType, SourceDemoSettings demoSettings) {
			if (!demoSettings.NewEngine) {
				switch (messageType) {
					case SvcMessageType.NetTick:
						return 3;
					case SvcMessageType.NetStringCmd:
						return 4;
					case SvcMessageType.NetSetConVar:
						return 5;
					case SvcMessageType.NetSignOnState:
						if (demoSettings.Game == SourceDemoSettings.SourceGame.PORTAL_1_STEAMPIPE)
							return 16;
						return 6;
					case SvcMessageType.SvcPrint:
						return 7;
					case SvcMessageType.NetSplitScreenUser:
					case SvcMessageType.SvcSplitScreen:
					case SvcMessageType.SvcPaintmapData:
						throw new ArgumentException($"unknown svc message type: {messageType}");
				}
			}
			return (byte)messageType;
		}


		public static SvcNetMessage ToSvcNetMessage(this SvcMessageType messageType, byte[] data, SourceDemo demoRef, int tick) {
			switch (messageType) {
				case SvcMessageType.NetNop:
					return new NetNop(data, demoRef, tick);
				case SvcMessageType.NetSignOnState:
					return new NetSignOnState(data, demoRef, tick);
				case SvcMessageType.NetTick:
					return new NetTick(data, demoRef, tick);
				case SvcMessageType.SvcCmdKeyValues:
					return new SvcCmdKeyValues(data, demoRef, tick);
				case SvcMessageType.SvcGameEvent:
					return new SvcGameEvent(data, demoRef, tick);
				case SvcMessageType.SvcPrefetch:
					return new SvcPrefetch(data, demoRef, tick);
				case SvcMessageType.SvcSetPause:
					return new SvcSetPause(data, demoRef, tick);
				case SvcMessageType.SvcSetView:
					return new SvcSetView(data, demoRef, tick);
				case SvcMessageType.SvcSounds:
					return new SvcSounds(data, demoRef, tick);
				case SvcMessageType.SvcTempEntities:
					return new SvcTempEntities(data, demoRef, tick);
				case SvcMessageType.SvcUserMessage:
					return new SvcUserMessage(data, demoRef, tick);
				case SvcMessageType.NetStringCmd:
					return new NetStringCmd(data, demoRef, tick);
				case SvcMessageType.SvcBspDecal:
					return new SvcBspDecal(data, demoRef, tick);
				default:
					return null;
			}
		}
	}
}