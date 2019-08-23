using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using JazzBot.Utilities;

namespace JazzBot.Data.Music
{
	/// <summary>
	/// Songs from remote source
	/// </summary>
	public sealed class RemoteMusicData : IMusicSource
	{
		/// <summary>
		/// List of songs to play
		/// </summary>
		public List<RemoteMusicItem> Queue { get; }

		/// <summary>
		/// Current program
		/// </summary>
		private Program Program { get; }

		public RemoteMusicData(Program program)
		{
			this.Program = program;
			this.Queue = new List<RemoteMusicItem>();
		}

		/// <summary>
		/// Shuffles queue
		/// </summary>
		public void Shuffle()
			=> this.Queue.Shuffle();

		/// <summary>
		/// Add song in the end of queue
		/// </summary>
		/// <param name="song">Song to add</param>
		public void Add(RemoteMusicItem song)
			=> this.Queue.Add(song);

		/// <summary>
		/// Add many songs to queue
		/// </summary>
		/// <param name="songs">Songs to add</param>
		public void Add(IEnumerable<RemoteMusicItem> songs)
			=> this.Queue.AddRange(songs);

		/// <summary>
		/// Checks if songs are present in this music source
		/// </summary>
		public bool IsPresent()
		{
			return this.Queue.Any();
		}

		/// <summary>
		/// Get <see cref="DiscordEmbed"/> representing info about currently playing song
		/// </summary>
		public Task<DiscordEmbed> GetCurrentSongEmbed()
		{
			var track = this.Queue[0];
			var embed = new DiscordEmbedBuilder
			{
				Title = $"{DiscordEmoji.FromGuildEmote(this.Program.Client, 518868301099565057)} Сейчас играет",
				Color = track.RequestedByMember.Color,
				Timestamp = DateTime.Now + track.Track.Length,
				Description = $"{Formatter.MaskedUrl(track.Track.Title, track.Track.Uri)} - {track.Track.Author}",
			}.AddField("Длительность", track.Track.Length.ToString(@"mm\:ss"), true)
			.WithFooter("Приблизительное время окончания");

			return Task.FromResult(embed.Build());
		}

		/// <summary>
		/// Get <see cref="Uri"/> for song to play
		/// </summary>
		public Task<Uri> GetCurrentSong()
		{
			var song = this.Queue[0];
			this.Queue.RemoveAt(0);
			return Task.FromResult(song.Track.Uri);
		}

		/// <summary>
		/// Clears queue of songs to play
		/// </summary>
		public void ClearQueue()
			=> this.Queue.Clear();
	}
}
