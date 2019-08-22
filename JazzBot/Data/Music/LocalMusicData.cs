using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using JazzBot.Enums;
using JazzBot.Services;
using JazzBot.Utilities;
using DSharpPlus;
using Microsoft.EntityFrameworkCore;
using File = TagLib.File;

namespace JazzBot.Data.Music
{
	/// <summary>
	/// Songs from local source
	/// </summary>
	public sealed class LocalMusicData : IMusicSource
	{

		/// <summary>
		/// Path to current song.
		/// </summary>
		public string PathToCurrentSong { get; set; } = null;

		/// <summary>
		/// Seed of the current guild playlist.
		/// </summary>
		public int Seed { get; set; }

		/// <summary>
		/// Id of current song in playlist table.
		/// </summary>
		public int IdOfCurrentSong { get; set; }

		/// <summary>
		/// Name of current playlist.
		/// </summary>
		public string PlaylistName { get; set; } //Not used for now

		/// <summary>
		/// List that stores songs that should be played after currently playing song.
		/// </summary>
		public Stack<string> PlayNextStack { get; }

		/// <summary>
		/// Guild for which this data is stored.
		/// </summary>
		public DiscordGuild Guild { get; }

		public Program Program { get; }

		public Bot Bot { get; }

		public LocalMusicData(DiscordGuild guild, Program currentProgram, Bot bot)
		{
			this.Program = currentProgram;
			this.Bot = bot;
			this.Guild = guild;
			var gId = (long) this.Guild.Id;
			var db = new DatabaseContext();
			var dGuild = db.Guilds.Single(x => x.IdOfGuild == gId);
			db.Dispose();
			this.Seed = dGuild.Seed;
			this.PlaylistName = dGuild.PlaylistName;
			this.IdOfCurrentSong = dGuild.IdOfCurrentSong;
			this.PlayNextStack = new Stack<string>();
		}

		/// <summary>
		/// Changes playlist in this guild.
		/// </summary>
		/// <param name="playlistName">New playlist</param>
		/// <returns></returns>
		public async Task ChangePlaylistAsync(string playlistName)
		{
			var db = new DatabaseContext();
			if (await db.Playlist.Select(x => x.PlaylistName).ContainsAsync(this.PlaylistName).ConfigureAwait(false))
			{
				var gId = (long) this.Guild.Id;
				var guild = await db.Guilds.SingleOrDefaultAsync(x => x.IdOfGuild == gId).ConfigureAwait(false);
				this.PlaylistName = playlistName;
				guild.PlaylistName = playlistName;
				this.Shuffle();
				db.Guilds.Update(guild);
				if (await db.SaveChangesAsync().ConfigureAwait(false) <= 0)
				{
					db.Dispose();
					throw new CustomJbException("Не удалось обновить базу данных", ExceptionType.DatabaseException);
				}
			}
			else
			{
				db.Dispose();
				throw new CustomJbException($"Плейлиста {playlistName} не существует", ExceptionType.PlaylistException);
			}
		}

		/// <summary>
		/// Shuffles playlist in this guild.
		/// </summary>
		public void Shuffle()
		{
			this.Seed = Helpers.CryptoRandom(0, 1000);
			this.IdOfCurrentSong = 1;
			this.ChangeCurrentSong(false);

		}

