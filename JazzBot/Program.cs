﻿using System;
using System.Collections.Generic;
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
using F23.StringSimilarity;
using JazzBot.Attributes;
using JazzBot.Commands;
using JazzBot.Data;
using JazzBot.Enums;
using JazzBot.Exceptions;
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

		public static void Main(string[] args)
		{
			var prog = new Program();
			prog.RunBotAsync().GetAwaiter().GetResult();

		}

		public async Task RunBotAsync()
		{
			var json = "";
			using (var fs = File.OpenRead("config.json"))
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


			this.Commands.RegisterCommands<UngruppedCommands>();
			this.Commands.RegisterCommands<MusicCommands>();
			this.Commands.RegisterCommands<OwnerCommands>();
			this.Commands.RegisterCommands<TagCommands>();
			this.Commands.RegisterCommands<InfoCommands>();
			this.Commands.RegisterCommands<EmojiCommands>();
			this.Commands.SetHelpFormatter<JazzBotHelpFormatter>();

			this.Commands.RegisterConverter(new CustomEmojiConverter());
			this.Commands.RegisterUserFriendlyTypeName<DiscordEmojiWrapper>("эмодзи");

			var icfg = new InteractivityConfiguration
			{
				Timeout = TimeSpan.FromSeconds(45)
			};
			this.Interactivity = this.Client.UseInteractivity(icfg);

			this.Lavalink = this.Client.UseLavalink();


			await this.Client.ConnectAsync();

			await Task.Delay(-1);
		}


		private Task Voice_VoiceServerUpdate(VoiceServerUpdateEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, this.Bot.LogName, $"Voice server in {e.Guild.Name} changed to {e.Endpoint}", DateTime.Now);
			return Task.CompletedTask;
		}


		private async Task Client_Ready(ReadyEventArgs e)
		{
			if (this.Bot.LogName == null)
			{
				this.Bot.LogName = e.Client.CurrentUser.Username;

				Console.Title = this.Bot.LogName;
			}

			if (string.IsNullOrWhiteSpace(this.Bot.DeleteEmojiName))
				this.Bot.DeleteEmojiName = DiscordEmoji.FromName(e.Client, ":no_entry_sign:").GetDiscordName();

			e.Client.DebugLogger.LogMessage(LogLevel.Info, this.Bot.LogName, "Бот готов к работе.", DateTime.Now);

			using(var db = new DatabaseContext())
			{
				var cuid = (long) e.Client.CurrentUser.Id;

				var config = await db.Configs.SingleOrDefaultAsync(x => x.Id == cuid);
				if(config == null)
				{
					config = new Configs
					{
						Id = cuid,
						Presence = "Music"
					};
					await db.Configs.AddAsync(config);
					int rowsAffected = await db.SaveChangesAsync();
					if(rowsAffected <= 0)
						throw new DatabaseException("Не удалось обновить БД", DatabaseActionType.Update);
					await e.Client.UpdateStatusAsync(new DiscordActivity(config.Presence, ActivityType.ListeningTo), UserStatus.Online).ConfigureAwait(false);

				}
				else
				{
					if(e.Client.CurrentUser.Presence?.Activity?.Name == null)
						await e.Client.UpdateStatusAsync(new DiscordActivity(config.Presence, ActivityType.ListeningTo), UserStatus.Online).ConfigureAwait(false);

				}
			}
		}

		private async Task Client_ClientError(ClientErrorEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Error, this.Bot.LogName, $"Exception occured: {e.Exception.GetType()}: {e.Exception.Message}", DateTime.Now);


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
			if (e.Client.CurrentApplication.Owners.Any(x => x.Id == e.User.Id))
			{
				DiscordMessage msg = null;
				if (e.Message?.Author == null)
					msg = await e.Channel.GetMessageAsync(e.Message.Id);
				else
					msg = e.Message;

				if (msg.Author.IsCurrent && e.Emoji.GetDiscordName() == this.Bot.DeleteEmojiName)
					await e.Message.DeleteAsync().ConfigureAwait(false);
			}
		}

		private Task Client_GuildAvailable(GuildCreateEventArgs e)
		{
			e.Client.DebugLogger.LogMessage(LogLevel.Info, this.Bot.LogName, $"Доступен сервер: {e.Guild.Name}", DateTime.Now);
			return Task.CompletedTask;
		}

		private Task Commands_CommandExecuted(CommandExecutionEventArgs e)
		{
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Info, this.Bot.LogName, $"{e.Context.User.Username} successfully executed '{e.Command.QualifiedName}'", DateTime.Now);
			return Task.CompletedTask;
		}

		private async Task Commands_CommandErrored(CommandErrorEventArgs e)
		{
			e.Context.Client.DebugLogger.LogMessage(LogLevel.Error, this.Bot.LogName, $"{e.Context.User.Username} tried executing '{e.Command?.QualifiedName ?? "<unknown command>"}' but it errored: {e.Exception.GetType()}: {e.Exception.Message ?? "<no message>"}", DateTime.Now);


			var ex = e.Exception;
			while (ex is AggregateException || ex.InnerException != null)
				ex = ex.InnerException;

			// Check if exception is result of command prechecks.
			switch(ex)
			{
				case ChecksFailedException exep:
				{
					var failedchecks = exep.FailedChecks.First();
					switch(failedchecks)
					{
						// Bot is lacking permissions.
						case RequireBotPermissionsAttribute reqbotperm:
						{
							string permissionsLacking = reqbotperm.Permissions.ToPermissionString();
							var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
							await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
								.WithTitle($"{emoji} Боту не хватает прав")
								.WithDescription(permissionsLacking)).ConfigureAwait(false);
							break;
						}

						// User is lacking permissions.
						case RequireUserPermissionsAttribute requserperm:
						{
							string permissionsLacking = requserperm.Permissions.ToPermissionString();
							var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");

							await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
								.WithTitle($"{emoji} Вам не хватает прав")
								.WithDescription(permissionsLacking)).ConfigureAwait(false);
							break;
						}

						// User is not owner of the bot.
						case RequireOwnerAttribute reqowner:
						{
							var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
							await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
							.WithTitle($"{emoji} Команда доступна только владельцу")).ConfigureAwait(false);
							break;
						}
						
						// User is not owner or don't have permissions.
						case OwnerOrPermissionAttribute ownerOrPermission:
						{
							string permissionsLacking = ownerOrPermission.Permissions.ToPermissionString();
							var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
							await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
								.WithTitle($"{emoji} Вы не являетесь владельцем или вам не хватает прав")
								.WithDescription(permissionsLacking)).ConfigureAwait(false);
							break;
						}

						// Command shouldn't be executed so fast.
						case CooldownAttribute cooldown:
						{
							await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
							.WithDescription("Вы пытаетесь использовать команду слишком часто, таймер - " +
									$"не больше {cooldown.MaxUses} раз в {cooldown.Reset.TotalMinutes} минут")).ConfigureAwait(false);
							break;
						}

						// User wasn't connected to voice channel. Optionally to the same voice channel as bot
						case RequireVoiceConnectionAttribute requireVoice:
						{
							await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
							.WithDescription($"Вы должны быть подключены к {(requireVoice.SameVoiceChannelAsBot ? "тому же голосовому каналу что и бот" : "голосовому каналу")}")).ConfigureAwait(false);
							break;
						}

						default:
						{
							await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
								.WithDescription($"Вам не хватает прав, чтобы узнать каких используйте {Formatter.InlineCode($"{this.Bot.Config.Discord.Prefixes.RandomElement()}help {e.Command.QualifiedName}")}"));
							break;
						}
					}

					break;
				}

				case DatabaseException dbEx:
				{
					var description = new StringBuilder("Произошла ошибка в работе БД, возможно стоит попробовать чуть позже.");
					description.AppendLine(string.IsNullOrWhiteSpace(dbEx.Message)
						? $"Тип действия: {dbEx.ActionType.ToString()}"
						: $"Сообщение - {Formatter.InlineCode(dbEx.Message)}. Тип действия: {dbEx.ActionType.ToString()}");

					await e.Context.RespondAsync(embed: EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
						.WithDescription(description.ToString())).ConfigureAwait(false);
					break;
				}

				case DiscordUserInputException inputEx:
				{
					await e.Context.RespondAsync($"{inputEx.Message}. Название параметра {inputEx.ArgumentName}").ConfigureAwait(false);
					break;
				}

				case CommandNotFoundException commandNotFoundException:
				{
					var cmdName = commandNotFoundException.CommandName;
					var suggestedCommands = new List<Command>();
					var nL = new NormalizedLevenshtein();

					// Let's assumme that 0.33 is good Levenshtein distance

					foreach(var cmd in this.Commands.RegisteredCommands.Values.Distinct())
					{
						if(cmd is CommandGroup cmdGroup)
						{
							foreach(var children in cmdGroup.Children)
							{
								if(Helpers.IsCommandSimilar(children, cmdName, nL))
								{
									suggestedCommands.Add(children);
								}
							}
							if(cmdGroup.IsExecutableWithoutSubcommands && Helpers.IsCommandSimilar(cmdGroup, cmdName, nL))
							{
								suggestedCommands.Add(cmdGroup);
							}
						}
						else
						{
							if(Helpers.IsCommandSimilar(cmd, cmdName, nL))
								suggestedCommands.Add(cmd);

						}
					}

					if(suggestedCommands.Any())
					{
						suggestedCommands.OrderBy(x => x.QualifiedName);
						var description = new StringBuilder();
						description.AppendLine($"Команды с названием {Formatter.InlineCode(cmdName)} не найдено. Вот возможные варианты того, что вы имели в виду:");
						foreach(var cmd in suggestedCommands)
							description.AppendLine(Formatter.InlineCode(cmd.QualifiedName));

						await e.Context.RespondAsync(embed: EmbedTemplates.ErrorEmbed()
							.WithDescription(description.ToString())).ConfigureAwait(false);
					}
					break;
				}

				case InvalidOperationException invOpEx when invOpEx.Message == "No matching subcommands were found, and this group is not executable.":
				{
					//Ignore.
					break;
				}

				default:
				{
					var embed = EmbedTemplates.CommandErrorEmbed(e.Context.Member, e.Command)
					.WithTitle("Произошла непредвиденная ошибка в работе команды")
					.WithDescription($"Message: \n{Formatter.InlineCode(ex.Message)}\n в \n{Formatter.InlineCode(ex.Source)}");
					await e.Context.RespondAsync(embed: embed).ConfigureAwait(false);
					await this.Bot.ErrorChannel.SendMessageAsync(embed: embed).ConfigureAwait(false);
					break;
				}
			}
		}

	}
}