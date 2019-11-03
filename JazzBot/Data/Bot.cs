using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace JazzBot.Data
{
	public sealed class Bot
	{
		/// <summary>
		/// Path to directory where playlists are stored.
		/// </summary>
		public string PathToDirectoryWithPlaylists { get; }


		/// <summary>
		/// Channel where all unexpected or important errors and exceptions should be posted.
		/// </summary>
		public DiscordChannel ErrorChannel { get; private set; }

		/// <summary>
		/// Channel where all requests should be posted.
		/// </summary>
		public DiscordChannel RequestChannel { get; private set; }

		/// <summary>
		/// Channel where coverarts for songs should be posted.
		/// </summary>
		public DiscordChannel CoverArtsChannel { get; private set; }


		/// <summary>
		/// Bot's config
		/// </summary>
		public JazzBotConfig Config { get; }

		/// <summary>
		/// Version of bot
		/// </summary>
		public string Version { get; }

		public string LogName { get; set; }

		public string DeleteEmojiName { get; set; }

		public object UpdateMusicLock { get; } = new object();



		public Bot(JazzBotConfig config, DiscordClient client)
		{
			this.PathToDirectoryWithPlaylists = config.Miscellaneous.PathToDirectoryWithPlaylists;
			this.Config = config;
			this.Version = Assembly.GetEntryAssembly().GetName().Version.ToString(3);
			//this.NetCoreVersion = Assembly.GetEntryAssembly().GetCustomAttribute<TargetFrameworkAttribute>().FrameworkName;
			client.Ready += this.Client_Ready;
		}

		private async Task Client_Ready(ReadyEventArgs e)
		{
			if (this.ErrorChannel == null)
				this.ErrorChannel = await e.Client.GetChannelAsync(this.Config.Discord.ErrorChannelId).ConfigureAwait(false);

			if (this.RequestChannel == null)
				this.RequestChannel = await e.Client.GetChannelAsync(this.Config.Discord.RequestChannelId).ConfigureAwait(false);

			if (this.CoverArtsChannel == null)
				this.CoverArtsChannel = await e.Client.GetChannelAsync(this.Config.Discord.CoverArtsChannelId).ConfigureAwait(false);
		}
	}
}
