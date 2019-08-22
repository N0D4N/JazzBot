﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using F23.StringSimilarity;
using JazzBot.Data;
using JazzBot.Services;
using JazzBot.Utilities;
using Microsoft.EntityFrameworkCore;


namespace JazzBot.Commands
{
	[Group("Music")]
	[Description("Комманды связанные с музыкой")]
	[Aliases("lm", "lmu", "mu", "m")]
	[ModuleLifespan(ModuleLifespan.Transient)]
	public sealed class MusicCommands : BaseCommandModule
	{
		private LavalinkService Lavalink { get; }

		private Bot Bot { get; }

		private MusicService Music { get; }

		private GuildMusicData GuildMusic { get; set; }

		private YoutubeService Youtube { get; }

		public MusicCommands(LavalinkService lavalink, MusicService music, Bot bot, YoutubeService youtube)
		{
			this.Lavalink = lavalink;
			this.Music = music;
			this.Bot = bot;
			this.Youtube = youtube;
		}

		public override async Task BeforeExecutionAsync(CommandContext context)
		{
			if (context.Member?.VoiceState.Channel == null)
			{
				throw new ArgumentException("Вы должны быть в голосовом канале", nameof(context.Member.VoiceState));
			}

			this.GuildMusic = await this.Music.GetOrCreateDataAsync(context.Guild).ConfigureAwait(false);

			if (this.GuildMusic?.LavalinkConnection?.IsConnected == true && this.GuildMusic.LavalinkConnection.Channel.Id != context.Member.VoiceState.Channel.Id)
				throw new ArgumentException("Вы должны находиться в том же голосовом канале что и бот", nameof(context.Member.VoiceState));

			await base.BeforeExecutionAsync(context).ConfigureAwait(false);
		}

		[Command("Start")]
		[Description("Подключается и начинает проигрывать песню из плейлиста сервера")]
		[Aliases("st")]
		public async Task Start(CommandContext context)
		{
			if (this.GuildMusic.RemoteMusic?.Queue?.Any() == true)
				return;
			await this.GuildMusic.CreatePlayerAsync(context.Member.VoiceState.Channel).ConfigureAwait(false);

			//await this.GuildMusic.LocalMusic.ChangeCurrentSong(false);

			this.GuildMusic.Play(await this.GuildMusic.LocalMusic.GetSongAsync(this.Lavalink).ConfigureAwait(false));
			this.GuildMusic.PlayingMessage = await context.RespondAsync(embed: await this.GuildMusic.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);

		}

		[Command("Play")]
		[Description("Подключается и начинает воспроизведение трека из интернета по задданной ссылке или названию")]
		[Aliases("p")]
		[Priority(1)]
		public async Task Play(CommandContext context, [RemainingText, Description("Ссылка на трек")]Uri trackUri)
		{
			var loadResult = await this.Lavalink.LavalinkNode.GetTracksAsync(trackUri);
			if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches || !loadResult.Tracks.Any())
				throw new ArgumentException("Ошибка загрузки треков", nameof(trackUri));

			var tracks = loadResult.Tracks.Select(x => new RemoteMusicItem(x, context.Member)).ToArray();
			this.GuildMusic.RemoteMusic.Add(tracks);

			if (this.GuildMusic.PlayingMessage == null)
				this.GuildMusic.PlayingMessage = await context.RespondAsync(embed: await this.GuildMusic.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);

