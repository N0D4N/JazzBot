using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using JazzBot.Enums;
using JazzBot.Services;
using JazzBot.Utilities;
using Microsoft.EntityFrameworkCore;
using File = TagLib.File;


namespace JazzBot.Data
{
	/// <summary>
	/// Music data for <see cref="DiscordGuild"/>.
	/// </summary>
	public sealed class GuildMusicData
	{
		/// <summary>
		/// Shows if something is played in this guild.
		/// </summary>
		public bool IsPlaying { get; set; } = false; //Not used probably should delete it

		/// <summary>
		/// Current song of type <see cref="TagLib.File"/> needed to showing what song is playing now.
		/// </summary>
		public File CurrentSong { get; set; } = null;

		/// <summary>
		/// Path to current song.
		/// </summary>
		public string PathToCurrentSong { get; set; }

		/// <summary>
		/// Seed of the current guild playlist.
		/// </summary>
		public int Seed { get; set; }

		/// <summary>
		/// Guild for which this data is stored.
		/// </summary>
		public DiscordGuild Guild { get; }

		/// <summary>
		/// Id of current song in playlist table.
		/// </summary>
		public int IdOfCurrentSong { get; set; }

		/// <summary>
		/// Name of current playlist.
		/// </summary>
		public string PlaylistName { get; set; } //Not used for now

		/// <summary>
		/// Message that shows detailed info about current playing song.
		/// </summary>
		public DiscordMessage PlayingMessage { get; set; }

		/// <summary>
		/// Voice channel in which music is playing.
		/// </summary>
		private DiscordChannel PlayingChannel => this.LavalinkConnection.Channel;

		/// <summary>
		/// Lavalink service.
		/// </summary>
		private LavalinkService Lavalink { get; }

		/// <summary>
		/// Lavalink connection in this guild.
		/// </summary>
		private LavalinkGuildConnection LavalinkConnection { get; set; }

		/// <summary>
		/// List that stores songs that should be played after currently playing song.
		/// </summary>
		private Stack<string> PlayNextStack { get; set; }

		/// <summary>
		/// Current song of type <see cref="LavalinkTrack"/> used for <see cref="LavalinkGuildConnection"/>.
		/// </summary>
		private LavalinkTrack Track { get; set; } // Not used for now

		/// <summary>
		/// Current bot program.
		/// </summary>
		private Program CurrentProgram { get; }

		public GuildMusicData(LavalinkService lavalink, DiscordGuild guild, Program program)
		{
			this.CurrentProgram = program;
			this.Guild = guild;
			this.Lavalink = lavalink;
			var db = new DatabaseContext();
			var dguild = db.Guilds.SingleOrDefault(x => x.IdOfGuild == this.Guild.Id);
			db.Dispose();
			this.Seed = dguild.Seed;
			this.IdOfCurrentSong = dguild.IdOfCurrentSong;
			this.PlaylistName = dguild.PlaylistName;
			this.PlayNextStack = new Stack<string>();	
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
			var songs = db.Playlist.Where(x => x.PlaylistName == this.PlaylistName).ToArray();
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
			this.CurrentSong = File.Create(path);
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
			var guild = db.Guilds.SingleOrDefault(x => x.IdOfGuild == this.Guild.Id);
			guild.IdOfCurrentSong = this.IdOfCurrentSong;
			guild.IdOfGuild = this.Guild.Id;
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
				throw new CustomJBException("Не удалось обновить базу данных",	ExceptionType.ForInnerPurposes);
			}
		}

		/// <summary>
		/// Starts playback.
		/// </summary>
		/// <param name="track"> Track that should be played </param>
		public void Play(LavalinkTrack track)
		{
			if (this.LavalinkConnection == null || !this.LavalinkConnection.IsConnected)
				return;
			
			this.InternalPlay(track);
			
		}

		/// <summary>
		/// Gets <see cref="LavalinkTrack"/> from <see cref="PathToCurrentSong"/> or top element in <see cref="PlayNextStack"/>.
		/// </summary>
		/// <returns><see cref="LavalinkTrack"/> from <see cref="PathToCurrentSong"/> or top element in <see cref="PlayNextStack"/></returns>
		public async Task<LavalinkTrack> GetSong()
		{
			// Songs in PlayNextList have higher priority than default playback.
			if (this.PlayNextStack.Any())
			{
				string path = this.PlayNextStack.Pop();
				var result = await this.Lavalink.LavalinkNode.GetTracksAsync(new FileInfo(path)).ConfigureAwait(false);
				this.CurrentSong = File.Create(path);
				return result.Tracks.ElementAt(0);
			}
			else
			{
				var result = await this.Lavalink.LavalinkNode.GetTracksAsync(new FileInfo(this.PathToCurrentSong)).ConfigureAwait(false);
				return result.Tracks.ElementAt(0);
			}
		}

		/// <summary>
		/// Skips currently playing song.
		/// </summary>
		/// <returns></returns>
		public void Skip() //Not used now
		{
			if (this.LavalinkConnection == null || !this.LavalinkConnection.IsConnected)
				return;

			this.LavalinkConnection.Stop();
			//await this.ChangeCurrentSong(true).ConfigureAwait(false);

			//this.InternalPlay(await GetSong().ConfigureAwait(false));
		}

