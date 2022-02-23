using System.Collections.Generic;
using System.Linq;
using DemoParser.Parser.Components.Messages;

namespace DemoParser.Parser.GameState {

	// for parsing SvcGameEvent, initialized from SvcGameEventList
	public class GameEventManager {

		public readonly IReadOnlyDictionary<uint, GameEventDescription> EventDescriptions;


		public GameEventManager(IEnumerable<GameEventDescription> descriptors) {
			EventDescriptions = descriptors.ToDictionary(desc => desc.EventId, desc => desc);
		}
	}
}
