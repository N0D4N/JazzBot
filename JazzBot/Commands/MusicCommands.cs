using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using F23.StringSimilarity;
using JazzBot.Attributes;
using JazzBot.Data;
using JazzBot.Data.Music;
using JazzBot.Enums;
using JazzBot.Exceptions;
using JazzBot.Services;
using JazzBot.Utilities;
using Microsoft.EntityFrameworkCore;


namespace JazzBot.Commands
{
	[Group("Music")]
	[Description("Команды связанные с музыкой")]
	[Aliases("lm", "lmu", "mu", "m")]
	[ModuleLifespan(ModuleLifespan.Transient)]
	public sealed class MusicCommands : BaseCommandModule
	{
		private Bot Bot { get; }

		private MusicService Music { get; }

		private GuildMusicData GuildMusic { get; set; }

		private YoutubeService Youtube { get; }

		private DatabaseContext Database { get; }

		public MusicCommands(MusicService music, Bot bot, YoutubeService youtube, DatabaseContext db)
		{
			this.Music = music;
			this.Bot = bot;
			this.Youtube = youtube;
			this.Database = db;
		}

		public override async Task BeforeExecutionAsync(CommandContext context)
		{
			this.GuildMusic = await this.Music.GetOrCreateDataAsync(context.Guild).ConfigureAwait(false);
		}

		[Command("Start")]
		[Description("Подключается и начинает проигрывать песню из плейлиста сервера")]
		[Aliases("st")]
		[RequireVoiceConnection(false)]
		public async Task Start(CommandContext context)
		{
			if (this.GuildMusic.IsPlaying)
				return;
			await this.GuildMusic.CreatePlayerAsync(context.Member.VoiceState.Channel).ConfigureAwait(false);
			this.GuildMusic.PlayingMessage = await context.RespondAsync(embed: await this.GuildMusic.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);

			await this.GuildMusic.Start();
		}

		[Command("Play")]
		[Description("Подключается и начинает воспроизведение трека из интернета по задданной ссылке или названию")]
		[Aliases("p")]
		[Priority(1)]
		[RequireVoiceConnection(true)]
		public async Task Play(CommandContext context, [RemainingText, Description("Ссылка на трек")]Uri trackUri)
		{
			var loadResult = await this.Music.Lavalink.LavalinkNode.GetTracksAsync(trackUri);
			if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches || !loadResult.Tracks.Any())
				throw new DiscordUserInputException("Ошибка загрузки треков", nameof(trackUri));

			var tracks = loadResult.Tracks.Select(x => new RemoteMusicItem(x, context.Member)).ToArray();
			var remoteMusic = this.GuildMusic.MusicSources[(int) MusicSourceType.RemoteMusicData] as RemoteMusicData;
			remoteMusic.Add(tracks);


			await this.GuildMusic.CreatePlayerAsync(context.Member.VoiceState.Channel).ConfigureAwait(false);
			if (this.GuildMusic.IsPlaying)
			{
				await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle($"{tracks.Length} трек(ов) было добавлено в очередь")).ConfigureAwait(false);
			}
			else
			{
				if (this.GuildMusic.PlayingMessage == null)
					this.GuildMusic.PlayingMessage = await context.RespondAsync(embed: await this.GuildMusic.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);
				await this.GuildMusic.Start();
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
				description.AppendLine($"{i}. {Formatter.InlineCode(el.VideoTitle)} {Formatter.Bold("—")} {Formatter.InlineCode(el.ChannelName)}");
				i++;
			}
			if (!searchResults.Any())
				throw new DiscordUserInputException("По заданному запросу на Youtube ничего не было найдено", nameof(searchQuery));

			var interactivity = context.Client.GetInteractivity();