			await this.GuildMusic.CreatePlayerAsync(context.Member.VoiceState.Channel).ConfigureAwait(false);
			if (!this.GuildMusic.IsPlaying)
			{
				this.GuildMusic.Play(this.GuildMusic.RemoteMusic.GetSong());
				this.GuildMusic.RemoteMusic.Pop();
			}
			else
			{
				await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle($"{tracks.Length} трек(ов) было добавлено в очередь")).ConfigureAwait(false);
			}
		}

		[Command("Play")]
		[Priority(0)]
		public async Task Play(CommandContext context, [RemainingText, Description("Текст для поиска")]string searchQuery)
		{
			var searchResults = (await this.Youtube.SearchAsync(searchQuery)).ToArray();
			var description = new StringBuilder();
			int i = 1;
			foreach (var el in searchResults)
			{
				description.AppendLine($"{i}. {Formatter.InlineCode(el.VideoTitle)} {Formatter.Bold("-")} {Formatter.InlineCode(el.ChannelName)}");
				i++;
			}
			if (!searchResults.Any())
				throw new ArgumentException("По заданному запросу на Youtube ничего не было найдено", nameof(searchQuery));
			var interactivity = context.Client.GetInteractivity();

			var msg = await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Выберите песню (ответьте 0 если чтобы отменить комманду)")
				.WithDescription(description.ToString())).ConfigureAwait(false);
			var intRes = await interactivity.WaitForMessageAsync(x => x.Author.Id == context.User.Id, TimeSpan.FromSeconds(10));
			if (intRes.TimedOut || !int.TryParse(intRes.Result.Content, out int result))
				throw new ArgumentException("Время вышло или ответ не является числом");
			if (result < 0 || result > searchResults.Length + 1)
				throw new ArgumentException("Число выходит за заданные границы");
			if (result == 0)
			{
				await msg.ModifyAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle("Поиск отменен").Build()).ConfigureAwait(false);
				return;
			}

			var selectedTrack = searchResults.ElementAt(result - 1);
			var loadResult = await this.Lavalink.LavalinkNode.GetTracksAsync(new Uri($"https://youtu.be/{selectedTrack.VideoId}"));
			if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || !loadResult.Tracks.Any())
				throw new ArgumentException("По данной ссылке ничего не было найдено");

			var tracks = loadResult.Tracks.Select(x => new RemoteMusicItem(x, context.Member)).ToArray();
			this.GuildMusic.RemoteMusic.Add(tracks);

			await msg.DeleteAsync().ConfigureAwait(false);

			if (this.GuildMusic.PlayingMessage == null)
				this.GuildMusic.PlayingMessage = await context.RespondAsync(embed: await this.GuildMusic.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);

			await this.GuildMusic.CreatePlayerAsync(context.Member.VoiceState.Channel).ConfigureAwait(false);
			if (!this.GuildMusic.IsPlaying)
			{
				this.GuildMusic.Play(this.GuildMusic.RemoteMusic.GetSong());
				this.GuildMusic.RemoteMusic.Pop();
			}
			else
			{
				await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle($"{tracks.Length} трек(ов) было добавлено в очередь")).ConfigureAwait(false);
			}


		}

		[Command("Leave")]
		[Description("Покидает голосовой канал к которому подключен")]
		[Aliases("lv", "l")]
		public async Task Leave(CommandContext context)
		{
			this.GuildMusic.Stop();
			await this.GuildMusic.DestroyPlayerAsync().ConfigureAwait(false);
			await context.RespondAsync("Бот отключен").ConfigureAwait(false);
		}


		[Command("SwitchPlaylist")]
		[Description("Сменить текущий плейлист")]
		[Aliases("sp")]
		[Cooldown(1, 300, CooldownBucketType.Guild)]
		public async Task SwitchPlaylist(CommandContext context, [RemainingText, Description("Название плейлиста")] string playlistName)
		{
			if (string.IsNullOrWhiteSpace(playlistName))
				throw new ArgumentException("Название плейлиста не должно быть пустым", nameof(playlistName));
			await this.GuildMusic.LocalMusic.ChangePlaylistAsync(playlistName).ConfigureAwait(false);
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle($"Плейлист успешно изменен на {playlistName}")).ConfigureAwait(false);
		}

		[Command("PlayNext")]
		[Description("Ищет песню по названию и воспроизводит ее следующей")]
		[Aliases("pn", "enqueue")]
		public async Task PlayNext(CommandContext context, [RemainingText, Description("Название песни")] string songName)
		{
			if (string.IsNullOrWhiteSpace(songName))
				throw new ArgumentException("Название песни не должно быть пустым", nameof(songName));
			var db = new DatabaseContext();
			var songs = await db.Playlist.ToArrayAsync().ConfigureAwait(false);
			var playNexts = new List<PlayNextElement>();
			var nL = new NormalizedLevenshtein();
			foreach (var song in songs)
			{
				playNexts.Add(new PlayNextElement(song.Path, song.Name, nL.Distance(song.Name, songName)));
			}
			playNexts = playNexts.OrderBy(s => s.Coefficient).ToList();
			var interactivity = context.Client.GetInteractivity();
			var description = new StringBuilder();
			for (int i = 0; i < 10; i++)
				description.AppendLine($"\n№ {i + 1}; Name: {playNexts[i].Title}.");

			var listMsg = await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Предолагаемый список песен")
				.WithDescription(description.ToString())
				.WithFooter($"Отправьте {Formatter.InlineCode("x")} для отмены")).ConfigureAwait(false);

			var msg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == context.User.Id, TimeSpan.FromSeconds(45));
			await listMsg.DeleteAsync().ConfigureAwait(false);

			if (!msg.TimedOut)
			{
				if (int.TryParse(msg.Result.Content, out int res))
				{
					if (res >= 1 && res <= playNexts.Count)
					{
						var gId = (long) context.Guild.Id;
						var guild = await db.Guilds.SingleOrDefaultAsync(g => g.IdOfGuild == gId).ConfigureAwait(false);
						db.Dispose();
						this.GuildMusic.LocalMusic.EnqueueToPlayNext(playNexts[res - 1].PathToFile);
						await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
							.WithTitle($"Следующей песней будет {playNexts[res - 1].Title}")).ConfigureAwait(false);
					}
					else
					{
						db.Dispose();
						throw new ArgumentException("Данное число выходит за границы", nameof(res));
					}
				}
				else if (msg.Result.Content.ToLowerInvariant() == "x")
				{
					db.Dispose();
				}
				else
				{
					db.Dispose();
					throw new ArgumentException("Ответ не является числом или время вышло", nameof(msg));
				}
			}

		}

		[Command("Skip")]
		[Description("Останавливает воспроизведение текущей песни и начинает воспроизведение следующей в очереди песни")]
		public async Task Skip(CommandContext context)
		{
			this.GuildMusic.Skip();
			await context.RespondAsync("Песню пропущено").ConfigureAwait(false);
		}

		[Command("Stop")]
		[Description("Останавливает воспроизведение текущей песни и удаляет все песни из очередей")]
		public async Task Stop(CommandContext context)
		{
			if (this.GuildMusic.RemoteMusic.Queue.Any())
				this.GuildMusic.RemoteMusic.Queue.Clear();
			if (this.GuildMusic.LocalMusic.PlayNextStack.Any())
				this.GuildMusic.LocalMusic.PlayNextStack.Clear();
			this.GuildMusic.Stop();
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Воспроизведение остановлено и списки очищены")).ConfigureAwait(false);
		}

		[Command("Shuffle")]
		[Description("Перемешивает список на воспроизведение в таком порядке - Песни из Интернета -> общая очередь из локального источника")]
		public async Task Shuffle(CommandContext context)
		{
			this.GuildMusic.Shuffle();
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Список был перемешан")).ConfigureAwait(false);
		}


		[Command("Playlists")]
		[Description("Показывает список доступных плейлистов")]
		public async Task Playlists(CommandContext context)
		{
			var db = new DatabaseContext();
			var pls = db.Playlist.Select(x => x.PlaylistName).Distinct();
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Список доступных плейлистов")
				.WithDescription(string.Join(',', pls.Select(x => Formatter.InlineCode(x))))).ConfigureAwait(false);
		}

		[Command("Playlist")]
		[Aliases("list")]
		[Description("Дает ссылку на документ со всеми песнями")]
		public async Task Playlist(CommandContext context)
		{
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Все песни в плейлистах")
				.WithUrl(this.Bot.Config.Miscellaneous.PlaylistLink)).ConfigureAwait(false);
		}
	}
}