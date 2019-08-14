using DSharpPlus.Lavalink;
using System;
using DSharpPlus.Entities;

namespace JazzBot.Data
{
	public sealed class RemoteMusicItem
	{
		public YoutubeSearchResult YoutubeData { get; }

		public LavalinkTrack Track { get; }

		public DiscordMember RequestedByMember { get; }

		public Uri ChannelUri { get; }

		public Uri VideoUri { get; }

		public RemoteMusicItem(YoutubeSearchResult searchResult, LavalinkTrack track, DiscordMember requestedByMember)
		{
			this.YoutubeData = searchResult;
			this.Track = track;
			this.RequestedByMember = requestedByMember;

			this.VideoUri = new Uri($"https://youtu.be/{this.YoutubeData.VideoId}");
			this.ChannelUri = new Uri($"https://www.youtube.com/channel/{this.YoutubeData.ChannelId}");
		}
	}
}