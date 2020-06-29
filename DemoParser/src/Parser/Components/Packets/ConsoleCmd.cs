using DemoParser.Parser.Components.Abstract;
using DemoParser.Parser.HelperClasses;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {
	
	/// <summary>
	/// Contains a command entered in the console or in-game.
	/// </summary>
	public class ConsoleCmd : DemoPacket {
		
		public string Command;
		
		
		public ConsoleCmd(SourceDemo demoRef, BitStreamReader reader, int tick) : base(demoRef, reader, tick) {}


		internal override void ParseStream(BitStreamReader bsr) {
			uint len = bsr.ReadUInt();
			int indexBefore = bsr.CurrentBitIndex;
			Command = bsr.ReadNullTerminatedString();
			bsr.CurrentBitIndex = (int)(indexBefore + len * 8); // to prevent null bytes from hecking this up
			SetLocalStreamEnd(bsr);
			TimingAdjustment.AdjustFromConsoleCmd(this);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new System.NotImplementedException();
		}


		public override void AppendToWriter(IndentedWriter iw) {
			iw.Append(Command);
		}
	}
}