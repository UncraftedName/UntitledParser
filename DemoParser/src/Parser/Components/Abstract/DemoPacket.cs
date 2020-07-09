using System;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils.BitStreams;
using TypeReMapper = DemoParser.Utils.TwoWayDict<byte, DemoParser.Parser.Components.Abstract.PacketType>;

namespace DemoParser.Parser.Components.Abstract {
	
	public abstract class DemoPacket : DemoComponent {
		
		public int Tick;
		// special cases, todo do the same thing as user messages
		private static readonly TypeReMapper Portal3420Mapper = new TypeReMapper {
			[(byte)0] = PacketType.StringTables
		};
		private static readonly TypeReMapper DemoProtocol4Mapper = new TypeReMapper {
			[8] = PacketType.CustomData,
			[9] = PacketType.StringTables
		};
		

		protected DemoPacket(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader) {
			Tick = tick;
		}
		
		
		// gets the packet type associated with this byte value
		public static PacketType ByteToPacketType(DemoSettings demoSettings, byte byteValue) {
			var def = (PacketType)byteValue;
			if (demoSettings.NewDemoProtocol)
				return DemoProtocol4Mapper.GetValueOrDefault(byteValue, def);
			else if (demoSettings.Game == SourceGame.PORTAL_1_3420)
				return Portal3420Mapper.GetValueOrDefault(byteValue, def);
			return def;
		}


		// gets the byte value associated with this packet type
		public static byte PacketTypeToByte(DemoSettings demoSettings, PacketType packetType) {
			var def = (byte)packetType;
			if (demoSettings.NewDemoProtocol)
				return DemoProtocol4Mapper.GetValueOrDefault(packetType, def);
			else if (demoSettings.Game == SourceGame.PORTAL_1_3420)
				return Portal3420Mapper.GetValueOrDefault(packetType, def);
			return def;
		}
	}


	public static class PacketFactory {
		
		public static DemoPacket CreatePacket(SourceDemo demoRef, BitStreamReader reader, int tick, PacketType packetType) {
			return packetType switch {
				PacketType.SignOn       => new SignOn(demoRef, reader, tick),
				PacketType.Packet       => new Packet(demoRef, reader, tick),
				PacketType.SyncTick     => new SyncTick(demoRef, reader, tick),
				PacketType.ConsoleCmd   => new ConsoleCmd(demoRef, reader, tick),
				PacketType.UserCmd      => new UserCmd(demoRef, reader, tick),
				PacketType.DataTables   => new DataTables(demoRef, reader, tick),
				PacketType.Stop         => new Stop(demoRef, reader, tick),
				PacketType.StringTables => new StringTables(demoRef, reader, tick),
				PacketType.CustomData   => new CustomData(demoRef, reader, tick),
				_ => throw new NotSupportedException($"unknown or unsupported packet type: {packetType}")
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
		StringTables,
		CustomData 
	}
}