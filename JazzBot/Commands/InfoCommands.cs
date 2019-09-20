using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using JazzBot.Data;
using JazzBot.Enums;
using JazzBot.Services;
using JazzBot.Utilities;
using Microsoft.Extensions.PlatformAbstractions;

namespace JazzBot.Commands
{
	[Group("Info")]
	[Description("Команды показывающие информацию о боте")]
	[ModuleLifespan(ModuleLifespan.Transient)]
	public sealed class InfoCommands : BaseCommandModule
	{
		private Bot Bot { get; }
		private LavalinkNodeConnection Lavalink { get; }

		public InfoCommands(Bot bot, LavalinkService lavalink)
		{
			this.Bot = bot;
			this.Lavalink = lavalink.LavalinkNode;
		}

		[Command("BotInfo")]
		[Description("Показывает информацию о этом боте")]
		[Aliases("about")]
		public async Task BotInfo(CommandContext context)
		{
			long memoryBytes = Process.GetCurrentProcess().PrivateMemorySize64;

			var dspVersion = context.Client.VersionString;

			var dncVersion = PlatformServices.Default.Application.RuntimeFramework.Version.ToString(2);
			var owner = context.Client.CurrentApplication.Owners.First();

			var description = new StringBuilder();

			var botRepos = new Uri("https://github.com/N0D4N/JazzBot");
			var dspRepos = new Uri("https://github.com/DSharpPlus/DSharpPlus");
			var lavalinkRepos = new Uri("https://github.com/Frederikam/Lavalink");
			var botWiki = new Uri("https://github.com/N0D4N/JazzBot/wiki");
			var supServerInvite = new Uri("https://discord.gg/xzynbQC");
			var botInvite = new Uri($"https://discordapp.com/api/oauth2/authorize?client_id={context.Client.CurrentUser.Id}&permissions=120966212&scope=bot");

			description.AppendLine($"{this.Bot.LogName} — музыкальный бот созданый на C# c помощью библиотеки DSharpPlus")
				.AppendLine()
				.AppendLine("Полезные ссылки")
				.AppendLine(Formatter.MaskedUrl("Репозиторий бота", botRepos))
				.AppendLine(Formatter.MaskedUrl("Репозиторий DSharpPlus", dspRepos))
				.AppendLine(Formatter.MaskedUrl("Репозиторий Lavalink", lavalinkRepos))
				.AppendLine(Formatter.MaskedUrl("Вики бота", botWiki))
				.AppendLine(Formatter.MaskedUrl("\"Около саппорт\" сервер", supServerInvite))
				.AppendLine(Formatter.MaskedUrl("Пригласить бота на свой сервер", botInvite));



			var embed = new DiscordEmbedBuilder
			{
				Description = description.ToString(),
				Color = DiscordColor.Black,
				Timestamp = DateTimeOffset.Now,
				ThumbnailUrl = context.Client.CurrentUser.AvatarUrl
			};
			await context.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
		}

		[Command("Statistics")]
		[Description("Показывает текущую статистику бота")]
		[Aliases("stats")]
		public async Task Statistics(CommandContext context)
		{
			var curProcess = Process.GetCurrentProcess();
			long memoryBytes = curProcess.PrivateMemorySize64;

			var dspVersion = context.Client.VersionString;
			var dncVersion = PlatformServices.Default.Application.RuntimeFramework.Version.ToString(2);

			var guildsMembersIds = context.Client.Guilds.Values.Select(x => x.Members.Values.Where(y => !y.IsBot).Select(z=> z.Id));
			var ids = new List<ulong>();
			foreach(var el in guildsMembersIds)
			{
				ids.AddRange(el);
			}

			long uniqueUsers = ids.Distinct().LongCount();

			var description = new StringBuilder();

			description.AppendLine("Статистика бота")
				.AppendLine("```cs")
				.AppendLine($"Память занимаемая приложением — {memoryBytes.ToSize(SizeUnits.MB)} MБ")
				.AppendLine($"Время работы бота — {Helpers.GetProcessUptime(curProcess).ToReadableString()}")
				.AppendLine($"Пинг — {context.Client.Ping} мс")
				.AppendLine($"Количество обслуживаемых серверов — {context.Client.Guilds.Count}")
				.AppendLine($"Количество уникальных пользователей — {uniqueUsers}")
				.AppendLine($"Версия бота — {this.Bot.Version}")
				.AppendLine($"Версия DSharpPlus — {dspVersion}")
				.AppendLine($"Версия .NET Core — {dncVersion}")
				.AppendLine("```")
				.AppendLine("Статистика Lavalink сервера")
				.AppendLine($"```cs")
				.AppendLine($"Активных плееров — {this.Lavalink.Statistics.ActivePlayers}")
				.AppendLine(
					$"Использование ОЗУ Lavalink сервером — {this.Lavalink.Statistics.RamUsed.ToSize(SizeUnits.MB)}МБ")
				.AppendLine($"Время работы Lavalink сервера — {this.Lavalink.Statistics.Uptime.ToReadableString()}")
				.AppendLine("```");

			var embed = EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithDescription(description.ToString());

			await context.RespondAsync(embed: embed).ConfigureAwait(false);
		}
	}
}
