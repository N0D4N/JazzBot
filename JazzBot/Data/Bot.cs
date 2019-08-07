﻿using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace JazzBot.Data
{
	public sealed class Bot
	{
		/// <summary>
		/// Path to directory where playlists are stored
		/// </summary>
		public string PathToDirectoryWithPlaylists { get; }

		/// <summary>
		/// Connection string to database for Entity Framework
		/// </summary>
		public string EFconstring { get; }

		/// <summary>
		/// Channel where all unexpected or important errors and exceptions should be posted
		/// </summary>
		public DiscordChannel ErrorChannel { get; private set; }

		/// <summary>
		/// Channel where all requests should be posted
		/// </summary>
		public DiscordChannel RequestChannel { get; private set; }

		/// <summary>
		/// Channel where all user-reports should be posted
		/// </summary>
		public DiscordChannel ReportChannel { get; private set; }

		/// <summary>
		/// Channel where coverarts for songs should be posted
		/// </summary>
		public DiscordChannel CoverArtsChannel { get; private set; }


		public JazzBotConfig Config {get;}


		public Bot(JazzBotConfig config, DiscordClient client)
		{
			this.PathToDirectoryWithPlaylists = config.Miscellaneous.PathToDirectoryWithPlaylists;
			this.EFconstring = config.Database.EntityFrameworkConnectionString;
			this.Config = config;
			client.Ready += this.Client_Ready;
		}

		private async Task Client_Ready(ReadyEventArgs e)
		{
			if (this.ErrorChannel == null)
				this.ErrorChannel = await e.Client.GetChannelAsync(this.Config.Discord.ErrorChannelID).ConfigureAwait(false);

			if (this.RequestChannel == null)
				this.RequestChannel = await e.Client.GetChannelAsync(this.Config.Discord.RequestChannelID).ConfigureAwait(false);

			if (this.ReportChannel == null)
				this.ReportChannel = await e.Client.GetChannelAsync(this.Config.Discord.ReportChannelID).ConfigureAwait(false);

			if (this.CoverArtsChannel == null)
				this.CoverArtsChannel = await e.Client.GetChannelAsync(this.Config.Discord.CoverArtsChannelID).ConfigureAwait(false);
		}
	}
}