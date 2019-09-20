using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;

namespace SegmentedBot.Handler {
	
	public class CommandHandler {
		
		private readonly DiscordSocketClient _client;
		private readonly CommandService _commands;
		

		// Retrieve client and CommandService instance via ctor
		public CommandHandler(DiscordSocketClient client, CommandService commands) {
			_commands = commands;
			_client = client;
		}
		

		public async Task InstallCommandsAsync() {
			_client.MessageReceived += HandleCommandAsync;
			await _commands.AddModulesAsync(assembly: Assembly.GetEntryAssembly(),
				services: null);
		}
		

		private async Task HandleCommandAsync(SocketMessage messageParam) {
			// Don't process the command if it was a system message
			SocketUserMessage message = messageParam as SocketUserMessage;
			if (message == null) return;
			
			// (message.Channel as SocketGuildChannel)?.Guild.Name

			// Create a number to track where the prefix ends and the command begins
			int argPos = 0;
			
			// Create a WebSocket-based command context based on the message
			var context = new SocketCommandContext(_client, message);
			
			if (message.Attachments.Count == 0) {

				// Determine if the message is a command based on the prefix and make sure no bots trigger commands
				if (!(message.HasCharPrefix('!', ref argPos) ||
					  message.HasMentionPrefix(_client.CurrentUser, ref argPos)) ||
					message.Author.IsBot)
					return;

			} else {
				if (message.Author.IsBot)
					return;
				await message.Channel.SendMessageAsync("attachment");
				return;
			}
			await _commands.ExecuteAsync(
				context: context,
				argPos: argPos,
				services: null);
		}
	}
}