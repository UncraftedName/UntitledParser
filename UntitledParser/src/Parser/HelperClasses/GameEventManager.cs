using System.Collections.Generic;
using UntitledParser.Parser.Components.Messages;

namespace UntitledParser.Parser.HelperClasses {
	
	// for parsing SvcGameEvent, initialized from SvcGameEventList
	public class GameEventManager {
		
		public List<GameEventDescription> DescriptorsForEvents;
		
		
		public GameEventManager(List<GameEventDescription> descriptorsForEvents) {
			DescriptorsForEvents = descriptorsForEvents;
		}
	}
}