using System;
using System.Threading.Tasks;
using Discord;

namespace SegmentedRunBot {
	
	internal class Program {

		public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();


		private Task Log(LogMessage msg) {
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}


		public async Task MainAsync() {
			
		}
	}
	// Configure your DiscordSocketClient to use these custom providers over the default ones.
	// To do this, set the WebSocketProvider and the optional UdpSocketProvider properties on the DiscordSocketConfig that you are passing into your client.
	// (needed supposedly cuz i have wind 7)
	/*var client = new DiscordSocketClient(new DiscordSocketConfig 
	{
		WebSocketProvider = WS4NetProvider.Instance,
		UdpSocketProvider = UDPClientProvider.Instance,
	});*/
}