using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Packets;

namespace DemoParser.Parser.Components.Abstract {
	
	public abstract class DemoPacket : DemoComponent {

		public PacketFrame FrameRef;
		public int Tick => FrameRef.Tick;

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
		
		protected DemoPacket(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef) {
			FrameRef = frameRef;
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
		
		public static DemoPacket CreatePacket(SourceDemo demoRef, PacketFrame frameRef, PacketType packetType) {
			return packetType switch {
				PacketType.SignOn       => new SignOn(demoRef, frameRef),
				PacketType.Packet       => new Packet(demoRef, frameRef),
				PacketType.SyncTick     => new SyncTick(demoRef, frameRef),
				PacketType.ConsoleCmd   => new ConsoleCmd(demoRef, frameRef),
				PacketType.UserCmd      => new UserCmd(demoRef, frameRef),
				PacketType.DataTables   => new DataTables(demoRef, frameRef),
				PacketType.Stop         => new Stop(demoRef, frameRef),
				PacketType.StringTables => new StringTables(demoRef, frameRef),
				PacketType.CustomData   => new CustomData(demoRef, frameRef),
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