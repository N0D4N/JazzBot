using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using JazzBot.Attributes;
using JazzBot.Data;
using JazzBot.Exceptions;
using JazzBot.Utilities;

namespace JazzBot.Commands
{
	public sealed class UngruppedCommands : BaseCommandModule
	{
		private Bot Bot { get; }

		public UngruppedCommands(Bot bot)
			=> this.Bot = bot;

		[Command("UserInfo")]
		[Description("Информация об этом участнике сервера")]
		[Aliases("memberinfo")]
		public async Task UserInfo(CommandContext context, [Description("Пользователь информацию о котором вы хотите увидеть")] DiscordMember member = null)
		{
			if (member == null)
				member = context.Member;
			await context.RespondAsync(embed: await this.MemberInfo(member, context).ConfigureAwait(false)).ConfigureAwait(false);
		}

		[Command("UserInfo")]
		public async Task UserInfo(CommandContext context, [RemainingText, Description("Имя пользователя")] string memberName)
		{
			if (string.IsNullOrWhiteSpace(memberName))
				throw new DiscordUserInputException("Имя пользователя не может быть пустым или состоять из пробелов", nameof(memberName));
			var member = context.Guild.Members.Values.FirstOrDefault(x => x.DisplayName == memberName || x.Username == memberName);
			if (member != null)
				await context.RespondAsync(embed: await this.MemberInfo(member, context).ConfigureAwait(false)).ConfigureAwait(false);
			else
				throw new DiscordUserInputException($"Пользователя с никнеймом или юзернеймом {memberName} не найдено", nameof(memberName));
		}

		[Command("Roll")]
		[Description("Выбирает случайное число между данными двумя целыми числами")]
		[Aliases("random", "r")]
		[Priority(2)]
		public async Task Roll(CommandContext context, [Description("Нижняя граница")] int min, [Description("Верхняя граница")] int max)
		{
			if(min > max)
			{
				int temp = max;
				max = min;
				min = temp;
			}
			int result = Helpers.CryptoRandom(min, max);
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle($"Случайное число в границах [{min};{max}] = {result}")).ConfigureAwait(false);
		}

