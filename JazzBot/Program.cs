using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using JazzBot.Attributes;
using JazzBot.Commands;
using JazzBot.Data;
using JazzBot.Enums;
using JazzBot.Services;
using JazzBot.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JazzBot
{
	public sealed class Program
	{
		public DiscordClient Client { get; private set; }
		public CommandsNextExtension Commands { get; private set; }
		public InteractivityExtension Interactivity;
		public LavalinkExtension Lavalink { get; private set; }
		public LavalinkNodeConnection LavalinkNode { get; set; }
		public static JazzBotConfig CfgJson { get; private set; }

		public Bot Bot { get; set; }

		private IServiceProvider Services { get; set; }

		private string LogName { get; set; } = null;

		public static void Main(string[] args)
		{
			var prog = new Program();
			prog.RunBotAsync().GetAwaiter().GetResult();

		}

		public async Task RunBotAsync()
		{
			var json = "";
			var file = new FileInfo(@"..\..\..\config.json");
			if (!file.Exists)
				file = new FileInfo("config.json");
			using (var fs = File.OpenRead(file.FullName))
			using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
				json = sr.ReadToEnd();

			CfgJson = JsonConvert.DeserializeObject<JazzBotConfig>(json);

			Console.WriteLine("Конфиг загружен");

			var cfg = new DiscordConfiguration
			{
				Token = CfgJson.Discord.Token,
				TokenType = TokenType.Bot,

				AutoReconnect = true,
				LogLevel = LogLevel.Debug,
				UseInternalLogHandler = true
			};

			this.Client = new DiscordClient(cfg);

			this.Client.Ready += this.Client_Ready;
			this.Client.GuildAvailable += this.Client_GuildAvailable;
			this.Client.ClientErrored += this.Client_ClientError;
			this.Client.VoiceServerUpdated += this.Voice_VoiceServerUpdate;
			this.Client.MessageReactionAdded += this.Client_ReactionAdded;
			this.Bot = new Bot(CfgJson, this.Client);

			this.Services = new ServiceCollection()
				.AddSingleton(this.Client)
				.AddSingleton(this.Bot)
				.AddSingleton<MusicService>()
				.AddSingleton(new YoutubeService(CfgJson.Youtube))
				.AddSingleton(new LavalinkService(CfgJson, this.Client))
				.AddScoped<DatabaseContext>()
				.AddSingleton(this)
				.BuildServiceProvider(true);


			var ccfg = new CommandsNextConfiguration
			{
				StringPrefixes = CfgJson.Discord.Prefixes,

				EnableDefaultHelp = true,

				CaseSensitive = false,

				EnableDms = false,

				EnableMentionPrefix = true,

				Services = this.Services
			};

			this.Commands = this.Client.UseCommandsNext(ccfg);



			this.Commands.CommandExecuted += this.Commands_CommandExecuted;
			this.Commands.CommandErrored += this.Commands_CommandErrored;


			this.Commands.RegisterCommands<Ungrupped>();
			this.Commands.RegisterCommands<MusicCommands>();
			this.Commands.RegisterCommands<OwnerCommands>();
			this.Commands.RegisterCommands<TagCommands>();
			this.Commands.RegisterCommands<Info>();
			this.Commands.SetHelpFormatter<JazzBotHelpFormatter>();

			var icfg = new InteractivityConfiguration
			{
				Timeout = TimeSpan.FromSeconds(60)
			};
			this.Interactivity = this.Client.UseInteractivity(icfg);

			this.Lavalink = this.Client.UseLavalink();


			await this.Client.ConnectAsync();

			await Task.Delay(-1);
		}


		private Task Voice_VoiceServerUpdate(VoiceServerUpdateEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, this.LogName, $"Voice server in {e.Guild.Name} changed to {e.Endpoint}", DateTime.Now);
			return Task.CompletedTask;
		}


		private async Task Client_Ready(ReadyEventArgs e)
		{
			if (this.LogName == null)
				this.LogName = e.Client.CurrentUser.Username;

			Console.Title = this.LogName;



			e.Client.DebugLogger.LogMessage(LogLevel.Info, this.LogName, "Бот готов к работе.", DateTime.Now);

			var db = new DatabaseContext();

			var cuid = (long) e.Client.CurrentUser.Id;

			var config = await db.Configs.SingleOrDefaultAsync(x => x.Id == cuid);
			if (config == null)
			{
				config = new Configs
				{
					Id = cuid,
					Presence = "Music"
				};
				await db.Configs.AddAsync(config);
				if (await db.SaveChangesAsync() <= 0)
					throw new CustomJbException("Не удалось обновить БД", ExceptionType.DatabaseException);
				await e.Client.UpdateStatusAsync(new DiscordActivity(config.Presence, ActivityType.ListeningTo), UserStatus.Online).ConfigureAwait(false);

			}
			else
			{
				if (e.Client.CurrentUser.Presence?.Activity?.Name == null)
					await e.Client.UpdateStatusAsync(new DiscordActivity(config.Presence, ActivityType.ListeningTo), UserStatus.Online).ConfigureAwait(false);

			}
			db.Dispose();
		}

		private async Task Client_ClientError(ClientErrorEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Error, this.LogName, $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);


			var chn = this.Bot.ErrorChannel;

			var ex = e.Exception;
			while (ex is AggregateException || ex.InnerException != null)
				ex = ex.InnerException;

			await chn.SendMessageAsync(embed: EmbedTemplates.ErrorEmbed()
				.WithTitle("Exception")
				.WithDescription($"Exception occured: {ex.GetType()}: {e.Exception.Message}")).ConfigureAwait(false);
		}

		/// <summary>
		/// Allows owner of the bot delete bot's messages by adding reaction to a message.
		/// </summary>
		private async Task Client_ReactionAdded(MessageReactionAddEventArgs e)
		{
			if (e.Message?.Author == null)
				return;
			if (e.Client.CurrentApplication.Owners.All(x => x.Id != e.User.Id) || e.Message.Author.Id != e.Client.CurrentUser.Id)
				return;
			var deleteEmoji = DiscordEmoji.FromName(e.Client, ":no_entry_sign:");
			if (e.Emoji.GetDiscordName() != deleteEmoji.GetDiscordName())
				return;

			await e.Message.DeleteAsync().ConfigureAwait(false);
		}

		private Task Client_GuildAvailable(GuildCreateEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, this.LogName, $"Доступен сервер: {e.Guild.Name}", DateTime.Now);
			return Task.CompletedTask;
		}

		private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
		{
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, this.LogName, $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);
			return Task.CompletedTask;
		}

		private async Task Commands_CommandErrored(CommandErrorEventArgs e)
		{
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, this.LogName, $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);


			var ex = e.Exception;
			while (ex is AggregateException || ex.InnerException != null)
				ex = ex.InnerException;

			// Check if exception is result of command prechecks.
			if (ex is ChecksFailedException exep)
			{


				var failedchecks = exep.FailedChecks.First();
				// Bot is lacking permissions.
				if (failedchecks is RequireBotPermissionsAttribute reqbotperm)
				{
					string permissionsLacking = reqbotperm.Permissions.ToPermissionString();
					var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
					await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
						.WithTitle($"{emoji} Боту не хватает прав")
						.WithDescription(permissionsLacking)).ConfigureAwait(false);
					return;
				}

				// User is lacking permissions.
				if (failedchecks is RequireUserPermissionsAttribute requserperm)
				{
					string permissionsLacking = requserperm.Permissions.ToPermissionString();
					var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

					await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
						.WithTitle($"{emoji} Вам не хватает прав")
						.WithDescription(permissionsLacking)).ConfigureAwait(false);
					return;
				}

				// User is not owner of the bot.
				if (failedchecks is RequireOwnerAttribute reqowner)
				{
					var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
					await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
					.WithTitle($"{emoji} Команда доступна только владельцу")).ConfigureAwait(false);
					return;
				}

				// User is not owner or don't have permissions.
				if (failedchecks is OwnerOrPermissionAttribute ownerOrPermission)
				{
					string permissionsLacking = ownerOrPermission.Permissions.ToPermissionString();
					var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
					await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
						.WithTitle($"{emoji} Вы не являетесь владельцем или вам не хватает прав")
						.WithDescription(permissionsLacking)).ConfigureAwait(false);
					return;
				}

				// Command shouldn't be executed so fast.
				if (failedchecks is CooldownAttribute cooldown)
				{
					await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
						.WithDescription("Вы пытаетесь использовать команду слишком часто, таймер - " +
								$"не больше {cooldown.MaxUses} раз в {cooldown.Reset.TotalMinutes} минут")).ConfigureAwait(false);
					return;
				}
			}
			// In most cases exception caused by user that inputted wrong info.
			else if (ex is ArgumentException argEx)
			{
				var description = new StringBuilder($"Произошла ошибка, скорее всего, связанная с данными вводимыми пользователями, с сообщением: \n{Formatter.InlineCode(argEx.Message)}\n в \n{Formatter.InlineCode(argEx.Source)}");
				if (!string.IsNullOrEmpty(argEx.ParamName))
					description.AppendLine($"Название параметра {Formatter.InlineCode(argEx.ParamName)}.");
				await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
					.WithTitle("Argument exception")
					.WithDescription(description.ToString())).ConfigureAwait(false);
				return;
			}
			else if (ex is CustomJbException jbEx)
			{
				switch (jbEx.ExceptionType)
				{
					case ExceptionType.DatabaseException:
						var embedDb = EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
							.WithTitle("Database exception")
							.WithDescription($"Произошла ошибка связанная с работой БД с сообщением: \n{Formatter.InlineCode(jbEx.Message)}\n в \n{Formatter.InlineCode(jbEx.Source)}");
						await e.Context.RespondAsync(embed: embedDb).ConfigureAwait(false);
						await this.Bot.ErrorChannel.SendMessageAsync(embed: embedDb).ConfigureAwait(false);
						break;

					case ExceptionType.PlaylistException:
						var embedPl = EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
							.WithTitle("Playlist exception")
							.WithDescription($"Произошла ошибка с работой плейлистов (скорее всего плейлиста не существует) с сообщением: \n{Formatter.InlineCode(jbEx.Message)}\n в \n{Formatter.InlineCode(jbEx.Source)}");
						await e.Context.RespondAsync(embed: embedPl).ConfigureAwait(false);
						break;

					case ExceptionType.ForInnerPurposes:
						await this.Bot.ErrorChannel.SendMessageAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
							.WithTitle("Inner Purposes Exception")
							.WithDescription($"Произошла внутренняя ошибка с сообщением: \n{Formatter.InlineCode(jbEx.Message)}\n в \n{Formatter.InlineCode(jbEx.Source)}")).ConfigureAwait(false);
						break;

					case ExceptionType.Unknown:
						await this.Bot.ErrorChannel.SendMessageAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
							.WithTitle("Неизвестная или дефолтная ошибка")
							.WithDescription($"с сообщением: \n{Formatter.InlineCode(jbEx.Message)}\n в \n{Formatter.InlineCode(jbEx.Source)}")).ConfigureAwait(false);
						break;

					case ExceptionType.Default:
						await this.Bot.ErrorChannel.SendMessageAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
							.WithTitle("Неизвестная или дефолтная ошибка")
							.WithDescription($"с сообщением: \n{Formatter.InlineCode(jbEx.Message)}\n в \n{Formatter.InlineCode(jbEx.Source)}")).ConfigureAwait(false);
						break;
					default:
						await this.Bot.ErrorChannel.SendMessageAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
							.WithTitle("Неизвестная или дефолтная ошибка")
							.WithDescription($"с сообщением: \n{Formatter.InlineCode(jbEx.Message)}\n в \n{Formatter.InlineCode(jbEx.Source)}")).ConfigureAwait(false);
						break;
				}
			}
			else if (ex is CommandNotFoundException commandNotFoundException)
			{
				// Ignore.
				return;
			}
			else if (ex is InvalidOperationException invOpEx && invOpEx.Message == "No matching subcommands were found, and this group is not executable.")
			{
				// Ignore.
				return;
			}
			else
			{
				var embed = EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
					.WithTitle("Произошла непредвиденная ошибка в работе команды")
					.WithDescription($"Message: \n{Formatter.InlineCode(ex.Message)}\n в \n{Formatter.InlineCode(ex.Source)}");
				await e.Context.RespondAsync(embed: embed).ConfigureAwait(false);
				await this.Bot.ErrorChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
			}

		}

	}
}