using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net.Providers.WS4Net;
using Discord.WebSocket;
using SegmentedBot.Handler;

namespace SegmentedBot {
	
	class Program {

		private static DiscordSocketClient _client;


		public static void Main(string[] args) => MainAsync().GetAwaiter().GetResult();
		


		private static async Task MainAsync() {
			_client = new DiscordSocketClient(
				new DiscordSocketConfig {WebSocketProvider = WS4NetProvider.Instance});
			_client.Log += Log;
			await _client.LoginAsync(TokenType.Bot, File.ReadAllText( // too lazy to do proper secrets atm
				$@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Microsoft\UserSecrets\901a5b7c-ee65-4aa9-af81-a28c11cb91ce\secrets.json"));
			await _client.StartAsync();
			CommandHandler handler = new CommandHandler(_client, new CommandService());
			await handler.InstallCommandsAsync();
			Console.ReadKey();
			await _client.StopAsync();
			// await Task.Delay(-1);
		}


		private static Task Log(LogMessage msg) {
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		} 
	}
}