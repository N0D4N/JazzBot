using DSharpPlus.Lavalink;
using System;
using DSharpPlus.Entities;

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