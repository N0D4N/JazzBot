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
using DSharpPlus;
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
		/// Guild for which this data is stored.
		/// </summary>
		public DiscordGuild Guild { get; }

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
		/// Current song of type <see cref="LavalinkTrack"/> used for <see cref="LavalinkGuildConnection"/>.
		/// </summary>
		private LavalinkTrack Track { get; set; } // Not used for now

		public LocalMusicData LocalMusic { get; }

		public RemoteMusicData RemoteMusic { get; }

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

			this.LocalMusic = new LocalMusicData(this.Guild);

			this.RemoteMusic = new RemoteMusicData();
		}		

		/// <summary>
		/// Starts playback.
		/// </summary>
		/// <param name="track"> Track that should be played </param>
		public void Play(LavalinkTrack track)
		{
			if (this.LavalinkConnection == null || !this.LavalinkConnection.IsConnected || this.IsPlaying)
				return;
			
			this.InternalPlay(track);
			
		}	

		/// <summary>
		/// Skips currently playing song.
		/// </summary>
		/// <returns></returns>
		public void Skip()
		{
			if (this.LavalinkConnection == null || !this.LavalinkConnection.IsConnected)
				return;

			this.LavalinkConnection.Stop();
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

			this.LocalMusic.PlayNextStack.Clear();
			this.RemoteMusic.Queue.Clear();

			//this.LavalinkConnection.PlaybackFinished -= this.PlaybackFinished;

			this.LavalinkConnection = null;
			await this.PlayingMessage.DeleteAsync().ConfigureAwait(false);
			this.PlayingMessage = null;

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
		/// Creates detailed info about currently playing  song.
		/// </summary>
		/// <returns><see cref="DiscordEmbed"/> with info about currently playing song</returns>
		public async Task<DiscordEmbed> NowPlayingEmbedAsync()
		{
			if(this.RemoteMusic.Queue.Any())
			{
				var track = this.RemoteMusic.Queue[0];
				var embed = new DiscordEmbedBuilder
				{
					Title = $"{DiscordEmoji.FromGuildEmote(this.CurrentProgram.Client, 518868301099565057)} Сейчас играет",
					Color = track.RequestedByMember.Color,
					Timestamp = DateTime.Now + track.Track.Length,
					Description = $"{Formatter.MaskedUrl(track.Track.Title, track.Track.Uri)} - {track.Track.Author}",
				}.AddField("Длительность", track.Track.Length.ToString(@"mm\:ss"), true)
				.WithFooter("Приблизительное время окончания");

				return embed.Build();
			}
			else
			{
				if (this.LocalMusic.CurrentSong == null)
					await this.LocalMusic.ChangeCurrentSong(false);
				var embed = new DiscordEmbedBuilder
				{
					Title = $"{DiscordEmoji.FromGuildEmote(this.CurrentProgram.Client, 518868301099565057)} Сейчас играет",
					Color = DiscordColor.Black,
					Timestamp = DateTimeOffset.Now + this.LocalMusic.CurrentSong.Properties.Duration
				}
			.AddField("Название", this.LocalMusic.CurrentSong.Tag.Title ?? "Неизвестное название")
			.AddField("Исполнитель", this.LocalMusic.CurrentSong.Tag.FirstPerformer ?? "Неизвестный исполнитель")
			.AddField("Альбом", this.LocalMusic.CurrentSong.Tag.Album ?? "Неизвестный альбом", true)
			.AddField("Дата", this.LocalMusic.CurrentSong.Tag.Year.ToString() ?? "Неизвестная дата", true)
			.AddField("Длительность", this.LocalMusic.CurrentSong.Properties.Duration.ToString(@"mm\:ss"), true)
			.AddField("Жанр", this.LocalMusic.CurrentSong.Tag.FirstGenre ?? "Неизвестный жанр", true)
			.WithFooter("Приблизительное время окончания");

				if (this.LocalMusic.CurrentSong.Tag.IsCoverArtLinkPresent())
				{
					embed.ThumbnailUrl = this.LocalMusic.CurrentSong.Tag.Comment;
				}
				// Checking if cover art is present to this file.
				else if (this.LocalMusic.CurrentSong.Tag.Pictures?.Any() == true)
				{
					var msg = await this.CurrentProgram.Bot.CoverArtsChannel.SendFileAsync("cover.jpg", new MemoryStream(this.LocalMusic.CurrentSong.Tag.Pictures.ElementAt(0).Data.Data)).ConfigureAwait(false);
					this.LocalMusic.CurrentSong.Tag.Comment = msg.Attachments[0].Url;
					this.LocalMusic.CurrentSong.Save();
					embed.ThumbnailUrl = this.LocalMusic.CurrentSong.Tag.Comment;
				}

				// Checking if this song was requested to add by some user.
				if (ulong.TryParse(this.LocalMusic.CurrentSong.Tag.FirstComposer, out ulong requestedById))
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
			
		}

		/// <summary>
		/// Checks if <see cref="PlayingMessage"/> is last in its channel.
		/// </summary>
		private bool IsMessageLast()
		{
			return this.PlayingMessage.Channel.LastMessageId == this.PlayingMessage.Id;
		}

		

		private async Task PlaybackFinished(TrackFinishEventArgs e)
		{	
			await Task.Delay(600).ConfigureAwait(false);
			this.IsPlaying = false;
			if (this.LavalinkConnection != null)
			{
				if (this.IsMessageLast())
					await this.PlayingMessage.ModifyAsync(embed: await this.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);
				else
				{
					var plmsg = this.PlayingMessage;
					this.PlayingMessage = await this.PlayingMessage.Channel.SendMessageAsync(embed: await this.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);
					await plmsg.DeleteAsync().ConfigureAwait(false);					
				}
				var track = this.RemoteMusic.Queue.Any() ? this.RemoteMusic.GetSong() : await this.LocalMusic.GetSong(this.Lavalink).ConfigureAwait(false);
				this.InternalPlay(track);
			}
		}

		private void InternalPlay(LavalinkTrack track)
		{
			this.IsPlaying = true;
			this.LavalinkConnection.Play(track);
		}
	}
}
