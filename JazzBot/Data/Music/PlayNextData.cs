using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using File = TagLib.File;
using JazzBot.Utilities;
using System.IO;
using System.Net;

namespace JazzBot.Data.Music
{
	public sealed class PlayNextData : IMusicSource
	{
		private Queue<string> PlayingQueue { get; }

		private Program Program { get; }

		public PlayNextData(Program program)
		{
			this.PlayingQueue = new Queue<string>();
			this.Program = program;
		}


		public bool IsPresent()
		{
			return this.PlayingQueue.Any();
		}

		public async Task<DiscordEmbed> GetCurrentSongEmbed()
		{
			var currentSong = File.Create(this.PlayingQueue.Peek());
			var embed = new DiscordEmbedBuilder
			{
				Title = $"{DiscordEmoji.FromGuildEmote(this.Program.Client, 518868301099565057)} Сейчас играет",
				Color = DiscordColor.Black,
				Timestamp = DateTimeOffset.Now + currentSong.Properties.Duration
			}
			.AddField("Название", currentSong.Tag.Title ?? "Неизвестное название")
			.AddField("Исполнитель", currentSong.Tag.FirstPerformer ?? "Неизвестный исполнитель")
			.AddField("Альбом", currentSong.Tag.Album ?? "Неизвестный альбом", true)
			.AddField("Дата", currentSong.Tag.Year.ToString(), true)
			.AddField("Длительность", currentSong.Properties.Duration.ToString(@"mm\:ss"), true)
			.AddField("Жанр", currentSong.Tag.FirstGenre ?? "Неизвестный жанр", true)
			.WithFooter("Приблизительное время окончания");

			if (currentSong.Tag.IsCoverArtLinkPresent())
			{
				embed.ThumbnailUrl = currentSong.Tag.Comment;
			}
			// Checking if cover art is present to this file.
			else if (currentSong.Tag.Pictures?.Any() == true)
			{
				var msg = await this.Program.Bot.CoverArtsChannel.SendFileAsync("cover.jpg", new MemoryStream(currentSong.Tag.Pictures.ElementAt(0).Data.Data)).ConfigureAwait(false);
				currentSong.Tag.Comment = msg.Attachments[0].Url;
				currentSong.Save();
				embed.ThumbnailUrl = currentSong.Tag.Comment;
			}

			// Checking if this song was requested to add by some user.
			if (ulong.TryParse(currentSong.Tag.FirstComposer, out ulong requestedById))
			{
				var user = await this.Program.Client.GetUserAsync(requestedById).ConfigureAwait(false);
				embed.Author = new DiscordEmbedBuilder.EmbedAuthor()
				{
					Name = $"@{user.Username}",
					IconUrl = user.AvatarUrl
				};
			}

			return embed.Build();
		}

		public Task<Uri> GetCurrentSong()
		{
			var currentSong = this.PlayingQueue.Dequeue();
			FileInfo file = new FileInfo(currentSong);
			return Task.FromResult(new Uri(file.FullName));
		}

		public void Enqueue(string path)
		{
			this.PlayingQueue.Enqueue(path);
		}

		public void ClearQueue()
			=> this.PlayingQueue.Clear();
	}
}