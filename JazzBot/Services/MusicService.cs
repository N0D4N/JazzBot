﻿using System.Collections.Concurrent;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using JazzBot.Data.Music;

namespace JazzBot.Services
{
	public sealed class MusicService
	{
		public LavalinkService Lavalink { get; }

		/// <summary>
		/// Music data for <see cref="DiscordGuild"/> bot is in.
		/// </summary>
		private ConcurrentDictionary<ulong, GuildMusicData> MusicData { get; }
		private Program CurrentProgram { get; }

		public MusicService(LavalinkService lavalink, Program program)
		{
			this.Lavalink = lavalink;
			this.CurrentProgram = program;
			this.MusicData = new ConcurrentDictionary<ulong, GuildMusicData>();
		}

		public Task<GuildMusicData> GetOrCreateDataAsync(DiscordGuild guild)
		{
			if (this.MusicData.TryGetValue(guild.Id, out var guildMusicData))
				return Task.FromResult(guildMusicData);

			guildMusicData = this.MusicData.AddOrUpdate(guild.Id, new GuildMusicData(this.Lavalink, guild, this.CurrentProgram), (k, v) => v);
			return Task.FromResult(guildMusicData);
		}
	}
}