using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using JazzBot.Services;


namespace JazzBot.Data.Music
{
	/// <summary>
	/// Music data for <see cref="DiscordGuild"/>.
	/// </summary>
	public sealed class GuildMusicData
	{
		/// <summary>
		/// Shows if something is played in this guild.
		/// </summary>
		public bool IsPlaying { get; set; }

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
		public LavalinkGuildConnection LavalinkConnection { get; private set; }

		/// <summary>
		/// Current bot program.
		/// </summary>
		private Program CurrentProgram { get; }

		public IMusicSource[] MusicSources { get; }

		public GuildMusicData(LavalinkService lavalink, DiscordGuild guild, Program program)
		{
			this.CurrentProgram = program;
			this.Guild = guild;
			this.Lavalink = lavalink;

			this.MusicSources = new IMusicSource[]
			{
				new RemoteMusicData(this.CurrentProgram),
				new PlayNextData(this.CurrentProgram),
				new LocalMusicData(this.Guild, this.CurrentProgram)
			};
		}

		/// <summary>
		/// Starts music playing in this guild
		/// </summary>
		public async Task Start()
		{
			if (this.LavalinkConnection == null || !this.LavalinkConnection.IsConnected || this.IsPlaying)
				return;

			var musicSource = this.MusicSources.First(x => x.IsPresent());
			var trackUri = await musicSource.GetCurrentSong();
			var trackLoad = await this.Lavalink.LavalinkNode.GetTracksAsync(trackUri);

			if (trackLoad.LoadResultType == LavalinkLoadResultType.TrackLoaded)
			{
				this.IsPlaying = true;
				this.InternalPlay(trackLoad.Tracks.First());
			}
			else
			{
				throw new ArgumentException($"По ссылке {trackUri} не удалось загрузить трек", nameof(trackUri));
			}
		}

		/// <summary>
		/// Skips currently playing song.
		/// </summary>
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
		public async Task CreatePlayerAsync(DiscordChannel channel)
		{
			if (this.LavalinkConnection != null && this.LavalinkConnection.IsConnected)
				return;
			this.LavalinkConnection = await this.Lavalink.LavalinkNode.ConnectAsync(channel).ConfigureAwait(false);
			this.LavalinkConnection.PlaybackFinished += this.PlaybackFinished;
		}

		/// <summary>
		/// Destroys player in this <see cref="DiscordGuild"/>.
		/// </summary>
		public async Task DestroyPlayerAsync()
		{
			if (this.LavalinkConnection == null)
				return;

			if (this.LavalinkConnection.IsConnected)
				this.LavalinkConnection.Disconnect();

			foreach (var musicSource in this.MusicSources)
				musicSource.ClearQueue();

			//this.LavalinkConnection.PlaybackFinished -= this.PlaybackFinished;
			this.IsPlaying = false;
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
			var musicSource = this.MusicSources.First(x => x.IsPresent());
			return await musicSource.GetCurrentSongEmbed();

		}

		/// <summary>
		/// Checks if <see cref="PlayingMessage"/> is last in its channel.
		/// </summary>
		private bool IsMessageLast()
			=> this.PlayingMessage.Channel.LastMessageId == this.PlayingMessage.Id;



		private async Task PlaybackFinished(TrackFinishEventArgs e)
		{
			await Task.Delay(600).ConfigureAwait(false);
			if (this.LavalinkConnection != null)
			{
				if (this.IsMessageLast())
					await this.PlayingMessage.ModifyAsync(embed: await this.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);
				else
				{
					var plMsg = this.PlayingMessage;
					this.PlayingMessage = await this.PlayingMessage.Channel.SendMessageAsync(embed: await this.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);
					await plMsg.DeleteAsync().ConfigureAwait(false);
				}

				var musicSource = this.MusicSources.First(x => x.IsPresent());
				var trackUri = await musicSource.GetCurrentSong();
				var trackLoad = await this.Lavalink.LavalinkNode.GetTracksAsync(trackUri);
				this.InternalPlay(trackLoad.Tracks.First());
			}
		}

		private void InternalPlay(LavalinkTrack track)
		{
			this.LavalinkConnection.Play(track);
		}
	}
}
