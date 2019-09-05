using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UncraftedDemoParser.DemoStructure;
using UncraftedDemoParser.DemoStructure.Packets;
using static UncraftedDemoParser.DemoStructure.Packets.Packet;

namespace UncraftedDemoParser.Utils {
	
	public static class Extensions {

		public static T[] SubArray<T>(this T[] data, int index, int length) {
			T[] result = new T[length];
			Array.Copy(data, index, result, 0, length);
			return result;
		}
		
		
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
		public static DemoComponent ToPacket(this PacketType packetType, byte[] data, SourceDemo demoRef) {
			switch (packetType) {
				case PacketType.SignOn:
					return new SignOn(data, demoRef);
				case PacketType.Packet:
					return new Packet(data, demoRef);
				case PacketType.SyncTick:
					return new SyncTick(data, demoRef);
				case PacketType.ConsoleCmd:
					return new ConsoleCmd(data, demoRef);
				case PacketType.UserCmd:
					return new UserCmd(data, demoRef);
				case PacketType.DataTables:
					return null;
				case PacketType.Stop:
					return null;
				case PacketType.CustomData:
					return new CustomData(data, demoRef);
				case PacketType.StringTables:
					return new StringTables(data, demoRef);
				default:
					throw new ArgumentOutOfRangeException(nameof(packetType), packetType, $"unknown packet type: {packetType}");
			}

			// more compact to use reflection but not tested and probably supa slow
			/*return (DemoComponent)Activator.CreateInstance(typeof(DemoComponent).Assembly.GetType(
				typeof(DemoComponent).Namespace + "." + packetType), data, demoRef);*/
		}


		public static SvcMessageType ToSvcMessageType(this byte b, bool newEngine) {
			if (!newEngine) {
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
					case 22:
					case 33:
						throw new ArgumentException($"unknown svc message type: {b}");
				}
			}
			return (SvcMessageType)b;
		}


		public static byte ToByte(this SvcMessageType svcMessage, bool newEngine) {
			if (!newEngine) {
				switch (svcMessage) {
					case SvcMessageType.NetTick:
						return 3;
					case SvcMessageType.NetStringCmd:
						return 4;
					case SvcMessageType.NetSetConVar:
						return 5;
					case SvcMessageType.NetSignOnState:
						return 6;
					case SvcMessageType.SvcPrint:
						return 7;
					case SvcMessageType.NetSplitScreenUser:
					case SvcMessageType.SvcSplitScreen:
					case SvcMessageType.SvcPaintmapData:
						throw new ArgumentException($"unknown svc message type: {svcMessage}");
				}
			}
			return (byte)svcMessage;
		}


		public static T RequireNonNull<T>(this T o) {
			if (o == null)
				throw new ArgumentException("something is null that isn't supposed to be");
			return o;
		}


		// removes count items that match the given predicate after or before the given index depending on backwardsSearch
		public static void RemoveItemsAfterIndex<T>(this List<T> list, Predicate<T> predicate, int index = 0, int count = 1, bool backwardsSearch = false) {
			if (backwardsSearch) {
				for (int i = index - 1; i >= 0 && count > 0; i--) {
					if (predicate.Invoke(list[i])) {
						list.RemoveAt(i);
						count--;
					}
				}
			} else {
				for (int i = index + 1; i < list.Count && count > 0; i++) {
					if (predicate.Invoke(list[i])) {
						list.RemoveAt(i);
						count--;
						i--;
					}
				}
			}
		}


		// convert byte array to binary as a string
		public static string AsBinStr(this byte[] bytes) {
			return String.Join(" ", 
				bytes.ToList()
					.Select(b => Convert.ToString(b, 2)).ToList()
					.Select(s => s.PadLeft(8, '0')));
		}


		public static string AsHexStr(this byte[] bytes) {
			return String.Join(" ",
				bytes.ToList()
					.Select(b => Convert.ToString(b, 16)).ToList()
					.Select(s => s.PadLeft(2, '0')));
		}


		public static void WriteToFiles(this string data, params string[] files) {
			files.ToList().ForEach(file => File.WriteAllText(file, data));
		}
	}
}