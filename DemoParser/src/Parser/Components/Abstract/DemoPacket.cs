using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Packets;

namespace DemoParser.Parser.Components.Abstract {
	
	public abstract class DemoPacket : DemoComponent {
		
		public int Tick;

		#region lookup tables
		
		public static readonly IReadOnlyList<PacketType> Portal3420Table = new[] {
			PacketType.StringTables,
			PacketType.SignOn,
			PacketType.Packet,
			PacketType.SyncTick,
			PacketType.ConsoleCmd,
			PacketType.UserCmd,
			PacketType.DataTables,
			PacketType.Stop
		};
		
		public static readonly IReadOnlyList<PacketType> Portal1UnpackTable = new[] {
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
		
		public static readonly IReadOnlyList<PacketType> DemoProtocol4Table = new[] {
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
		
		#endregion
		
		protected DemoPacket(SourceDemo? demoRef, int tick) : base(demoRef) {
			Tick = tick;
		}
		
		
		// gets the packet type associated with this byte value
		public static PacketType ByteToPacketType(DemoSettings demoSettings, byte b) {
			var tab = demoSettings.PacketTypes;
			if (tab == null)
				return PacketType.Unknown;
			else if (b >= tab.Count)
				return PacketType.Invalid;
			else
				return tab[b];
		}
		
		
		// gets the byte value associated with this packet type
		public static byte PacketTypeToByte(DemoSettings demoSettings, PacketType p) {
			if (demoSettings.PacketTypesReverseLookup.TryGetValue(p, out int i))
				return (byte)i;
			throw new ArgumentException($"no packet id found for {p}");
		}
	}
	
	
	public static class PacketFactory {
		
		public static DemoPacket CreatePacket(SourceDemo demoRef, int tick, PacketType packetType) {
			return packetType switch {
				PacketType.SignOn       => new SignOn(demoRef, tick),
				PacketType.Packet       => new Packet(demoRef, tick),
				PacketType.SyncTick     => new SyncTick(demoRef, tick),
				PacketType.ConsoleCmd   => new ConsoleCmd(demoRef, tick),
				PacketType.UserCmd      => new UserCmd(demoRef, tick),
				PacketType.DataTables   => new DataTables(demoRef, tick),
				PacketType.Stop         => new Stop(demoRef, tick),
				PacketType.StringTables => new StringTables(demoRef, tick),
				PacketType.CustomData   => new CustomData(demoRef, tick),
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