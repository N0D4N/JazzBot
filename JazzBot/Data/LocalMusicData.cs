using System;
using System.Collections.Generic;
using System.Text;
using File = TagLib.File;
using DSharpPlus.Entities;
using JazzBot.Services;
using System.Linq;
using System.Threading.Tasks;
using JazzBot.Utilities;
using Microsoft.EntityFrameworkCore;
using JazzBot.Enums;
using System.IO;
using DSharpPlus.Lavalink.Entities;
using DSharpPlus.Lavalink;

namespace JazzBot.Data
{
	public sealed class LocalMusicData
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

		public LocalMusicData(DiscordGuild guild)
		{
			this.Guild = guild;
			var gId = (long) this.Guild.Id;
			var db = new DatabaseContext();
			var dguild = db.Guilds.SingleOrDefault(x => x.IdOfGuild == gId);
			db.Dispose();
			this.Seed = dguild.Seed;
			this.PlaylistName = dguild.PlaylistName;
			this.IdOfCurrentSong = dguild.IdOfCurrentSong;
			this.PlayNextStack = new Stack<string>();
		}

		/// <summary>
		/// Changes playlist in this guild.
		/// </summary>
		/// <param name="playlistName">New playlist</param>
		/// <returns></returns>
		public async Task ChangePlaylist(string playlistName)
		{
			var db = new DatabaseContext();
			if (await db.Playlist.Select(x => x.PlaylistName).ContainsAsync(PlaylistName).ConfigureAwait(false))
			{
				var gId = (long)this.Guild.Id;
				var guild = await db.Guilds.SingleOrDefaultAsync(x => x.IdOfGuild == gId).ConfigureAwait(false);
				this.PlaylistName = playlistName;
				guild.PlaylistName = playlistName;
				this.Shuffle();
				db.Guilds.Update(guild);
				if (await db.SaveChangesAsync().ConfigureAwait(false) <= 0)
				{
					db.Dispose();
					throw new CustomJBException("Не удалось обновить базу данных", ExceptionType.DatabaseException);
				}
			}
			else
			{
				db.Dispose();
				throw new CustomJBException($"Плейлиста {playlistName} не существует", ExceptionType.PlaylistException);
			}
		}

		/// <summary>
		/// Shuffles playlist in this guild.
		/// </summary>
		public void Shuffle()
		{
			this.Seed = Helpers.Cryptorandom(0, 1000);
			this.IdOfCurrentSong = 1;
			this.ChangeCurrentSong(false);

		}

		/// <summary>
		/// Get current song info from database and optionally changes its id.
		/// </summary>
		/// <param name="updateID">Specifies if id of current song should be changed</param>
		/// <returns></returns>
		public Task ChangeCurrentSong(bool updateID)
		{
			if (updateID)
				IdOfCurrentSong++;
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
				IdOfCurrentSong++;
				path = songs.ElementAt(this.IdOfCurrentSong).Path;
			}
			this.PathToCurrentSong = path;

			if (updateID)
				UpdateDB();
			return Task.CompletedTask;
		}

		/// <summary>
		/// Updates info about this <see cref="DiscordGuild"/> in database.
		/// </summary>
		private void UpdateDB()
		{
			var db = new DatabaseContext();
			var gId = (long)this.Guild.Id;
			var guild = db.Guilds.SingleOrDefault(x => x.IdOfGuild == gId);
			guild.IdOfCurrentSong = this.IdOfCurrentSong;
			guild.IdOfGuild = (long)this.Guild.Id;
			guild.PlaylistName = this.PlaylistName;
			guild.Seed = this.Seed;

			db.Guilds.Update(guild);
			if (db.SaveChanges() > 0)
			{
				db.Dispose();
				return;
			}
			else
			{
				db.Dispose();
				throw new CustomJBException("Не удалось обновить базу данных", ExceptionType.ForInnerPurposes);
			}
		}

		/// <summary>
		/// Gets <see cref="LavalinkTrack"/> from <see cref="PathToCurrentSong"/> or top element in <see cref="PlayNextStack"/>.
		/// </summary>
		/// <returns><see cref="LavalinkTrack"/> from <see cref="PathToCurrentSong"/> or top element in <see cref="PlayNextStack"/></returns>
		public async Task<LavalinkTrack> GetSong(LavalinkService lavalink)
		{
			var db = new DatabaseContext();
			if (await db.Playlist.CountAsync().ConfigureAwait(false) < this.IdOfCurrentSong)
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

		/// <summary>
		/// Adds song to <see cref="PlayNextStack"/>.
		/// </summary>
		/// <param name="path">Path to song</param>
		public void EnqueueToPlayNext(string path)
		{
			this.PlayNextStack.Push(path);
		}
	}
}
