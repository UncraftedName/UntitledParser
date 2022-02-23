using System.Collections.Generic;

namespace DemoParser.Parser.GameState {

	// There are many things that are updated as we parse - the string tables can get new entries, entity baselines are
	// updated, etc. This is just a little class to hold that clutter in one spot (it used to all be in the demo class).
	public class GameState {

		public DataTableParser? DataTableParser;
		internal GameEventManager? GameEventManager;
		internal readonly StringTablesManager StringTablesManager;
		internal EntitySnapshot? EntitySnapshot;
		internal EntityBaseLines? EntBaseLines;
		internal uint ClientSoundSequence; // increases with each reliable sound
		// We want access to the bit reader of each component after parsing, but some data must be decompressed so store
		// the bytes here. Any component that accesses this must know which index its data is at.
		internal readonly List<byte[]> DecompressedLookup;


		public GameState(SourceDemo demo) {
			DecompressedLookup = new List<byte[]>();
			StringTablesManager = new StringTablesManager(demo);
			ClientSoundSequence = 0;
		}
	}
}
