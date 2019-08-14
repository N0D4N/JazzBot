using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using JazzBot.Data;

namespace JazzBot.Services
{
	public sealed class YoutubeService
	{
		private string ApiKey { get; }
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

		public async Task<IEnumerable<YoutubeSearchResult>> SearchAsync(string query)
		{
			var uri = new Uri($"https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=10&q={WebUtility.UrlEncode(query)}&type=video&fields=items(id(videoId)%2Csnippet(title%2CchannelTitle%2CchannelId%2Cthumbnails(medium(url))))&key={this.ApiKey}");
			var json = "{}";

			using (var request = await this.HttpClient.GetAsync(uri))
			using (var responce = await request.Content.ReadAsStreamAsync())
			using (var streamReader = new StreamReader(responce, new UTF8Encoding(false)))
				json = await streamReader.ReadToEndAsync().ConfigureAwait(false);

			var jsonData = JObject.Parse(json);
			return jsonData["items"].ToObject<IEnumerable<YoutubeResponce>>().Select(x=> new YoutubeSearchResult(x));

		}
	}

}
