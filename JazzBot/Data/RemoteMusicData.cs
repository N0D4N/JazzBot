using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Lavalink;
using JazzBot.Utilities;

namespace JazzBot.Data
{
	public sealed class RemoteMusicData
	{
		public List<RemoteMusicItem> Queue { get; private set; }

		public RemoteMusicData()
			=> this.Queue = new List<RemoteMusicItem>();
		

		public void Shuffle()
			=> this.Queue.Shuffle();

		public void Add(RemoteMusicItem song)
			=> this.Queue.Add(song);
		

		public void Add(IEnumerable<RemoteMusicItem> songs)
			=> this.Queue.AddRange(songs);


		public LavalinkTrack GetSong()
			=> this.Queue[0].Track;

		public void Pop()
		{
			if(this.Queue.Any())
				this.Queue.RemoveAt(0);
		}

	}
}
