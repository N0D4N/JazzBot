using DSharpPlus.Entities;
using DSharpPlus.Lavalink;

namespace JazzBot.Data
{
	public sealed class RemoteMusicItem
	{
		public LavalinkTrack Track { get; }

		public DiscordMember RequestedByMember { get; }

		public RemoteMusicItem(LavalinkTrack track, DiscordMember requestedByMember)
		{
			this.Track = track;
			this.RequestedByMember = requestedByMember;

		}
	}
}