			var msg = await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Выберите песню (ответьте 0 если чтобы отменить команду)")
				.WithDescription(description.ToString())).ConfigureAwait(false);
			var intRes = await interactivity.WaitForMessageAsync(x => x.Author.Id == context.User.Id, TimeSpan.FromSeconds(10));
			if (intRes.TimedOut || !int.TryParse(intRes.Result.Content, out int result))
			{
				await msg.ModifyAsync("Время вышло или ответ не является числом", null).ConfigureAwait(false);
				return;

			}

			if (result < 0 || result > searchResults.Length + 1)
			{
				await msg.ModifyAsync("Число выходит за заданные границы", null).ConfigureAwait(false);
				return;
			}
			if (result == 0)
			{
				await msg.ModifyAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle("Поиск отменен").Build()).ConfigureAwait(false);
				return;
			}

			var selectedTrack = searchResults[result-1];
			var loadResult = await this.Music.Lavalink.LavalinkNode.GetTracksAsync(new Uri($"https://youtu.be/{selectedTrack.VideoId}"));
			if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || !loadResult.Tracks.Any())
				throw new DiscordUserInputException("По данной ссылке ничего не было найдено", nameof(selectedTrack));

			var tracks = loadResult.Tracks.Select(x => new RemoteMusicItem(x, context.Member)).ToArray();

			var remoteMusic = this.GuildMusic.MusicSources[(int) MusicSourceType.RemoteMusicData] as RemoteMusicData;
			remoteMusic.Add(tracks);

			await this.GuildMusic.CreatePlayerAsync(context.Member.VoiceState.Channel).ConfigureAwait(false);
			if (this.GuildMusic.IsPlaying)
			{
				await msg.ModifyAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle($"{tracks.Length} трек(ов) было добавлено в очередь").Build()).ConfigureAwait(false);
			}
			else
			{
				var playingEmbed = await this.GuildMusic.NowPlayingEmbedAsync();
				if (this.GuildMusic.PlayingMessage == null)
				{
					this.GuildMusic.PlayingMessage = await msg.ModifyAsync(embed: new DiscordEmbedBuilder(playingEmbed)
					{
						Description = $"{tracks.Length} трек(ов) было добавлено в очередь\n{playingEmbed.Description}"
					}.Build()).ConfigureAwait(false);
				}

				await this.GuildMusic.Start();
			}


		}

		[Command("Leave")]
		[Description("Покидает голосовой канал к которому подключен")]
		[Aliases("lv", "l")]
		[RequireVoiceConnection(true)]
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
		[RequireVoiceConnection(true)]
		public async Task SwitchPlaylist(CommandContext context, [RemainingText, Description("Название плейлиста")] string playlistName)
		{
			if (string.IsNullOrWhiteSpace(playlistName))
				throw new DiscordUserInputException("Название плейлиста не должно быть пустым", nameof(playlistName));

			var localMS = this.GuildMusic.MusicSources[(int) MusicSourceType.LocalMusicData] as LocalMusicData;

			await localMS.ChangePlaylistAsync(playlistName);

			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle($"Плейлист успешно изменен на {playlistName}")).ConfigureAwait(false);
		}

		[Command("PlayNext")]
		[Description("Ищет песню по названию и воспроизводит ее следующей")]
		[Aliases("pn", "enqueue")]
		[Cooldown(5, 15, CooldownBucketType.Guild)]
		[RequireVoiceConnection(true)]
		public async Task PlayNext(CommandContext context, [RemainingText, Description("Название песни")] string songName)
		{
			if (string.IsNullOrWhiteSpace(songName))
				throw new DiscordUserInputException("Название песни не должно быть пустым", nameof(songName));
			var songs = new List<Songs>();
			lock(this.Bot.UpdateMusicLock)
			{
				songs = this.Database.Playlist.ToList();
			}
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
				.WithFooter($"Отправьте {Formatter.InlineCode("0")} для отмены")).ConfigureAwait(false);

			var msg = await interactivity.WaitForMessageAsync(xm => xm.Author.Id == context.User.Id, TimeSpan.FromSeconds(45));

			if (!msg.TimedOut)
			{
				if (int.TryParse(msg.Result.Content, out int res))
				{
					if (res >= 1 && res <= playNexts.Count)
					{
						var pNData = this.GuildMusic.MusicSources[(int) MusicSourceType.PlayNextData] as PlayNextData;
						pNData.Enqueue(playNexts[res - 1].PathToFile);

						await listMsg.ModifyAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
							.WithTitle($"Следующей песней будет {playNexts[res - 1].Title}").Build()).ConfigureAwait(false);

						await Task.Delay(TimeSpan.FromSeconds(30));
						await listMsg.DeleteAsync();
					}
					else if (res == 0)
					{
						await listMsg.ModifyAsync("Выбор отменен", null).ConfigureAwait(false);
						await Task.Delay(TimeSpan.FromSeconds(30));
						await listMsg.DeleteAsync();
					}
					else
					{
						await listMsg.ModifyAsync("Данное число выходит за границы", null).ConfigureAwait(false);
						await Task.Delay(TimeSpan.FromSeconds(30));
						await listMsg.DeleteAsync();
					}
				}
				else
				{
					await listMsg.ModifyAsync("Ответ не является числом или время вышло", null).ConfigureAwait(false);
					await Task.Delay(TimeSpan.FromSeconds(30));
					await listMsg.DeleteAsync();
				}
			}

		}

		[Command("Skip")]
		[Description("Останавливает воспроизведение текущей песни и начинает воспроизведение следующей в очереди песни")]
		[Aliases("s")]
		[Cooldown(5, 15, CooldownBucketType.Guild)]
		[RequireVoiceConnection(true)]
		public async Task Skip(CommandContext context)
		{
			this.GuildMusic.Skip();
			await context.RespondAsync("Песню пропущено").ConfigureAwait(false);
		}

		[Command("Stop")]
		[Description("Останавливает воспроизведение текущей песни и удаляет все песни из очередей")]
		[RequireVoiceConnection(true)]
		public async Task Stop(CommandContext context)
		{
			this.GuildMusic.Stop();
			foreach (var musicSource in this.GuildMusic.MusicSources)
				musicSource.ClearQueue();
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Воспроизведение остановлено и списки очищены")).ConfigureAwait(false);
		}

		[Command("Shuffle")]
		[Description("Перемешивает список на воспроизведение в таком порядке - Песни из Интернета -> общая очередь из локального источника")]
		[RequireVoiceConnection(true)]
		public async Task Shuffle(CommandContext context)
		{
			var remoteMS = this.GuildMusic.MusicSources[(int) MusicSourceType.RemoteMusicData] as RemoteMusicData;
			if (remoteMS.IsPresent())
			{
				remoteMS.Shuffle();
				await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
					.WithTitle("Список из Интернета был перемешан")).ConfigureAwait(false);
				return;
			}

			var localMS = this.GuildMusic.MusicSources[(int) MusicSourceType.LocalMusicData] as LocalMusicData;
			localMS.Shuffle();
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Встроенный список был перемешан")).ConfigureAwait(false);
		}


		[Command("Playlists")]
		[Description("Показывает список доступных плейлистов")]
		public async Task Playlists(CommandContext context)
		{
			var playlists = new List<string>();
			lock(this.Bot.UpdateMusicLock)
			{
				playlists = this.Database.Playlist.Select(x => x.PlaylistName).Distinct().ToList();
			}
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Список доступных плейлистов")
				.WithDescription(string.Join(',', playlists.Select(x => Formatter.InlineCode(x))))).ConfigureAwait(false);
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