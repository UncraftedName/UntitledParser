using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace SegmentedRunBot.Modules {
	
	public class InfoModule : ModuleBase<SocketCommandContext> {

		[Command("say")]
		[Summary("Echoes a message")]
		public Task SayAsync([Remainder] [Summary("The text to echo")]
			string echo) => ReplyAsync(echo + " (you fucking twat)");
	}
	

	public class SampleModule : ModuleBase<SocketCommandContext> {

		[Command("square")]
		[Summary("squares a number")]
		public async Task SquareAsync([Summary("The number to square")] float num) {
			await Context.Channel.SendMessageAsync($"{num}^2 = {(num * num)}");
		}


		[Command("userinfo")]
		[Summary("returns info about user")]
		public async Task UserInfoAsync([Summary("The user to get info from")] SocketUser user = null) {
			var currentUser = user ?? Context.Client.CurrentUser;
			await ReplyAsync($"{currentUser.Username}#{currentUser.Discriminator}");
		}
	}
}