using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using JazzBot.Utilities;

namespace JazzBot.Data.Music
{
	/// <summary>
	/// Songs from remote source
	/// </summary>
	public sealed class RemoteMusicData : IMusicSource
	{
		public List<RemoteMusicItem> Queue { get; }

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
		/// Get first song in queue
		/// </summary>
		/// <returns>First song in queue</returns>
		public LavalinkTrack GetSong()
			=> this.Queue[0].Track;

		/// <summary>
		/// Deletes first song in queue
		/// </summary>
		public void Pop()
		{
			if (this.Queue.Any())
				this.Queue.RemoveAt(0);
		}

		public bool IsPresent()
		{
			return this.Queue.Any();
		}

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

		public Task<Uri> GetCurrentSong()
		{
			var song = this.Queue[0];
			this.Queue.RemoveAt(0);
			return Task.FromResult(song.Track.Uri);
		}

		public void ClearQueue()
			=> this.Queue.Clear();
	}
}
