namespace DemoParser.Parser.Components.Packets {

	/*
	 * The sign on length in the demo header contains one (or more?) sign packet(s), a data tables packet, and a string
	 * tables packet. This sign on packet used to have a bunch of mysterious bytes, but due to the power of not
	 * believing in coincidences, I'm like 98% sure that this is just a normal packet. The only difference is that this
	 * gets stuff like SvcServerInfo and SvcCreateStringTables messages which you don't get in normal packets. It is
	 * left as an exercise for the reader to determine why there is a string tables packet AND SvcCreateStringTables
	 * messages (seriously why? It's basically just the same huge lump of data twice).
	 */

	public class SignOn : Packet {
		public SignOn(SourceDemo? demoRef, PacketFrame frameRef) : base(demoRef, frameRef) {}
	}
}
