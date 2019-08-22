using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using JazzBot.Data;
using Newtonsoft.Json.Linq;
using JazzBot.Data.Music;

namespace JazzBot.Services
{
	/// <summary>
	/// Service for searching in youtube
	/// </summary>
	public sealed class YoutubeService
	{
		/// <summary>
		/// Youtube Api key
		/// </summary>
		private string ApiKey { get; }

		/// <summary>
		/// HttpClient which do all requests
		/// </summary>
		private HttpClient HttpClient { get; }

		public YoutubeService(JazzBotConfigYoutube config)
		{
			this.ApiKey = config.ApiKey;
			this.HttpClient = new HttpClient()
			{
				BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/search")
			};
			this.HttpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "JazzBot");
		}

		/// <summary>
		/// Searches Youtube for provided query
		/// </summary>
		/// <param name="query">String to search for</param>
		/// <returns>Results of searching</returns>
		public async Task<IEnumerable<YoutubeSearchResult>> SearchAsync(string query)
		{
			var uri = new Uri($"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=10&q={WebUtility.UrlEncode(query)}&type=video&fields=items(id(videoId)%2Csnippet(title%2CchannelTitle))&key={this.ApiKey}");
			var json = "{}";

			using (var request = await this.HttpClient.GetAsync(uri))
			using (var responce = await request.Content.ReadAsStreamAsync())
			using (var streamReader = new StreamReader(responce, new UTF8Encoding(false)))
				json = await streamReader.ReadToEndAsync().ConfigureAwait(false);

			var jsonData = JObject.Parse(json);
			return jsonData["items"].ToObject<IEnumerable<YoutubeResponce>>().Select(x => new YoutubeSearchResult(x));

		}
	}

}
