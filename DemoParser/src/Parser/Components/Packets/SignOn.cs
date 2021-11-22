using System;
using System.Linq;
using DemoParser.Parser.Components.Abstract;
using DemoParser.Utils;
using DemoParser.Utils.BitStreams;

namespace DemoParser.Parser.Components.Packets {
	
	/*
	 * The general structure of the sign on data looks like this:
	 * SignOn packet, DataTables packet, <StringTables packet>.
	 * If you use the sign on length in the header, you will skip over all this data. Since we want to parse everything,
	 * we parse those individual packets as much as we can. This class contains just the SignOn packet, not all of the
	 * sign on data. The general structure of this packet looks like this:
	 * [bunch of unknown data], [size of remaining data (I think, regardless we ignore this)], [messages].
	 * The unknown data poses the biggest problem since its size is game-dependent, and as far as I know can only be
	 * found with a brute force search. I don't know the contents of that data yet, so I simply skip over it. The
	 * messages contain the server info (which is very juicy) and the creation of the string tables. Since most games
	 * also have a string tables packet, I'm not sure what the purpose of having that data twice is...
	 */
	
	/// <summary>
	/// Contains server info and messages for the creation of string tables.
	/// </summary>
	public class SignOn : DemoPacket, IContainsMessageStream {

		public int Unknown1, Unknown2;
		public MessageStream MessageStream {get;set;}


		public SignOn(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}


		protected override void Parse(ref BitStreamReader bsr) {
			byte[] garbage = bsr.ReadBytes(DemoInfo.SignOnGarbageBytes);
			Unknown1 = bsr.ReadSInt();
			Unknown2 = bsr.ReadSInt();
			if (garbage.Any(b => b != 0))
				DemoRef.LogError("SignOn garbage data is not all 0's! Data: " + garbage.SequenceToString());
			MessageStream = new MessageStream(DemoRef);
			MessageStream.ParseStream(ref bsr);
		}
		

		internal override void WriteToStreamWriter(BitStreamWriter bsw) {
			throw new NotImplementedException();
		}


		public override void PrettyWrite(IPrettyWriter pw) {
			pw.AppendLine(DemoInfo.SignOnGarbageBytes + " bytes with no data");
			pw.AppendLine($"2 unknown ints: {Unknown1}, {Unknown2}");
			MessageStream.PrettyWrite(pw);
		}
	}
}