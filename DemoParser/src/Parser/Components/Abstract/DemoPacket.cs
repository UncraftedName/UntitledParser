using System;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Abstract {
	
	public abstract class DemoPacket : DemoComponent {
		
		public int Tick;


		protected DemoPacket(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader) {
			Tick = tick;
		}
		
		
		// gets the packet type associated with this byte value
		public static PacketType ByteToPacketType(SourceDemo demo, byte byteValue) {
			if (byteValue > 0 && byteValue < 8)
				return (PacketType)byteValue;
			if (demo.DemoSettings.NewEngine) {
				if (byteValue == 8)
					return PacketType.CustomData;
				if (byteValue == 9)
					return PacketType.StringTables;
			}
			if (byteValue == 8)
				return PacketType.StringTables;
			if (byteValue == 0) {
				if (demo.DemoSettings.Game == SourceDemoSettings.SourceGame.PORTAL_1_3420)
					return PacketType.StringTables;
			}
			string e = $"unknown packet type. Value: {byteValue}";
			demo.AddError(e);
			throw new ArgumentException(e, nameof(byteValue));
		}


		// gets the byte value associated with this packet type
		public static byte PacketTypeToByte(PacketType packetType, SourceDemoSettings demoSettings) {
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
			throw new ArgumentException($"unknown packet type: {packetType}", nameof(packetType));
		}
	}


	public static class PacketFactory {
		
		public static DemoPacket CreatePacket(SourceDemo demoRef, BitStreamReader reader, int tick, PacketType packetType) {
			return packetType switch {
				PacketType.SignOn 		=> (DemoPacket)new SignOn(demoRef, reader, tick),
				PacketType.Packet 		=> new Packet(demoRef, reader, tick),
				PacketType.SyncTick 	=> new SyncTick(demoRef, reader, tick),
				PacketType.ConsoleCmd 	=> new ConsoleCmd(demoRef, reader, tick),
				PacketType.UserCmd 		=> new UserCmd(demoRef, reader, tick),
				PacketType.DataTables 	=> new DataTables(demoRef, reader, tick),
				PacketType.Stop 		=> new Stop(demoRef, reader, tick),
				PacketType.CustomData 	=> new CustomData(demoRef, reader, tick),
				PacketType.StringTables => new StringTables(demoRef, reader, tick),
				_ => throw new NotImplementedException($"unknown or unsupported packet type: {packetType}")
			};
		}
	}
	
	
	public enum PacketType {
		SignOn = 1,
		Packet,
		SyncTick,
		ConsoleCmd,
		UserCmd,
		DataTables,
		Stop,
		CustomData,
		StringTables
	}
}