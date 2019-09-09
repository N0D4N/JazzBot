using System;
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
using JazzBot.Utilities;
using Microsoft.Extensions.PlatformAbstractions;
using JazzBot.Services;

namespace JazzBot.Commands
{
	[Group("InfoCommands")]
	[Description("Команды показывающие информацию о боте")]
	[Aliases("info", "inf")]
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

			var dncVersion = PlatformServices.Default.Application.RuntimeFramework.Version.ToString(2)
			var owner = context.Client.CurrentApplication.Owners.First();

			var description = new StringBuilder();
			var botReposUri = new Uri("https://github.com/N0D4N/JazzBot");
			var dspReposUri = new Uri("https://github.com/DSharpPlus/DSharpPlus");
			var lavalinkReposUri = new Uri("https://github.com/Frederikam/Lavalink");

			description.AppendLine(
					$"{Formatter.MaskedUrl(this.Bot.LogName, botReposUri)} - создан на C# c помощью библиотеки {Formatter.MaskedUrl("DSharpPlus", dspReposUri)}")
				.AppendLine()
				.AppendLine("Статистика бота")
				.AppendLine($"```cs")
				.AppendLine($"Память занимаемая приложением — {memoryBytes.ToSize(SizeUnits.MB)} MБ")
				.AppendLine($"Пинг — {context.Client.Ping} мс")
				.AppendLine($"Время работы — {this.ProcessUptime()}")
				.AppendLine($"Количество обслуживаемых серверов — {context.Client.Guilds.Count}")
				.AppendLine($"Версия бота — {this.Bot.Version}")
				.AppendLine($"Версия DSharpPlus — {dspVersion}")
				.AppendLine($"Версия .NET Core — {dncVersion}")
				.AppendLine("```")
				.AppendLine()
				.AppendLine($"Статистика {Formatter.MaskedUrl("Lavalink сервера", lavalinkReposUri)}")
				.AppendLine($"```cs")
				.AppendLine($"Активных плееров — {this.Lavalink.Statistics.ActivePlayers}")
				.AppendLine(
					$"Загруженность ЦП Lavalink сервером — {this.Lavalink.Statistics.CpuLavalinkLoad.ToString("F")}%")
				.AppendLine(
					$"Использование ОЗУ Lavalink сервером — {this.Lavalink.Statistics.RamUsed.ToSize(SizeUnits.MB)}МБ")
				.AppendLine("```");



			var embed = new DiscordEmbedBuilder
			{

				Description = description.ToString(),
				Color = DiscordColor.Black,
				Timestamp = DateTimeOffset.Now,
				ThumbnailUrl = context.Client.CurrentUser.AvatarUrl
			};
			await context.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
		}

		[Command("Ping")]
		[Description("Текущий пинг")]
		public async Task Ping(CommandContext context)
		{
			await context.RespondAsync($"Пинг: {context.Client.Ping} мс").ConfigureAwait(false);
		}

		[Command("Uptime")]
		[Description("Время работы")]
		public async Task Uptime(CommandContext context)
		{
			await context.RespondAsync("Время работы" + this.ProcessUptime()).ConfigureAwait(false);
		}

		private string ProcessUptime()
			=> (DateTime.Now - Process.GetCurrentProcess().StartTime).ToString("g");
	}
}
