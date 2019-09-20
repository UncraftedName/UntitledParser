using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace SegmentedBot.Commands {
	
	public class AdminModule : ModuleBase<SocketCommandContext> {

		private static readonly Tuple<long, string>[] VerifiedAdmins = 
			{Tuple.Create(396934998881730560, "UncraftedName#6401")}; // move this to a file eventually™, allows it to be dynamic


		private bool NotVerifiedAdmin() {
			SocketUser user = Context.User;
			// (Context.Message.Channel as SocketGuildChannel).Guild;
			return !VerifiedAdmins.Contains(Tuple.Create((long)user.Id, user.ToString()));
		}


		[Command("purge")]
		public async Task PurgeAsync() {
			if (NotVerifiedAdmin()) return;
			await ReplyAsync("you fool, you absolute buffoon!");
		}
	}
}