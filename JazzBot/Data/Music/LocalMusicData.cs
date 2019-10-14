using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using JazzBot.Enums;
using JazzBot.Exceptions;
using JazzBot.Services;
using JazzBot.Utilities;
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
		/// Guild for which this data is stored.
		/// </summary>
		public DiscordGuild Guild { get; }

		public Program Program { get; }


		public LocalMusicData(DiscordGuild guild, Program currentProgram)
		{
			this.Program = currentProgram;
			this.Guild = guild;
			var gId = (long) this.Guild.Id;
			using(var db = new DatabaseContext())
			{
				var dGuild = db.Guilds.Single(x => x.IdOfGuild == gId);
				this.Seed = dGuild.Seed;
				this.PlaylistName = dGuild.PlaylistName;
				this.IdOfCurrentSong = dGuild.IdOfCurrentSong;
			}
			this.ChangeCurrentSong(false);
		}

		/// <summary>
		/// Changes playlist in this guild.
		/// </summary>
		/// <param name="playlistName">New playlist</param>
		/// <returns></returns>
		public async Task ChangePlaylistAsync(string playlistName)
		{
			using(var db = new DatabaseContext())
			{
				bool doesPlaylistExist = false;
				lock(this.Program.Bot.UpdateMusicLock)
				{
					doesPlaylistExist = db.Playlist.Select(x => x.PlaylistName).Distinct().Contains(this.PlaylistName);
				}
				if(doesPlaylistExist)
				{
					var gId = (long) this.Guild.Id;
					var guild = await db.Guilds.SingleOrDefaultAsync(x => x.IdOfGuild == gId).ConfigureAwait(false);
					this.PlaylistName = playlistName;
					guild.PlaylistName = playlistName;
					this.Shuffle();
					db.Guilds.Update(guild);
					int rowsAffected = db.SaveChanges();
					if(rowsAffected <= 0)
						throw new DatabaseException("Не удалось обновить базу данных", DatabaseActionType.Update);
					
				}
				else
					throw new DatabaseException($"Плейлиста {playlistName} не существует", DatabaseActionType.Get);
			}
		}

		/// <summary>
		/// Shuffles playlist in this guild.
		/// </summary>
		public void Shuffle()
		{
			this.Seed = Helpers.CryptoRandom(int.MinValue, int.MaxValue);
			this.IdOfCurrentSong = 1;
			this.ChangeCurrentSong(false);

		}

		/// <summary>
		/// Get current song info from database and optionally changes its id.
		/// </summary>
		/// <param name="updateId">Specifies if id of current song should be changed</param>
		/// <returns></returns>
		public void ChangeCurrentSong(bool updateId)
		{
			if (updateId)
				this.IdOfCurrentSong++;
			var songs = new List<Songs>();
			using(var db = new DatabaseContext())
			{
				lock(this.Program.Bot.UpdateMusicLock)
				{
					songs = db.Playlist.Where(x => x.PlaylistName == this.PlaylistName).ToList();
				}
			}
			foreach (var song in songs)
			{
				song.Numing = Helpers.OrderingFormula(this.Seed, song.SongId);
			}
			string path = songs.OrderBy(x => x.Numing).ElementAt(this.IdOfCurrentSong).Path;
			while (!System.IO.File.Exists(path))
			{
				if(this.IdOfCurrentSong++ < songs.Count)
					path = songs[this.IdOfCurrentSong].Path;
				else
				{
					this.Shuffle();
					return;
				}
			}
			this.PathToCurrentSong = path;

			if (updateId)
				this.UpdateDb();
		}

		/// <summary>
		/// Updates info about this <see cref="DiscordGuild"/> in database.
		/// </summary>
		private void UpdateDb()
		{
			using(var db = new DatabaseContext())
			{
				var gId = (long) this.Guild.Id;
				var guild = db.Guilds.Single(x => x.IdOfGuild == gId);
				guild.IdOfCurrentSong = this.IdOfCurrentSong;
				guild.IdOfGuild = (long) this.Guild.Id;
				guild.PlaylistName = this.PlaylistName;
				guild.Seed = this.Seed;
				db.Guilds.Update(guild);
				int rowsAffected = db.SaveChanges();

				if(rowsAffected <= 0)
					throw new DatabaseException("Не удалось обновить базу данных", DatabaseActionType.Update);
			}
		}

		/// <summary>
		/// Checks if songs are present in this music source
		/// </summary>
		public bool IsPresent()
		=> true;

		/// <summary>
		/// Get <see cref="DiscordEmbed"/> representing info about currently playing song
		/// </summary>
		public async Task<DiscordEmbed> GetCurrentSongEmbed()
		{
			DiscordEmbedBuilder embed = null;
			using(var currentSong = File.Create(this.PathToCurrentSong))
			{
				embed = new DiscordEmbedBuilder
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

				if(currentSong.Tag.IsCoverArtLinkPresent())
				{
					embed.ThumbnailUrl = currentSong.Tag.Comment;
				}
				// Checking if cover art is present to this file.
				else if(currentSong.Tag.Pictures?.Any() == true)
				{
					var msg = await this.Program.Bot.CoverArtsChannel.SendFileAsync("cover.jpg", new MemoryStream(currentSong.Tag.Pictures[0].Data.Data)).ConfigureAwait(false);
					currentSong.Tag.Comment = msg.Attachments[0].Url;
					currentSong.Save();
					embed.ThumbnailUrl = currentSong.Tag.Comment;
				}

				// Checking if this song was requested to add by some user.
				if(ulong.TryParse(currentSong.Tag.FirstComposer, out ulong requestedById))
				{
					var user = await this.Program.Client.GetUserAsync(requestedById).ConfigureAwait(false);
					embed.Author = new DiscordEmbedBuilder.EmbedAuthor()
					{
						Name = $"@{user.Username}",
						IconUrl = user.AvatarUrl
					};
				}
			}
			return embed?.Build();
		}

		/// <summary>
		/// Get <see cref="Uri"/> for song to play
		/// </summary>
		public Task<Uri> GetCurrentSong()
		{
			var file = new FileInfo(this.PathToCurrentSong);
			int playlistLength = 0;

			using(var db = new DatabaseContext())
			{
				lock(this.Program.Bot.UpdateMusicLock)
				{
					playlistLength = db.Playlist.Count(x => x.PlaylistName == this.PlaylistName);
				}
			}

			if(playlistLength < this.IdOfCurrentSong)
				this.Shuffle();
			else
				this.ChangeCurrentSong(true);
			return Task.FromResult(new Uri(file.FullName, UriKind.Relative));
		}

		/// <summary>
		/// Does nothing because we don't want to clear local queue
		/// </summary>
		public void ClearQueue()
		{
		}

	}
}
