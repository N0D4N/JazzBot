using DSharpPlus.Lavalink;
using JazzBot.Enums;

namespace JazzBot.Data
{
	public sealed class CurrentTrack
	{
		public SongType SongType { get; set; }

		public RemoteMusicItem RemoteTrack { get; set; }

		public CurrentTrack(SongType songType, RemoteMusicItem remoteMusic = null)
		{
			this.SongType = SongType;
			this.RemoteTrack = remoteMusic;
		}
	}
}
