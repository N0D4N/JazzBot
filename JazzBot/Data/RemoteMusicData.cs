using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Lavalink;
using JazzBot.Utilities;

namespace JazzBot.Data
{
	/// <summary>
	/// Songs from remote source
	/// </summary>
	public sealed class RemoteMusicData
	{
		public List<RemoteMusicItem> Queue { get; }

		public RemoteMusicData()
			=> this.Queue = new List<RemoteMusicItem>();

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

	}
}
