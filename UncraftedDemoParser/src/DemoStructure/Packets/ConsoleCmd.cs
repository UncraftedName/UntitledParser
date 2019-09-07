using UncraftedDemoParser.DemoStructure.Packets.Abstract;
using UncraftedDemoParser.Utils;

namespace UncraftedDemoParser.DemoStructure.Packets {
	
	// first 4 bytes are length of the command, the command is also null terminated
	public class ConsoleCmd : DemoPacket {

		// int size    <- not stored for simplicity
		public string Command;
		
		
		public ConsoleCmd(byte[] data, SourceDemo demoRef, int tick) : base(data, demoRef, tick) {}


		public override void ParseBytes() {
			Command = System.Text.Encoding.Default.GetString(
				Bytes.SubArray(4, Bytes.Length - 5));
		}

		public override void UpdateBytes() {
			BitFieldWriter bfw = new BitFieldWriter();
			bfw.WriteInt(Command.Length + 1); // string length + null terminator
			bfw.WriteString(Command);
			Bytes = bfw.Data;
		}


		public override string ToString() {
			return $"\t{Command}";
		}
	}
}