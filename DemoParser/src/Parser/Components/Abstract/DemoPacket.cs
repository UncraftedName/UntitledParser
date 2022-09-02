using System;
using System.Collections.Generic;
using DemoParser.Parser.Components.Packets;
using DemoParser.Utils;

namespace DemoParser.Parser.Components.Abstract {

	public abstract class DemoPacket : DemoComponent {

		public readonly PacketFrame FrameRef;
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

		public static readonly IReadOnlyList<PacketType> Portal15135Table = new[] {
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
		public static PacketType ByteToPacketType(DemoInfo demoInfo, byte b) {
			var tab = demoInfo.PacketTypes;
			if (tab == null) {
				return PacketType.Unknown;
			} else if (b >= tab.Count) {
				// HACK for 3740. Of course valve didn't change the net protocol, so this
				// is the first place I could think of to differentiate the versions.
				if (demoInfo.Game == SourceGame.PORTAL_1_3420 && b == 8) {
					demoInfo.Game = SourceGame.PORTAL_1_3740;
					demoInfo.PacketTypes = Portal15135Table;
					demoInfo.PacketTypesReverseLookup = Portal15135Table.CreateReverseLookupDict(PacketType.Invalid);
					return ByteToPacketType(demoInfo, b);
				}
				return PacketType.Invalid;
			} else {
				return tab[b];
			}
		}


		// gets the byte value associated with this packet type
		public static byte PacketTypeToByte(DemoInfo demoInfo, PacketType p) {
			if (demoInfo.PacketTypesReverseLookup.TryGetValue(p, out int i))
				return (byte)i;
			throw new ArgumentException($"no packet id found for {p}");
		}
	}


	public static class PacketFactory {

		public static DemoPacket CreatePacket(SourceDemo dRef, PacketFrame fRef, PacketType packetType) {
			return packetType switch {
				PacketType.SignOn       => new SignOn      (dRef, fRef),
				PacketType.Packet       => new Packet      (dRef, fRef),
				PacketType.SyncTick     => new SyncTick    (dRef, fRef),
				PacketType.ConsoleCmd   => new ConsoleCmd  (dRef, fRef),
				PacketType.UserCmd      => new UserCmd     (dRef, fRef),
				PacketType.DataTables   => new DataTables  (dRef, fRef),
				PacketType.Stop         => new Stop        (dRef, fRef),
				PacketType.StringTables => new StringTables(dRef, fRef),
				PacketType.CustomData   => new CustomData  (dRef, fRef),
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