		/// <summary>
		/// Get current song info from database and optionally changes its id.
		/// </summary>
		/// <param name="updateId">Specifies if id of current song should be changed</param>
		/// <returns></returns>
		public Task ChangeCurrentSong(bool updateId)
		{
			if (updateId)
				this.IdOfCurrentSong++;
			var db = new DatabaseContext();
			var songs = db.Playlist.Where(x => x.PlaylistName == this.PlaylistName).ToList();
			db.Dispose();
			foreach (var song in songs)
			{
				song.Numing = Helpers.OrderingFormula(this.Seed, song.SongId);
			}
			string path = songs.OrderBy(x => x.Numing).ElementAt(this.IdOfCurrentSong).Path;
			while (!System.IO.File.Exists(path))
			{
				this.IdOfCurrentSong++;
				path = songs.ElementAt(this.IdOfCurrentSong).Path;
			}
			this.PathToCurrentSong = path;

			if (updateId)
				this.UpdateDb();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Updates info about this <see cref="DiscordGuild"/> in database.
		/// </summary>
		private void UpdateDb()
		{
			var db = new DatabaseContext();
			var gId = (long) this.Guild.Id;
			var guild = db.Guilds.Single(x => x.IdOfGuild == gId);
			guild.IdOfCurrentSong = this.IdOfCurrentSong;
			guild.IdOfGuild = (long) this.Guild.Id;
			guild.PlaylistName = this.PlaylistName;
			guild.Seed = this.Seed;

			db.Guilds.Update(guild);
			if (db.SaveChanges() > 0)
			{
				db.Dispose();
			}
			else
			{
				db.Dispose();
				throw new CustomJbException("Не удалось обновить базу данных", ExceptionType.ForInnerPurposes);
			}
		}

		/// <summary>
		/// Gets <see cref="LavalinkTrack"/> from <see cref="PathToCurrentSong"/> or top element in <see cref="PlayNextStack"/>.
		/// </summary>
		/// <returns><see cref="LavalinkTrack"/> from <see cref="PathToCurrentSong"/> or top element in <see cref="PlayNextStack"/></returns>
		public async Task<LavalinkTrack> GetSongAsync(LavalinkService lavalink)
		{
			var db = new DatabaseContext();
			var playlistLength = await db.Playlist.CountAsync().ConfigureAwait(false);
			if (playlistLength < this.IdOfCurrentSong)
				this.Shuffle();
			else
				await this.ChangeCurrentSong(true).ConfigureAwait(false);
			db.Dispose();

			// Songs in PlayNextList have higher priority than default playback.
			if (this.PlayNextStack.Any())
			{
				string path = this.PlayNextStack.Pop();
				var result = await lavalink.LavalinkNode.GetTracksAsync(new FileInfo(path)).ConfigureAwait(false);
				return result.Tracks.ElementAt(0);
			}
			else
			{
				var result = await lavalink.LavalinkNode.GetTracksAsync(new FileInfo(this.PathToCurrentSong)).ConfigureAwait(false);
				return result.Tracks.ElementAt(0);
			}
		}

		public bool IsPresent()
			=> true;

		public async Task<DiscordEmbed> GetCurrentSongEmbed()
		{
			var currentSong = File.Create(this.PathToCurrentSong);
			var embed = new DiscordEmbedBuilder
			{
				Title = $"{DiscordEmoji.FromGuildEmote(this.Program.Client, 518868301099565057)} Сейчас играет",
				Color = DiscordColor.Black,
				Timestamp = DateTimeOffset.Now + currentSong.Properties.Duration
			}
			.AddField("Название", currentSong.Tag.Title ?? "Неизвестное название")
			.AddField("Исполнитель", currentSong.Tag.FirstPerformer ?? "Неизвестный исполнитель")
			.AddField("Альбом", currentSong.Tag.Album ?? "Неизвестный альбом", true)
			.AddField("Дата", currentSong.Tag.Year.ToString(), true)
			.AddField("Длительность", currentSong.Properties.Duration.ToString(@"mm\:ss"), true)
			.AddField("Жанр", currentSong.Tag.FirstGenre ?? "Неизвестный жанр", true)
			.WithFooter("Приблизительное время окончания");

			if (currentSong.Tag.IsCoverArtLinkPresent())
			{
				embed.ThumbnailUrl = currentSong.Tag.Comment;
			}
			// Checking if cover art is present to this file.
			else if (currentSong.Tag.Pictures?.Any() == true)
			{
				var msg = await this.Bot.CoverArtsChannel.SendFileAsync("cover.jpg", new MemoryStream(currentSong.Tag.Pictures.ElementAt(0).Data.Data)).ConfigureAwait(false);
				currentSong.Tag.Comment = msg.Attachments[0].Url;
				currentSong.Save();
				embed.ThumbnailUrl = currentSong.Tag.Comment;
			}

			// Checking if this song was requested to add by some user.
			if (ulong.TryParse(currentSong.Tag.FirstComposer, out ulong requestedById))
			{
				var user = await this.Program.Client.GetUserAsync(requestedById).ConfigureAwait(false);
				embed.Author = new DiscordEmbedBuilder.EmbedAuthor()
				{
					Name = $"@{user.Username}",
					IconUrl = user.AvatarUrl
				};
			}

			return embed.Build();
		}

		public Uri GetCurrentSong()
		{
			return new Uri(WebUtility.UrlEncode(this.PathToCurrentSong));
		}

		/// <summary>
		/// Adds song to <see cref="PlayNextStack"/>.
		/// </summary>
		/// <param name="path">Path to song</param>
		public void EnqueueToPlayNext(string path)
			=> this.PlayNextStack.Push(path);
	}
}
