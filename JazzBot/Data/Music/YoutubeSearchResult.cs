using Newtonsoft.Json;

namespace JazzBot.Data.Music
{
	/// <summary>
	/// Parsed responce of Youtube Api
	/// </summary>
	public sealed class YoutubeSearchResult
	{
		/// <summary>
		/// Title of the video
		/// </summary>
		public string VideoTitle { get; }

		/// <summary>
		/// Id of the video
		/// </summary>
		public string VideoId { get; }

		/// <summary>
		/// Name of the channel
		/// </summary>
		public string ChannelName { get; }

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
