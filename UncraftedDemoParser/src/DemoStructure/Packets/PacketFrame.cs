using System;
using System.Text;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure.Packets {
	
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
	
	
	// Holds information such as packet type and what tick the packet occured on.
	// Also determines the size of the packet at the pointer of the byte array passed to its constructor,
	// and automatically increments the pointer to point to the next frame.
	public class PacketFrame : DemoComponent {

		private byte _typeValue;

		public PacketType Type {
			get => _typeValue.ToPacketType(_demoRef.DemoSettings.NewEngine);
			set => _typeValue = value.ToByte(_demoRef.DemoSettings.NewEngine);
		}
		
		public int Tick;
		public byte? AlignmentByte; // new engine only
		public DemoComponent DemoComponent;

		public byte[] remainingBytes; // only used for stop packet

		
		// pointer is the current index in the byte array, is automatically updated to go to the next frame
		public PacketFrame(byte[] data, ref int pointer, SourceDemo demoRef) : base(data, demoRef) {
			int originalPointer = pointer; // used to measure length of sub array to extract
			_typeValue = data[pointer];
			
			if (Type == PacketType.Stop) {
				DemoComponent = new Stop( demoRef);
				// this is a guess: new engine always has 5 bytes of leftover data, old engine always has 3
				remainingBytes = data.SubArray(pointer, demoRef.DemoSettings.NewEngine ? 5 : 3);
				pointer += 1 + remainingBytes.Length;
				//Console.WriteLine("STOP");
			} else {
				Tick = GetIntAtpointer(data, pointer + 1);
				
				// Console.WriteLine($"[{Tick}] {Type.ToString().ToUpper()}");

				int currentPointer = pointer + 5;
				if (demoRef.DemoSettings.HasAlignmentByte) {
					AlignmentByte = data[currentPointer];
					currentPointer++;
				}

				int packetLength;

				switch (Type) {
					case PacketType.SignOn:
						packetLength = demoRef.Header.SignOnLength;
						break;
					case PacketType.Packet:
						packetLength = demoRef.DemoSettings.MaxSplitscreenPlayers * 76 + 12; // length of the main chunk of the packet
						packetLength += GetIntAtpointer(data, currentPointer + packetLength - 4); // length of the additional data in the packet
						break;
					case PacketType.SyncTick:
						throw new ArgumentException("Idk how to parse the sync tick packet");
					case PacketType.ConsoleCmd:
						packetLength = GetIntAtpointer(data, currentPointer) + 4;
						break;
					case PacketType.UserCmd:
						packetLength = GetIntAtpointer(data, currentPointer + 4) + 8;
						break;
					case PacketType.DataTables:
						throw new ArgumentException("Idk how to parse the data tables packet");
					case PacketType.CustomData:
						packetLength = GetIntAtpointer(data, currentPointer + 4) + 8;
						break;
					case PacketType.StringTables:
						packetLength = GetIntAtpointer(data, currentPointer) + 4;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				DemoComponent = Type.ToPacket(data.SubArray(currentPointer, packetLength), demoRef).RequireNonNull();
				pointer = currentPointer + packetLength;
			}
			Bytes = data.SubArray(originalPointer, pointer - originalPointer);
		}


		public override void ParseBytes() {
			DemoComponent.ParseBytes();
		}

		public override void UpdateBytes() {
			BitFieldWriter bfw;
			if (Type == PacketType.Stop) {
				bfw = new BitFieldWriter(1 + remainingBytes.Length);
				bfw.WriteByte(_typeValue);
				bfw.WriteBytes(remainingBytes);
			} else {
				DemoComponent.UpdateBytes();
				bfw = new BitFieldWriter(5 + (AlignmentByte.HasValue ? 1 : 0) + DemoComponent.Bytes.Length);
				bfw.WriteByte(_typeValue);
				bfw.WriteInt(Tick);
				if (AlignmentByte.HasValue)
					bfw.WriteByte(AlignmentByte.Value);
				bfw.WriteBytes(DemoComponent.Bytes);
			}
			Bytes = bfw.Data;
		}


		private int GetIntAtpointer(byte[] data, int pointer) {
			byte[] intBytes = data.SubArray(pointer, 4);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intBytes);
			return BitConverter.ToInt32(intBytes, 0);
		}


		public override string ToString() {
			if (Type == PacketType.Stop) {
				return "STOP";
			} else {
				StringBuilder output = new StringBuilder();
				output.AppendLine($"[{Tick}] {Type.ToString().ToUpper()}");
				output.AppendLine(DemoComponent.ToString());
				return output.ToString();
			}
		}
	}
}