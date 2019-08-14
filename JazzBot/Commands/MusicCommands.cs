using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Interactivity;
using F23.StringSimilarity;
using JazzBot.Data;
using JazzBot.Services;
using JazzBot.Utilities;
using DSharpPlus.Lavalink;
using Microsoft.EntityFrameworkCore;


namespace JazzBot.Commands
{
	[Group("LocalMusic")]
	[Description("Комманды связанные с музыкой")]
	[Aliases("lm", "lmu")]
	public sealed class MusicCommands : BaseCommandModule
	{
		private LavalinkService Lavalink { get; }


		private Bot Bot { get; }

		private MusicService Music { get; }

		private GuildMusicData GuildMusic { get; set; }

		public MusicCommands(LavalinkService lavalink, MusicService music, Bot bot)
		{
			this.Lavalink = lavalink;
			this.Music = music;
			this.Bot = bot;
		}

		public override async Task BeforeExecutionAsync(CommandContext context)
		{
			if (context.Member?.VoiceState.Channel == null)
			{
				throw new ArgumentException("Вы должны быть в голосовм канале", nameof(context.Member.VoiceState));
			}

			this.GuildMusic = await this.Music.GetOrCreateDataAsync(context.Guild).ConfigureAwait(false);

			await base.BeforeExecutionAsync(context).ConfigureAwait(false);
		}

		[Command("Start")]
		[Description("Подключается и начиает проигрывать песню из плейлиста гильдии")]
		[Aliases("st")]
		public async Task Start(CommandContext context)
		{

			await GuildMusic.CreatePlayerAsync(context.Member.VoiceState.Channel).ConfigureAwait(false);

			await this.GuildMusic.LocalMusic.ChangeCurrentSong(false);
			this.GuildMusic.PlayingMessage = await context.RespondAsync(embed: await this.GuildMusic.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);

			this.GuildMusic.Play(await this.GuildMusic.LocalMusic.GetSong(this.Lavalink).ConfigureAwait(false));

		}

		[Command("Play")]
		public async Task Play(CommandContext context, Uri trackUri)
		{
			var loadResult = await this.Lavalink.LavalinkNode.GetTracksAsync(trackUri);
			if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed || !loadResult.Tracks.Any())
			{
				await context.RespondAsync("Ошибка загрузки треков").ConfigureAwait(false);
				return;
			}
			if(loadResult.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
			{
				var tracks = loadResult.Tracks.Select(x => new RemoteMusicItem(x, context.Member));
				this.GuildMusic.RemoteMusic.Add(tracks);
			}
			else if(loadResult.LoadResultType == LavalinkLoadResultType.TrackLoaded)
			{
				this.GuildMusic.RemoteMusic.Add(new RemoteMusicItem(loadResult.Tracks.First(), context.Member));
			}

			this.GuildMusic.PlayingMessage = await context.RespondAsync(embed: await this.GuildMusic.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);

			await this.GuildMusic.CreatePlayerAsync(context.Member.VoiceState.Channel).ConfigureAwait(false);
			this.GuildMusic.Play(this.GuildMusic.RemoteMusic.GetSong());
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

		[Command("Playing")]
		[Description("Отображает информацию о текущей песни")]
		[Aliases("np", "nowplaying")]
		public async Task NowPlaying(CommandContext context)
		{
			await context.RespondAsync(embed: await this.GuildMusic.NowPlayingEmbedAsync().ConfigureAwait(false)).ConfigureAwait(false);
		}


		[Command("SwitchPlaylist")]
		[Description("Сменить текущий плейлист")]
		[Aliases("sp")]
		[Cooldown(1, 300, CooldownBucketType.Guild)]
		public async Task SwitchPlaylist(CommandContext context, [RemainingText, Description("Название плейлиста")] string playlistname)
		{
			if (string.IsNullOrWhiteSpace(playlistname))
				throw new ArgumentException("Название плейлиста не должно быть пустым", nameof(playlistname));
			await this.GuildMusic.LocalMusic.ChangePlaylist(playlistname).ConfigureAwait(false);
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle($"Плейлист успешно изменен на {playlistname}")).ConfigureAwait(false);
		}

		[Command("PlayNext")]
		[Description("Ищет песню по названию и воспроизводит ее следующей")]
		[Aliases("pn", "enqueue")]
		public async Task PlayNext(CommandContext context, [RemainingText, Description("Название песни")] string songname)
		{
			if (string.IsNullOrWhiteSpace(songname))
				throw new ArgumentException("Название песни не должно быть пустым", nameof(songname));
			var db = new DatabaseContext();
			var songs = await db.Playlist.ToArrayAsync().ConfigureAwait(false);
			var playNexts = new List<PlayNextElement>();
			var nL = new NormalizedLevenshtein();
			foreach (var song in songs)
			{
				playNexts.Add(new PlayNextElement(song.Path, song.Name, nL.Distance(song.Name, songname)));
			}
			playNexts = playNexts.OrderBy(s => s.Coefficient).ToList();
			var interactivity = context.Client.GetInteractivity();
			StringBuilder description = new StringBuilder();
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
						var gId = (long)context.Guild.Id;
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
				else if(msg.Result.Content.ToLowerInvariant() == "x")
				{
					db.Dispose();
					return;
				}
				else
				{
					db.Dispose();
					throw new ArgumentException("Ответ не является числом или время вышло", nameof(msg));
				}
			}

		}

		[Command("Skip")]
		[Description("Skips playing of the current track")]
		public async Task Skip(CommandContext context)
		{
			this.GuildMusic.Skip();
			await context.RespondAsync("Песню пропущено").ConfigureAwait(false);
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
				.WithUrl(Bot.Config.Miscellaneous.PlaylistLink)).ConfigureAwait(false);
		}
	}
}