		/// <summary>
		/// Creates player in specified <see cref="DiscordChannel"/> in this <see cref="DiscordGuild"/>.
		/// </summary>
		/// <param name="channel">Voice channel in which player should be created</param>
		/// <returns></returns>
		public async Task CreatePlayerAsync(DiscordChannel channel)
		{
			if (this.LavalinkConnection != null && this.LavalinkConnection.IsConnected)
				return;
			this.LavalinkConnection = await Lavalink.LavalinkNode.ConnectAsync(channel).ConfigureAwait(false);
			this.LavalinkConnection.PlaybackFinished += this.PlaybackFinished;
		}

		/// <summary>
		/// Destroys player in this <see cref="DiscordGuild"/>.
		/// </summary>
		/// <returns></returns>
		public async Task DestroyPlayerAsync()
		{
			if (this.LavalinkConnection == null)
				return;

			if (this.LavalinkConnection.IsConnected)
				this.LavalinkConnection.Disconnect();
			this.PlayNextStack.Clear();
			//this.LavalinkConnection.PlaybackFinished -= this.PlaybackFinished;

			this.LavalinkConnection = null;
			await this.PlayingMessage.DeleteAsync().ConfigureAwait(false);
			this.PlayingMessage = null;

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
		/// Stops playback.
		/// </summary>
		public void Stop()
		{
			if (this.LavalinkConnection == null || !this.LavalinkConnection.IsConnected)
				return;

			this.LavalinkConnection.Stop();
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
				var guild = await db.Guilds.SingleOrDefaultAsync(x => x.IdOfGuild == this.Guild.Id).ConfigureAwait(false);
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
		/// Creates detailed info about currently playing  song.
		/// </summary>
		/// <returns><see cref="DiscordEmbed"/> with info about currently playing song</returns>
		public async Task<DiscordEmbed> NowPlayingEmbedAsync()
		{

			var embed = new DiscordEmbedBuilder
			{
				Title = $"{DiscordEmoji.FromGuildEmote(this.CurrentProgram.Client, 518868301099565057)} Сейчас играет",
				Color = DiscordColor.Black,
				Timestamp = DateTimeOffset.Now + this.CurrentSong.Properties.Duration
			}
			.AddField("Название", this.CurrentSong.Tag.Title ?? "Неизвестное название")
			.AddField("Исполнитель", this.CurrentSong.Tag.FirstPerformer ?? "Неизвестный исполнитель")
			.AddField("Альбом", this.CurrentSong.Tag.Album ?? "Неизвестный альбом", true)
			.AddField("Дата", this.CurrentSong.Tag.Year.ToString() ?? "Неизвестная дата", true)
			.AddField("Длительность", this.CurrentSong.Properties.Duration.ToString(@"mm\:ss"), true)
			.AddField("Жанр", this.CurrentSong.Tag.FirstGenre ?? "Неизвестный жанр", true)
			.WithFooter("Приблизительное время окончания");

			if (this.CurrentSong.Tag.IsCoverArtLinkPresent())
			{
				embed.ThumbnailUrl = this.CurrentSong.Tag.Comment;
			}
			// Checking if cover art is present to this file.
			else if (this.CurrentSong.Tag.Pictures?.Any() == true ) 
			{
				var msg = await this.CurrentProgram.Bot.CoverArtsChannel.SendFileAsync("cover.jpg", new MemoryStream(this.CurrentSong.Tag.Pictures.ElementAt(0).Data.Data)).ConfigureAwait(false);
				this.CurrentSong.Tag.Comment = msg.Attachments[0].Url;
				this.CurrentSong.Save();
				embed.ThumbnailUrl = this.CurrentSong.Tag.Comment;
			}
				
			// Checking if this song was requested to add by some user.
			if (ulong.TryParse(this.CurrentSong.Tag.FirstComposer, out ulong requestedById)) 
			{
				var user = await this.CurrentProgram.Client.GetUserAsync(requestedById).ConfigureAwait(false);
				embed.Author = new DiscordEmbedBuilder.EmbedAuthor()
				{
					Name = $"@{user.Username}",
					IconUrl = user.AvatarUrl
				};
			}
			return embed.Build();
		}

		/// <summary>
		/// Checks if <see cref="PlayingMessage"/> is last in its channel.
		/// </summary>
		private bool IsMessageLast()
		{
			return this.PlayingMessage.Channel.LastMessageId == this.PlayingMessage.Id;
		}

		/// <summary>
		/// Adds song to <see cref="PlayNextStack"/>.
		/// </summary>
		/// <param name="path">Path to song</param>
		public void EnqueueToPlayNext(string path)
		{
			this.PlayNextStack.Push(path);
		}

		private async Task PlaybackFinished(TrackFinishEventArgs e)
		{	
			await Task.Delay(600).ConfigureAwait(false);
			this.IsPlaying = false;
			if (this.LavalinkConnection != null)
			{
				var db = new DatabaseContext();
				if (await db.Playlist.CountAsync().ConfigureAwait(false) < this.IdOfCurrentSong)
					this.Shuffle();
				else
					await this.ChangeCurrentSong(true).ConfigureAwait(false);
				if (this.IsMessageLast())
					await this.PlayingMessage.ModifyAsync(embed: await this.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);
				else
				{
					var plmsg = this.PlayingMessage;
					this.PlayingMessage = await this.PlayingMessage.Channel.SendMessageAsync(embed: await this.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);
					await plmsg.DeleteAsync().ConfigureAwait(false);					
				}
				this.InternalPlay(await this.GetSong());
			}
		}

		private void InternalPlay(LavalinkTrack track)
		{
			this.IsPlaying = true;
			this.LavalinkConnection.Play(track);
		}
	}
}
