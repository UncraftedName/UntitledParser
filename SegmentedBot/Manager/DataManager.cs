using System;
using System.Collections.Generic;
using Discord.WebSocket;

namespace SegmentedBot.Manager {
	
	public interface IDataManager {
		
		void CreateGuildIfNotExists(string guildName);

		void RenameDemosInChannel(SocketGuildChannel channel, string formatStr);

		void PurgeAllData();

		void PurgeChannelDemos(SocketGuildChannel channel);

		void RemoveDemo(SocketGuildChannel channel, string demoName); // should match demo name or demo #

		// Returns null if this demo already exists.
		// Returns a tuple containing a list of demos that may have come directly before this one,
		// and a function to call as a response to the user selecting which demo to use.
		// The input to the returned function will be either the index of the list of the previous demo, or -1 for a new branch.
		Tuple<List<int>, Action<int>> AddDemo(SocketGuildChannel channel, byte[] demoData, string fileName);
	}
}