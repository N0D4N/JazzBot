using Newtonsoft.Json;

namespace JazzBot.Data
{
	public sealed class YoutubeSearchResult
	{
		public string VideoTitle { get; }

		public string VideoId { get; }

		public string ChannelName { get; }

		public YoutubeSearchResult(string videoTitle, string videoId, string videoThumb, string channelName, string channelId)
		{
			this.VideoTitle = videoTitle;
			this.VideoId = videoId;
			this.ChannelName = channelName;
		}

		public YoutubeSearchResult(YoutubeResponce responce)
		{
			this.VideoTitle = responce.Snippet.VideoTitle;
			this.VideoId = responce.Id.VideoId;
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

			[JsonProperty("title")]
			public string VideoTitle { get; private set; }
			
			[JsonProperty("channelTitle")]
			public string ChannelTitle { get; private set; }


		}



	}

}