		[Command("Roll")]
		[Priority(1)]
		public async Task Roll(CommandContext context, [Description("Верхняя граница")] int max)
		{
			int min = 0;
			if(max <= min)
				throw new DiscordUserInputException("Верхняя граница должна быть больше 0", nameof(max));

			int result = Helpers.CryptoRandom(min, max);
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle($"Случайное число в границах [{min};{max}] = {result}")).ConfigureAwait(false);
		}

		[Command("Roll")]
		[Priority(0)]
		public async Task Roll(CommandContext context)
		{
			int min = 0;
			int max = 10;

			int result = Helpers.CryptoRandom(min, max);
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle($"Случайное число в границах [{min};{max}] = {result}")).ConfigureAwait(false);
		}

		[Command("Choice")]
		[Description("Выбирает случайный вариант из представленных")]
		[Aliases("Pick")]
		public async Task Choice(CommandContext context, [Description("Варианты среди которых нужно сделать выбор")] params string[] choices)
		{
			if (choices?.Any() != true)
				throw new DiscordUserInputException("Вы должны предоставить хотя бы 1 вариант для выбора", nameof(choices));
			string x = choices[Helpers.CryptoRandom(0, choices.Length)].Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere");
			await context.RespondAsync(x).ConfigureAwait(false);
		}

		[Command("Ban")]
		[Description("Банит пользователя по его ID, пользователю не обязательно находиться на этом сервере")]
		[RequirePermissions(Permissions.BanMembers)]
		public async Task Ban(CommandContext context, [Description("ID пользователя которого нужно забанить")]ulong id, [RemainingText, Description("Причина бана")] string reason = "")
		{
			if (context.Member.Id == id)
				throw new DiscordUserInputException("Вы не можете забанить себя", nameof(id));
			string userString = $"{context.User.Username}#{context.User.Discriminator} ({context.User.Id})";
			string reasonString = string.IsNullOrWhiteSpace(reason) ? " отсутствует" : $": {reason}";

			// Member is still in the guild.
			if (context.Guild.Members.TryGetValue(id, out var member))
			{
				if (context.Member.Hierarchy > member.Hierarchy && context.Guild.CurrentMember.Hierarchy > member.Hierarchy)
				{
					try
					{
						await context.Guild.BanMemberAsync(id, 7, $"Ответственный модератор {userString}. Причина{reasonString}").ConfigureAwait(false);
						await context.RespondAsync("Пользователь успешно забанен.").ConfigureAwait(false);
					}
					catch
					{
						await context.RespondAsync($"Не удалось забанить пользователя с ID = {id}, проверьте правильность ID, или положение высшей роли бота в отношении человека которого вы хотите забанить").ConfigureAwait(false);
					}
				}
				else
					await context.RespondAsync("Вы не можете забанить пользователя с ролью выше вашей, ролью выше бота или владельца сервера");
			}
			// Member is not in the guild.
			else
			{
				try
				{
					await context.Guild.BanMemberAsync(id, 7, $"Ответственный модератор {userString}. Причина{reasonString}").ConfigureAwait(false);
					await context.RespondAsync("Пользователь успешно забанен.").ConfigureAwait(false);
				}
				catch
				{
					await context.RespondAsync($"Не удалось забанить пользователя с ID = {id}, проверьте правильность ID, или положение высшей роли бота в отношении человека которого вы хотите забанить").ConfigureAwait(false);
				}
			}
		}

		[Command("Invite")]
		[Description("Ссылка с приглашением бота на ваш сервер")]
		[Aliases("inv")]
		public async Task Invite(CommandContext context)
		{

			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithDescription(Formatter.MaskedUrl("Ссылка-приглашение бота на ваш сервер",
				new Uri("https://discordapp.com/api/oauth2/authorize?client_id=" + context.Client.CurrentUser.Id + "&permissions=120966212&scope=bot"))))
				.ConfigureAwait(false);
		}

		[Command("say")]
		[Description("Отправляет \"embed\" в заданный канал с заданным текстом")]
		[OwnerOrPermission(Permissions.ManageGuild)]
		public async Task Say(CommandContext context,
			[Description("Канал в который нужно отправить сообщение")]DiscordChannel channelToSayIn,
			[RemainingText, Description("Текст который нужно отправить")] string messageContent)
		{
			if (string.IsNullOrWhiteSpace(messageContent))
				throw new DiscordUserInputException("Содержимое сообщения не должно быть пустым", nameof(messageContent));
			var embed = new DiscordEmbedBuilder
			{
				Description = messageContent.Replace("@everyone", "@\u200beveryone").Replace("@here", "@\u200bhere"),
				Timestamp = DateTime.Now,
				Color = Helpers.ExtendedColor(context.Member, context.Guild.CurrentMember),
			}.WithAuthor($"{context.Member.Username}#{context.Member.Discriminator}", iconUrl: context.Member.AvatarUrl);
			try
			{
				await channelToSayIn.SendMessageAsync(embed: embed).ConfigureAwait(false);
			}
			catch
			{
				await context.RespondAsync("Не удалось отправить сообщение, проверьте все и попробуйте снова").ConfigureAwait(false);
			}
		}

		[Command("Update")]
		[Description("Дает ссылку на информацию о последнем обновлении")]
		public async Task Update(CommandContext context)
		{
			await context.RespondAsync(embed: EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithDescription(Formatter.MaskedUrl("Ссылка на последнее обновление", new Uri(this.Bot.Config.Miscellaneous.UpdateLink)))).ConfigureAwait(false);
		}

		[Command("SyncBans")]
		[Description("Переносит баны из общего с ботом сервера в текущий")]
		[RequirePermissions(Permissions.BanMembers)]
		[RequireUserPermissions(Permissions.ManageGuild)]
		[Cooldown(1, 300, CooldownBucketType.Guild)]
		[Priority(1)]
		public async Task SyncBans(CommandContext context, [Description("Id сервера баны из которого нужно взять")] ulong serverId)
		{
			var banServer = context.Client.Guilds.GetValueOrDefault(serverId);
			await this.BanSyncer(context, banServer);
		}

		[Command("SyncBans")]
		[Priority(0)]
		public async Task SyncBans(CommandContext context, [RemainingText, Description("Название сервера баны из которого нужно взять")] string serverName)
		{
			if(string.IsNullOrWhiteSpace(serverName))
			{
				throw new DiscordUserInputException("Название сервера не может быть пустым или полностью состоять из пробелов");
			}
			serverName = serverName.ToLowerInvariant();
			var banServer = context.Client.Guilds.Values.FirstOrDefault(x => x.Name.ToLowerInvariant() == serverName);
			await this.BanSyncer(context, banServer);
		}

		

		private async Task BanSyncer(CommandContext context, DiscordGuild banServer)
		{
			var member = await banServer?.GetMemberAsync(context.Member.Id);
			if(banServer == null || member == null)
			{
				throw new DiscordUserInputException("Общего сервера с таким названием не найдено, возможно бот состоит в нескольких серверах с таким же названием. Вы можете попробовать воспользоваться перегрузкой и передать в качестве параметра Id сервера.", nameof(banServer.Name));
			}
			var manageGuildPerms = Permissions.ManageGuild;
			var firstGuildChannel = banServer.Channels.Values.FirstOrDefault();
			if(firstGuildChannel == null)
			{
				var hasPermissions = member?.Roles?.Any(x => x.Permissions.HasPermission(manageGuildPerms));
				if(!hasPermissions.GetValueOrDefault(false) || !banServer.EveryoneRole.Permissions.HasPermission(manageGuildPerms))
				{
					throw new DiscordUserInputException($"На выбранном сервере у вас нет права {manageGuildPerms.ToPermissionString()}");
				}
			}
			else if(!member.PermissionsIn(firstGuildChannel).HasPermission(manageGuildPerms))
			{
				throw new DiscordUserInputException($"На выбранном сервере у вас нет права {manageGuildPerms.ToPermissionString()}");
			}
			IReadOnlyList<DiscordBan> bans = null;
			var curGuildBans = (await context.Guild.GetBansAsync())?.ToList();
			try
			{
				bans = await banServer.GetBansAsync();
			}
			catch { }
			if(bans?.Any() == true)
			{
				var toBan = bans.ToList();
				if(curGuildBans?.Any() == true)
				{
					toBan = bans.Except(curGuildBans, new DiscordBanEqualityComparer()).ToList();
				}
				foreach(var ban in toBan)
				{
					try
					{
						await context.Guild.BanMemberAsync(ban.User.Id,
							reason: $"Ответственный модератор: {context.Member.Username}#{context.Member.Discriminator}. Причина: синхронизация банов с сервером {banServer.Name}.");
					}
					catch { }
				}
			}
		}

		private Task<DiscordEmbed> MemberInfo(DiscordMember member, CommandContext context)
		{
			var roles = new StringBuilder();
			if (member.Roles?.Any() == true)
			{
				var rolesList = member.Roles.OrderByDescending(x => x.Position).Select(x => x.Name).ToList();
				rolesList.Add("everyone");
				roles.AppendJoin(',', rolesList.Select(x => $"{Formatter.InlineCode($"@{x}")}"));
			}
			else
				roles.AppendLine(Formatter.InlineCode("@everyone"));
			return Task.FromResult(EmbedTemplates.ExecutedByEmbed(context.Member, context.Guild.CurrentMember)
				.WithTitle("Информация об участнике")
			.AddField("Пользователь", $"{ member.Username}#{member.Discriminator}", true)
			.AddField("ID", member.Id.ToString(), true)
			.AddField("Отображаемое имя", member.DisplayName, true)
			.AddField("Аккаунт создан", $"{(DateTimeOffset.Now - member.CreationTimestamp).Days} дней назад ({member.CreationTimestamp.ToString("dddd, MMM dd yyyy HH:mm:ss zzz", new CultureInfo("ru-Ru"))})", false)
			.AddField("Участник присоединился", $"{(DateTimeOffset.Now - member.JoinedAt).Days} дней назад ({member.JoinedAt.ToString("dddd, MMM dd yyyy HH:mm:ss zzz", new CultureInfo("ru-Ru"))})", false)
			.AddField("Роли", roles.ToString(), false)
			.WithThumbnailUrl(member.AvatarUrl).Build());
		}
	}
}