using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using JazzBot.Data;
using JazzBot.Enums;
using JazzBot.Utilities;
using Microsoft.Extensions.PlatformAbstractions;

namespace JazzBot.Commands
{
	[Group("InfoCommands")]
	[Description("Команды показывающие информацию о боте")]
	[Aliases("info", "inf")]
	class Info : BaseCommandModule
	{
		[Command("BotInfo")]
		[Description("Показывает информацию о этом боте")]
		[Aliases("about")]
		public async Task BotInfo(CommandContext context)
		{
			var jbv = typeof(Bot)
				.GetTypeInfo()
				.Assembly
				?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
				?.InformationalVersion

				??

				typeof(Bot)
				.GetTypeInfo()
				.Assembly
				.GetName()
				.Version
				.ToString(3);

			long memoryBytes = Process.GetCurrentProcess().PrivateMemorySize64;

			var dspv = context.Client.VersionString;

			var dncv = PlatformServices.Default.Application.RuntimeFramework.Version.ToString(2);
			var owner = context.Client.CurrentApplication.Owners.First();

			var embed = new DiscordEmbedBuilder
			{

				Description = $"{Formatter.MaskedUrl(context.Client.CurrentUser.Username, new Uri("https://github.com/N0D4N/JazzBot"))} - создан на C# c помощью библиотеки {Formatter.MaskedUrl("DSharpPlus", new Uri("https://github.com/DSharpPlus/DSharpPlus"))}",
				Color = DiscordColor.Black,
				Timestamp = DateTimeOffset.Now,
				ThumbnailUrl = context.Client.CurrentUser.AvatarUrl
			}
			.WithAuthor($"{owner.Username}", iconUrl: owner.AvatarUrl)
			.AddField("Память занимаемая приложением", $"{memoryBytes.ToSize(SizeUnits.MB)}MB")
			.AddField("Пинг", context.Client.Ping.ToString() + " мс", true)
			.AddField("Время работы", this.ProcessUptime(), true)
			.AddField("Количество обслуживаемых серверов", context.Client.Guilds.Count.ToString(), true)
			.AddField("Версия бота", jbv, true)
			.AddField("Версия DSharpPlus", dspv, true)
			.AddField("Версия .NET Core", dncv, true);
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
