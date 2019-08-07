using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using JazzBot.Data;

namespace JazzBot.Services
{
	public sealed class LavalinkService
	{
		public LavalinkNodeConnection LavalinkNode { get; private set; }
		private JazzBotConfig Config { get; }

		public LavalinkService(JazzBotConfig config, DiscordClient client)
		{
			this.Config = config;
			client.Ready += this.Client_Ready;
		}

		private async Task Client_Ready(ReadyEventArgs e)
		{
			if (this.LavalinkNode != null)
				return;
			var lavalink = e.Client.GetLavalink();
			this.LavalinkNode = await lavalink.ConnectAsync(new LavalinkConfiguration
			{
				Password = this.Config.Lavalink.Password,

				SocketEndpoint = new ConnectionEndpoint(this.Config.Lavalink.Hostname, this.Config.Lavalink.Port),
				RestEndpoint = new ConnectionEndpoint(this.Config.Lavalink.Hostname, this.Config.Lavalink.Port)
			}).ConfigureAwait(false);
		}
	}
}
