using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using JazzBot.Data;

namespace JazzBot.Services
{
	public sealed class MusicService
	{
		private LavalinkService Lavalink { get; }

		/// <summary>
		/// Music data for <see cref="DiscordGuild"/> bot is in
		/// </summary>
		public ConcurrentDictionary<ulong, GuildMusicData> MusicData { get; }
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



		public Task<LavalinkLoadResult> GetTracksAsync(FileInfo file)
			=> this.Lavalink.LavalinkNode.GetTracksAsync(file);
	}
}