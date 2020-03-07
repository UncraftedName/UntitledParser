using System.Collections.Generic;
using DemoParser.Parser.Components.Messages;

namespace DemoParser.Parser.HelperClasses {
	
	// for parsing SvcGameEvent, initialized from SvcGameEventList
	public class GameEventManager {
		
		public List<GameEventDescription> DescriptorsForEvents;
		
		
		public GameEventManager(List<GameEventDescription> descriptorsForEvents) {
			DescriptorsForEvents = descriptorsForEvents;
		}
	}
}