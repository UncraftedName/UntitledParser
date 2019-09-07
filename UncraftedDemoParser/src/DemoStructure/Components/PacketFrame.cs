using System;
using System.Text;
using UncraftedDemoParser.DemoStructure.Components.Abstract;
using UncraftedDemoParser.DemoStructure.Components.Packets;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure.Components {
	
	
	// Holds information such as packet type and what tick the packet occured on (stored in the packet)
	// Also determines the size of the packet at the pointer of the byte array passed to its constructor,
	// and automatically increments the pointer to point to the next frame.
	public class PacketFrame : DemoComponent {

		private byte _typeValue;

		public PacketType Type {
			get => _typeValue.ToPacketType(DemoRef.DemoSettings.NewEngine);
			set => _typeValue = value.ToByte(DemoRef.DemoSettings.NewEngine);
		}

		// in the demo the tick is stored as part of the packet frame, 
		// but for convenience in the parser it is part of the demo packet
		public int Tick {
			get => DemoPacket.Tick;
			set => DemoPacket.Tick = value;
		}

		public byte? AlignmentByte; // new engine only
		public DemoPacket DemoPacket;


		// pointer is the current index in the byte array, is automatically updated to go to the next frame
		public PacketFrame(byte[] data, ref int pointer, SourceDemo demoRef) : base(data, demoRef) {
			int originalPointer = pointer; // used to measure length of sub array to extract
			_typeValue = data[pointer];
			
			if (Type == PacketType.Stop && !demoRef.DemoSettings.NewEngine) { // last byte is cut off
				DemoPacket = new Stop(demoRef, demoRef.Header.TickCount); // stop packet tick = header tick
				pointer = data.Length; // put the pointer to the very end
			} else {
				int tick = GetIntAtPointer(data, pointer + 1); // passed to the packet

				int currentPointer = pointer + 5;
				if (DemoRef.DemoSettings.HasAlignmentByte) {
					AlignmentByte = data[currentPointer];
					currentPointer++;
				}

				int packetLength;

				switch (Type) {
					case PacketType.SignOn:
						packetLength = demoRef.DemoSettings.Header.SignOnLength;
						break;
					case PacketType.Packet:
						packetLength = demoRef.DemoSettings.MaxSplitscreenPlayers * 76 + 12; // length of the main chunk of the packet
						packetLength += GetIntAtPointer(data, currentPointer + packetLength - 4); // length of the additional data in the packet
						break;
					case PacketType.SyncTick:
						throw new ArgumentException("Idk how to parse the sync tick packet");
					case PacketType.ConsoleCmd:
						packetLength = GetIntAtPointer(data, currentPointer) + 4;
						break;
					case PacketType.UserCmd:
						packetLength = GetIntAtPointer(data, currentPointer + 4) + 8;
						break;
					case PacketType.DataTables:
						throw new ArgumentException("Idk how to parse the data tables packet");
					case PacketType.CustomData:
						packetLength = GetIntAtPointer(data, currentPointer + 4) + 8;
						break;
					case PacketType.StringTables:
						packetLength = GetIntAtPointer(data, currentPointer) + 4;
						break;
					case PacketType.Stop:
						packetLength = 0;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				DemoPacket = Type.ToDemoPacket(data.SubArray(currentPointer, packetLength), demoRef, tick).RequireNonNull();
				pointer = currentPointer + packetLength;
			}
			Bytes = data.SubArray(originalPointer, pointer - originalPointer);
		}


		public override void ParseBytes() {
			DemoPacket.ParseBytes();
		}

		public override void UpdateBytes() { // no specialness for the stop packet, has no effect on demo playback
			DemoPacket.UpdateBytes();
			BitFieldWriter bfw = new BitFieldWriter(5 + (AlignmentByte.HasValue ? 1 : 0) + DemoPacket.Bytes.Length);
			bfw.WriteByte(_typeValue);
			bfw.WriteInt(DemoPacket.Tick);
			if (AlignmentByte.HasValue)
				bfw.WriteByte(AlignmentByte.Value);
			bfw.WriteBytes(DemoPacket.Bytes);
			Bytes = bfw.Data;
		}


		private int GetIntAtPointer(byte[] data, int pointer) {
			byte[] intBytes = data.SubArray(pointer, 4);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intBytes);
			return BitConverter.ToInt32(intBytes, 0);
		}


		public override string ToString() {
			StringBuilder output = new StringBuilder();
			output.AppendLine($"[{DemoPacket.Tick}] {Type.ToString().ToUpper()}");
			if (Type != PacketType.Stop) // just to prevent adding a new line for the stop packet
				output.AppendLine(DemoPacket.ToString());
			return output.ToString();
		}
	}
}