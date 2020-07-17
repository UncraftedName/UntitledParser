using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;
using static DemoParser.Parser.SourceGame;

namespace DemoParser.Parser.Components.Abstract {
	
	public abstract class DemoPacket : DemoComponent {
		
		public int Tick;

		#region lookup tables
		
		private static readonly PacketType[] Portal3420Table = {
			PacketType.StringTables,
			PacketType.SignOn,
			PacketType.Packet,
			PacketType.SyncTick,
			PacketType.ConsoleCmd,
			PacketType.UserCmd,
			PacketType.DataTables,
			PacketType.Stop
		};
		private static readonly Dictionary<PacketType, int> Portal3420ReverseTable = Portal3420Table.CreateReverseLookupDict();
		
		private static readonly PacketType[] Portal1UnpackTable = {
			PacketType.Invalid,
			
			PacketType.SignOn,
			PacketType.Packet,
			PacketType.SyncTick,
			PacketType.ConsoleCmd,
			PacketType.UserCmd,
			PacketType.DataTables,
			PacketType.Stop,
			PacketType.StringTables
		};
		private static readonly Dictionary<PacketType, int> Portal1UnpackReverseTable = Portal1UnpackTable.CreateReverseLookupDict();
		
		private static readonly PacketType[] DemoProtocol4Table = {
			PacketType.Invalid,
			
			PacketType.SignOn,
			PacketType.Packet,
			PacketType.SyncTick,
			PacketType.ConsoleCmd,
			PacketType.UserCmd,
			PacketType.DataTables,
			PacketType.Stop,
			PacketType.CustomData,
			PacketType.StringTables
		};
		private static readonly Dictionary<PacketType, int> DemoProtocol4ReverseTable = DemoProtocol4Table.CreateReverseLookupDict();
		
		#endregion
		
		protected DemoPacket(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader) {
			Tick = tick;
		}
		
		
		// gets the packet type associated with this byte value
		public static PacketType ByteToPacketType(DemoSettings demoSettings, byte b) {
			var lookupTable = demoSettings.Game switch {
				PORTAL_1_3420      => Portal3420Table,
				PORTAL_1_UNPACK    => Portal1UnpackTable,
				PORTAL_1_STEAMPIPE => Portal1UnpackTable,
				PORTAL_2           => DemoProtocol4Table,
				L4D2_2000          => DemoProtocol4Table,
				L4D2_2042          => DemoProtocol4Table,
				_ => null
			};
			if (lookupTable == null)
				return PacketType.Unknown;
			else if (b >= lookupTable.Length)
				return PacketType.Invalid;
			else
				return lookupTable[b];
		}
		
		
		// gets the byte value associated with this packet type
		public static byte PacketTypeToByte(DemoSettings demoSettings, PacketType p) {
			var reverseLookupTable = demoSettings.Game switch {
				PORTAL_1_3420      => Portal3420ReverseTable,
				PORTAL_1_UNPACK    => Portal1UnpackReverseTable,
				PORTAL_1_STEAMPIPE => Portal1UnpackReverseTable,
				PORTAL_2           => DemoProtocol4ReverseTable,
				L4D2_2000          => DemoProtocol4ReverseTable,
				L4D2_2042          => DemoProtocol4ReverseTable,
				_ => throw new ArgumentException($"no reverse packet lookup table for {demoSettings.Game}")
			};
			return (byte)reverseLookupTable[p];
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
		// just used by me
		Unknown,
		Invalid,
		
		SignOn,
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