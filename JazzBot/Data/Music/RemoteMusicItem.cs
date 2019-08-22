using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace JazzBot.Data.Music
{
	public sealed class RemoteMusicItem
	{
		/// <summary>
		/// Song to play
		/// </summary>
		public LavalinkTrack Track { get; }

		/// <summary>
		/// Member that requested this song
		/// </summary>
		public DiscordMember RequestedByMember { get; }

		public RemoteMusicItem(LavalinkTrack track, DiscordMember requestedByMember)
		{
			this.Track = track;
			this.RequestedByMember = requestedByMember;

		}
	}
}