using Newtonsoft.Json;

namespace JazzBot.Data
{
	public sealed class YoutubeSearchResult
	{
		public string VideoTitle { get; }

		public string VideoId { get; }

		public string VideoThumbnailUrl { get; }

		public string ChannelName { get; }

		public string ChannelId { get; }

		public YoutubeSearchResult(string videoTitle, string videoId, string videoThumb, string channelName, string channelId)
		{
			this.VideoTitle = videoTitle;
			this.VideoId = videoId;
			this.VideoThumbnailUrl = videoThumb;
			this.ChannelName = channelName;
			this.ChannelId = channelId;
		}

		public YoutubeSearchResult(YoutubeResponce responce)
		{
			this.VideoTitle = responce.Snippet.VideoTitle;
			this.VideoId = responce.Id.VideoId;
			this.VideoThumbnailUrl = responce.Snippet.Thumbnail.ThumbnailMediumSize.VideoThumbnailUrl;
			this.ChannelId = responce.Snippet.ChannelId;
			this.ChannelName = responce.Snippet.ChannelTitle;
		}
	}

	public sealed class YoutubeResponce
	{
		[JsonProperty("id")]
		public ResponceId Id { get; private set; }

		[JsonProperty("snippet")]
		public ResponceSnippet Snippet { get; private set; }

		public sealed class ResponceId
		{
			[JsonProperty("videoId")]
			public string VideoId { get; private set; }

		}

		public sealed class ResponceSnippet
		{
			[JsonProperty("channelId")]
			public string ChannelId { get; private set; }

			[JsonProperty("title")]
			public string VideoTitle { get; private set; }

			[JsonProperty("thumbnails")]
			public ResponceThumbnail Thumbnail { get; private set; }
			
			[JsonProperty("channelTitle")]
			public string ChannelTitle { get; private set; }

			public sealed class ResponceThumbnail
			{
				[JsonProperty("medium")]
				public ResponceThumbnailMedium ThumbnailMediumSize { get; private set; }

				public sealed class ResponceThumbnailMedium
				{
					[JsonProperty("url")]
					public string VideoThumbnailUrl { get; private set; }
				}

			}

		}



	}

}
